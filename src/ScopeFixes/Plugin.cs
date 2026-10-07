using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace ScopeExposureFix
{
    // Fixes for Undead Legacy picture-in-picture optics ("WeaponScopeCamera" under the player camera).
    // Client-side - every player who wants the fixes needs it.
    [BepInPlugin("talos.scopeexposurefix", "UL Scope Fixes", "1.2.0")]
    [BepInDependency("UndeadLegacy", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource L;

        void Awake()
        {
            L = Logger;
            var harmony = new Harmony("talos.scopeexposurefix");
            // Patch each fix on its own so one broken by a UL update does not take the others down.
            HipOffsets.Load();
            foreach (var type in new[] { typeof(FixPipScopeVolumes), typeof(PipMiddleClickZoom), typeof(ScopeOcclusionMargin), typeof(HipOffsetPatch) })
            {
                try { harmony.CreateClassProcessor(type).Patch(); }
                catch (System.Exception e) { L.LogError($"{type.Name} failed to patch: {e}"); }
            }
        }

        internal static bool Found(MethodBase m, string what)
        {
            if (m != null) return true;
            L.LogWarning(what + " not found - Undead Legacy changed, this fix is disabled");
            return false;
        }
    }

    // MinEventActionULM_StartPipScope adds a PostProcessLayer to the scope camera but never sets
    // volumeLayer, so the layer sees no PostProcessVolume: no auto exposure (night eye adaptation)
    // and no color grading / brightness. Give it the player camera's volume layer.
    [HarmonyPatch]
    static class FixPipScopeVolumes
    {
        static bool _logged;

        static MethodBase Target => AccessTools.Method("MinEventActionULM_StartPipScope:Execute");
        static bool Prepare() => Plugin.Found(Target, "MinEventActionULM_StartPipScope.Execute");
        static MethodBase TargetMethod() => Target;

        static void Postfix()
        {
            var player = GameManager.Instance?.World?.GetPrimaryPlayer();
            if (player == null || player.cameraTransform == null) return;
            var main = player.cameraTransform.GetComponent<PostProcessLayer>();
            var scope = player.cameraTransform.Find("WeaponScopeCamera");
            if (main == null || scope == null) return;
            foreach (var layer in scope.GetComponentsInChildren<PostProcessLayer>(true))
            {
                if (layer == main || layer.volumeLayer == main.volumeLayer) continue;
                layer.volumeLayer = main.volumeLayer;
                layer.volumeTrigger = layer.transform;
                if (!_logged) { _logged = true; Plugin.L.LogInfo($"Scope camera now uses post-process volumes (layer mask {main.volumeLayer.value})"); }
            }
        }
    }

    // Middle mouse (CameraFunction) in ItemActionULM_Zoom.ConsumeCameraFunction always steps the
    // PLAYER camera FOV (CurrentZoom: MaxZoomIn <-> MaxZoomOut, -5 per press), even for PIP optics,
    // so the whole screen zooms and the scope image does not. For PIP optics apply the same rule to
    // the scope camera FOV instead (PipZoomMax = most zoomed in, PipZoomMin = most zoomed out).
    [HarmonyPatch]
    static class PipMiddleClickZoom
    {
        static MethodBase Target => AccessTools.Method("ItemActionULM_Zoom:ConsumeCameraFunction");
        static bool Prepare() => Plugin.Found(Target, "ItemActionULM_Zoom.ConsumeCameraFunction");
        static MethodBase TargetMethod() => Target;

        static bool Prefix(ItemActionData _actionData, ref bool __result)
        {
            var holder = _actionData?.invData?.holdingEntity;
            var t = Traverse.Create(_actionData);
            if (holder == null || !holder.AimingGun || !t.Field("hasScopeCam").GetValue<bool>()) return true;

            float zoomedIn = t.Field("PipZoomMax").GetValue<float>();
            float zoomedOut = t.Field("PipZoomMin").GetValue<float>();
            if (Mathf.Approximately(zoomedIn, zoomedOut) || t.Field("bZoomInProgress").GetValue<bool>())
            {
                __result = true;
                return false;
            }
            float cur = t.Field("ScopeZoom").GetValue<float>();
            float next = Mathf.Approximately(cur, zoomedIn) ? zoomedOut : Mathf.Clamp(cur - 5f, zoomedIn, zoomedOut);
            t.Field("ScopeZoom").SetValue(next);
            var cam = t.Field("ScopeCamera").GetValue<Camera>();
            if (cam != null) cam.fieldOfView = next;
            holder.Buffs.SetCustomVar(".ScopeZoom", next, false, default);
            __result = true;
            return false;
        }
    }

    // UL replaces OcclusionManager.UpdateVisibility. While a PIP scope is up it keeps an occluded
    // renderer visible if its pivot projects inside the scope camera's NDC box +/- margin, where
    // margin = clamp(1.5 + maxExtent / (dist + 0.1), max 3.5) ignores the camera FOV. At high
    // magnification a zombie whose renderer pivot (feet/root) is off the aim point lands outside the
    // box and is hidden even though it fills the scope. Widen the margin by the real on-screen size:
    // 1 + (maxExtent + 0.5) / (dist * tan(fov / 2)). Has no effect at normal FOV.
    [HarmonyPatch]
    static class ScopeOcclusionMargin
    {
        static MethodBase Target =>
            AccessTools.Method("H_OcclussionManagerPatch+Patch_OcclusionManager_UpdateVisibility:Prefix");
        static bool Prepare() => Plugin.Found(Target, "UL OcclusionManager.UpdateVisibility patch");
        static MethodBase TargetMethod() => Target;

        public static float Widen(float margin, float dist, float maxExtent, Camera cam)
        {
            if (cam == null) return margin;
            float tan = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            if (tan <= 0f) return margin;
            float m = 1f + (maxExtent + 0.5f) / (Mathf.Max(dist, 0.1f) * tan);
            return m > margin ? m : margin;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var c = new List<CodeInstruction>(instructions);
            int sqrt = c.FindIndex(i => i.opcode == OpCodes.Call && i.operand is MethodInfo m && m.Name == "Sqrt");
            int ext = c.FindIndex(sqrt + 1, i => i.opcode == OpCodes.Ldfld && i.operand is FieldInfo f && f.Name == "maxExtent");
            int cap = -1;
            for (int k = ext < 0 ? c.Count : ext; k + 2 < c.Count; k++)
                if (c[k].opcode == OpCodes.Ldc_R4 && (float)c[k].operand == 3.5f && c[k + 1].opcode == OpCodes.Stloc_S) { cap = k; break; }
            if (sqrt < 0 || ext < 0 || cap < 0 || c[sqrt + 1].opcode != OpCodes.Stloc_S || c[ext - 1].opcode != OpCodes.Ldloc_S)
            {
                Plugin.L.LogWarning("UL occlusion code layout changed - scope occlusion fix disabled");
                return c;
            }
            object distLocal = c[sqrt + 1].operand;
            var entryLoad = c[ext - 1];
            var extField = c[ext].operand;
            var marginStore = c[cap + 1];
            int at = cap + 2;
            var inject = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldloc_S, marginStore.operand),
                new CodeInstruction(OpCodes.Ldloc_S, distLocal),
                new CodeInstruction(entryLoad.opcode, entryLoad.operand),
                new CodeInstruction(OpCodes.Ldfld, extField),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ScopeOcclusionMargin), nameof(Widen))),
                new CodeInstruction(marginStore.opcode, marginStore.operand),
            };
            c[at].MoveLabelsTo(inject[0]);
            c.InsertRange(at, inject);
            Plugin.L.LogInfo("Scope occlusion margin now accounts for magnification");
            return c;
        }
    }

    // Per-item hip (not aiming) weapon offset. vp_FPWeapon.Refresh springs the view model to
    // DefaultPosition + AimingPositionOffset; UL/vanilla zoom actions set AimingPositionOffset to the
    // scope offset while aiming and to zero otherwise. For listed items we put our offset there while
    // NOT aiming, so the hip position drops but aiming alignment is untouched.
    // Offsets live in fpv_offsets.txt next to the DLL ("itemName=x,y,z") and in AppDomain data, because
    // the game's mod loader and BepInEx may each load their own copy of this assembly (the console
    // command runs in one copy, the patch in the other).
    static class HipOffsets
    {
        const string Key = "talos.scopefixes.hipoffsets";
        internal const string DefaultContent = "ulmGunKineticSniperRifleM1A=0,-0.06,0\n";

        internal static string FilePath =>
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(HipOffsets).Assembly.Location), "fpv_offsets.txt");

        internal static Dictionary<string, Vector3> Map
        {
            get
            {
                if (System.AppDomain.CurrentDomain.GetData(Key) is Dictionary<string, Vector3> d) return d;
                d = new Dictionary<string, Vector3>();
                System.AppDomain.CurrentDomain.SetData(Key, d);
                return d;
            }
        }

        internal static void Load()
        {
            var map = Map;
            map.Clear();
            if (!System.IO.File.Exists(FilePath)) System.IO.File.WriteAllText(FilePath, DefaultContent);
            foreach (var raw in System.IO.File.ReadAllLines(FilePath))
            {
                var line = raw.Trim();
                int eq = line.IndexOf('=');
                if (line.Length == 0 || line.StartsWith("#") || eq <= 0) continue;
                var v = line.Substring(eq + 1).Split(',');
                if (v.Length == 3 && float.TryParse(v[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var x)
                    && float.TryParse(v[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var y)
                    && float.TryParse(v[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var z))
                    map[line.Substring(0, eq).Trim()] = new Vector3(x, y, z);
            }
        }

        internal static void Save()
        {
            var sb = new System.Text.StringBuilder("# itemName=x,y,z (metres; y < 0 lowers the weapon). Edit in game: fpvoffset\n");
            foreach (var kv in Map)
                sb.Append(kv.Key).Append('=').Append(F(kv.Value.x)).Append(',').Append(F(kv.Value.y)).Append(',').Append(F(kv.Value.z)).Append('\n');
            System.IO.File.WriteAllText(FilePath, sb.ToString());
        }

        internal static string F(float f) => f.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
    }

    [HarmonyPatch(typeof(vp_FPWeapon), "Update")]
    static class HipOffsetPatch
    {
        static bool _ours;

        static void Postfix(vp_FPWeapon __instance)
        {
            var p = GameManager.Instance?.World?.GetPrimaryPlayer();
            if (p == null || p.AimingGun || p.vp_FPWeapon != __instance) return;
            var item = p.inventory?.holdingItem;
            bool listed = item != null && HipOffsets.Map.TryGetValue(item.Name, out var offset);
            if (!listed && !_ours) return;
            var want = listed ? HipOffsets.Map[item.Name] : Vector3.zero;
            _ours = listed;
            if (__instance.AimingPositionOffset == want) return;
            __instance.AimingPositionOffset = want;
            __instance.Refresh();
        }
    }

    public class ConsoleCmdFpvOffset : ConsoleCmdAbstract
    {
        public override bool IsExecuteOnClient => true;
        public override int DefaultPermissionLevel => 1000;
        public override string[] getCommands() => new[] { "fpvoffset" };
        public override string getDescription() => "Hip position offset of the held weapon (UL Scope Fixes)";
        public override string getHelp() =>
            "fpvoffset            - show the held item and its offset\n" +
            "fpvoffset <x> <y> <z> - set the offset for the held item (metres, y < 0 = lower), saved\n" +
            "fpvoffset reset      - remove the offset for the held item";

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            var out_ = SdtdConsole.Instance;
            var item = GameManager.Instance?.World?.GetPrimaryPlayer()?.inventory?.holdingItem;
            if (item == null) { out_.Output("No held item"); return; }
            var map = HipOffsets.Map;
            if (_params.Count == 1 && _params[0] == "reset")
            {
                map.Remove(item.Name);
                HipOffsets.Save();
            }
            else if (_params.Count == 3)
            {
                var v = new float[3];
                for (int i = 0; i < 3; i++)
                    if (!float.TryParse(_params[i].Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v[i]))
                    { out_.Output("Bad number: " + _params[i]); return; }
                map[item.Name] = new Vector3(v[0], v[1], v[2]);
                HipOffsets.Save();
            }
            else if (_params.Count != 0) { out_.Output(getHelp()); return; }
            out_.Output(map.TryGetValue(item.Name, out var o)
                ? $"{item.Name}: {HipOffsets.F(o.x)} {HipOffsets.F(o.y)} {HipOffsets.F(o.z)}"
                : $"{item.Name}: no offset");
        }
    }
}
