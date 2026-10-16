using System.Reflection;
using HarmonyLib;

namespace HonestTooltips
{
    // Native mod API (no BepInEx). Adds a character window binding {ulmtRunStamina}: stamina change per second
    // while running, computed exactly like PlayerEntityStats.UpdatePlayerStaminaOT does.
    public class ModApi : IModApi
    {
        public void InitMod(Mod _modInstance)
        {
            new Harmony("talos.honesttooltips").PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch]
    static class CharacterWindowBindings
    {
        static MethodBase target;

        static bool Prepare()
        {
            var t = AccessTools.TypeByName("XUiC_ULM_CharacterFrameWindow");
            target = t == null ? null : AccessTools.Method(t, "GetBindingValueInternal");
            if (target == null) Log.Warning("[HonestTooltips] Undead Legacy character window not found, running stamina row disabled");
            return target != null;
        }

        static MethodBase TargetMethod() => target;

        static bool Prefix(XUiController __instance, ref string value, string bindingName, ref bool __result)
        {
            if (bindingName != "ulmtRunStamina") return true;
            var player = __instance.xui?.playerUI?.entityPlayer;
            value = player == null ? "" : RunStamina(player).ToString("0.##") + "/s";
            __result = true;
            return false;
        }

        // The game evaluates StaminaChangeOT with the current movement|stance tags, and effect groups also check
        // the movement tag (UL's running drain requires !EntityHasMovementTag idle). The window is open while
        // standing still, so pretend for this one evaluation that the player runs while standing.
        static float RunStamina(EntityPlayer player)
        {
            var move = player.CurrentMovementTag;
            var stance = player.CurrentStanceTag;
            float v;
            try
            {
                player.CurrentMovementTag = EntityAlive.MovementTagRunning;
                player.CurrentStanceTag = EntityAlive.StanceTagStanding;
                v = EffectManager.GetValue(PassiveEffects.StaminaChangeOT, null, 0f, player, null,
                    EntityAlive.MovementTagRunning | EntityAlive.StanceTagStanding);
            }
            finally
            {
                player.CurrentMovementTag = move;
                player.CurrentStanceTag = stance;
            }
            if (v > 0f && player.Stats is PlayerEntityStats stats)
                v *= System.Math.Max(0.2f, stats.GetWaterPercent());
            return v;
        }
    }
}
