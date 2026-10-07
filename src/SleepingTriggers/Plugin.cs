using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace SleepingTriggers
{
    // Vanilla POIs have sleeper volumes of type "Trigger" (flags & 7 == 3). They never spawn by
    // proximity (SleeperVolume.CheckTouching / TouchGroup skip them via IsTriggerAndNoRespawn) and
    // only appear when the player hits a trigger (door, loot, trigger volume) - then Spawn() starts
    // WakeAttackLater, which wakes every zombie after 1 s and targets that player.
    // This plugin makes them ordinary sleepers: spawned on approach like any volume. A trigger no
    // longer force-wakes them; instead zombies already spawned get the vanilla Attack-volume stealth
    // check (Touch -> PlayerStealth.CanSleeperAttackDetect), so hidden ambushes still jump out on a
    // noisy player. Volumes first spawned by the trigger itself appear asleep.
    // Sirens are sleeper-volume scripts (MinScript, prefab property SVS<n>) that play an alarm sound
    // and fire "trigger N". Volumes triggered by such a script (or owning one) instead wake each
    // zombie with WakeChance, without an attack target; the rest become light sleepers.
    // Server-side logic only - needed on the host, harmless on clients.
    [BepInPlugin("talos.sleepingtriggers", "Sleeping Trigger Zombies", "1.2.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource L;
        internal static ConfigEntry<float> WakeChance;
        internal static ConfigEntry<string> SirenSounds;

        void Awake()
        {
            L = Logger;
            WakeChance = Config.Bind("Siren", "WakeChance", 0.5f,
                "Chance (0..1) that each zombie of a siren-triggered volume wakes up (without a target on the player)");
            SirenSounds = Config.Bind("Siren", "SoundKeywords", "alarm,siren,buzzer,security",
                "A volume script that plays a sound containing any of these words is a siren");
            new Harmony("talos.sleepingtriggers").PatchAll(typeof(Plugin).Assembly);
            L.LogInfo($"Trigger sleeper volumes spawn on approach as sleepers; triggers do a stealth check; sirens wake {WakeChance.Value:P0} without target");
        }
    }

    static class Siren
    {
        const ushort CmdSound = 40;
        static readonly FieldInfo MinScriptF = AccessTools.Field(typeof(SleeperVolume), "minScript");
        static readonly FieldInfo CommandList = AccessTools.Field(typeof(MinScript), "commandList");
        static readonly ConditionalWeakTable<MinScript, object> Scripts = new ConditionalWeakTable<MinScript, object>();
        static readonly ConditionalWeakTable<SleeperVolume, object> Marked = new ConditionalWeakTable<SleeperVolume, object>();
        static readonly System.Random Rnd = new System.Random();

        // True while a siren script's Tick runs, so volumes it triggers know the source.
        internal static bool InSirenScript;

        internal static bool IsSiren(MinScript ms)
        {
            if (ms == null) return false;
            if (Scripts.TryGetValue(ms, out var cached)) return (bool)cached;
            bool siren = false;
            var keys = Plugin.SirenSounds.Value.Split(',');
            foreach (var cmd in (IList)CommandList.GetValue(ms))
            {
                var t = Traverse.Create(cmd);
                if (t.Field("command").GetValue<ushort>() != CmdSound) continue;
                var sound = t.Field("parameters").GetValue<string>() ?? "";
                foreach (var k in keys)
                    if (k.Trim().Length > 0 && sound.IndexOf(k.Trim(), StringComparison.OrdinalIgnoreCase) >= 0) siren = true;
            }
            Scripts.Add(ms, siren);
            return siren;
        }

        internal static bool OwnsSiren(SleeperVolume sv) => IsSiren((MinScript)MinScriptF.GetValue(sv));

        internal static void Mark(SleeperVolume sv, bool on)
        {
            Marked.Remove(sv);
            if (on) Marked.Add(sv, null);
        }

        internal static bool IsMarked(SleeperVolume sv) => Marked.TryGetValue(sv, out _);

        internal static bool WakeRandom(EntityAlive ea)
        {
            if (Rnd.NextDouble() < Plugin.WakeChance.Value) { ea.ConditionalTriggerSleeperWakeUp(); return true; }
            ea.SetSleeperActive();
            return false;
        }

        internal static IEnumerator WakeLater(EntityAlive ea)
        {
            yield return new WaitForSeconds(1f);
            if (ea != null && !ea.IsDead()) WakeRandom(ea);
        }
    }

    [HarmonyPatch(typeof(SleeperVolume), nameof(SleeperVolume.IsTriggerAndNoRespawn), MethodType.Getter)]
    static class SpawnOnApproach
    {
        static bool Prefix(ref bool __result) { __result = false; return false; }
    }

    [HarmonyPatch(typeof(SleeperVolume), "WakeAttackLater")]
    static class NoWakeOnTrigger
    {
        static bool Prefix(SleeperVolume __instance, EntityAlive _ea, ref IEnumerator __result)
        {
            __result = Siren.IsMarked(__instance) ? Siren.WakeLater(_ea) : Empty();
            return false;
        }
        static IEnumerator Empty() { yield break; }
    }

    [HarmonyPatch(typeof(MinScript), nameof(MinScript.Tick))]
    static class SirenScriptContext
    {
        static void Prefix(MinScript __instance, out bool __state)
        {
            __state = Siren.InSirenScript;
            Siren.InSirenScript = Siren.IsSiren(__instance);
        }
        static void Finalizer(bool __state) { Siren.InSirenScript = __state; }
    }

    [HarmonyPatch(typeof(SleeperVolume), nameof(SleeperVolume.OnTriggered))]
    static class StealthCheckOnTrigger
    {
        static readonly MethodInfo Touch = AccessTools.Method(typeof(SleeperVolume), "Touch");
        static readonly FieldInfo RespawnMap = AccessTools.Field(typeof(SleeperVolume), "respawnMap");
        static readonly object Attack = Enum.ToObject(Touch.GetParameters()[3].ParameterType, 2);

        static void Prefix(bool ___isSpawned, out bool __state) { __state = ___isSpawned; }

        static void Postfix(SleeperVolume __instance, EntityPlayer _player, World _world, bool __state)
        {
            bool siren = Siren.InSirenScript || Siren.OwnsSiren(__instance);
            Siren.Mark(__instance, siren);
            if (!__state || _player == null) return;

            var alive = new List<EntityAlive>();
            foreach (int id in ((IDictionary)RespawnMap.GetValue(__instance)).Keys)
                if (_world.GetEntity(id) is EntityAlive ea && !ea.IsDead()) alive.Add(ea);

            if (siren)
            {
                int woke = 0;
                foreach (var ea in alive) if (Siren.WakeRandom(ea)) woke++;
                Plugin.L.LogInfo($"Siren by {_player.EntityName}: volume {__instance.BoxMin}, woke {woke} of {alive.Count} without target");
                return;
            }
            Touch.Invoke(__instance, new object[] { _world, _player, true, Attack });
            Plugin.L.LogInfo($"Trigger by {_player.EntityName}: volume {__instance.BoxMin} spawned, stealth check on {alive.Count} alive (crouching={_player.IsCrouching})");
        }
    }
}
