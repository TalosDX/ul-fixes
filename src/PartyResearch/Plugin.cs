using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace ULPartyResearch
{
    // Undead Legacy gates crafting recipes by research of the LOCAL player only
    // (ULM_Recipe.IsUnlockedByReseachOrDefault(EntityPlayer, Recipe), used by ULM_Recipe.IsUnlocked
    // and the crafting UIs). UL already has ULM_Research.KnownByPartyList(EntityPlayerLocal, out bool)
    // for its research UI. This postfix lets a recipe through when the gate said "locked" but the
    // research is known by a party member. Everything is resolved by reflection, so a UL update that
    // renames a member just disables the patch with an error in the log.
    // Client-side: every player who wants to craft with party research needs it.
    [BepInPlugin("ul.party.research", "UL Party Research Craft", "0.2.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource L;

        static MethodInfo _gate;          // ULM_Recipe.IsUnlockedByReseachOrDefault(EntityPlayer, Recipe)
        static MethodInfo _getUnlock;     // ULM_ResearchManager.GetUnlock(string) -> ULM_Research
        static MethodInfo _get;           // ULM_ResearchManager.Get(string) -> ULM_Research (fallback)
        static MethodInfo _knownByParty;  // ULM_Research.KnownByPartyList(EntityPlayerLocal, out bool)
        static Type _entityPlayerLocal;
        static MethodInfo _recipeGetName; // Recipe.GetName()
        static int _diag;
        const int DiagMax = 30;

        void Awake()
        {
            L = Logger;
            try { Init(); }
            catch (Exception e) { L.LogError("Init failed: " + e); }
        }

        void Init()
        {
            Assembly ul = null;
            Type manager = null;
            Type research = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t;
                try { t = asm.GetTypes().FirstOrDefault(x => x.Name == "ULM_ResearchManager"); }
                catch { continue; }
                if (t != null) { ul = asm; manager = t; break; }
            }
            if (ul == null) { L.LogError("UL assembly not found"); return; }

            Type[] types;
            try { types = ul.GetTypes(); }
            catch (ReflectionTypeLoadException e) { types = e.Types.Where(x => x != null).ToArray(); }

            const BindingFlags S = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var recipeType = types.FirstOrDefault(x => x.Name == "ULM_Recipe");
            research = types.FirstOrDefault(x => x.Name == "ULM_Research");

            _gate = recipeType?.GetMethods(S).FirstOrDefault(m =>
                m.Name.IndexOf("IsUnlockedByRese", StringComparison.OrdinalIgnoreCase) >= 0
                && m.ReturnType == typeof(bool) && m.GetParameters().Length == 2);
            _getUnlock = manager.GetMethods(S).FirstOrDefault(m => m.Name == "GetUnlock" && m.GetParameters().Length == 1);
            _get = manager.GetMethods(S).FirstOrDefault(m => m.Name == "Get" && m.GetParameters().Length == 1);
            _knownByParty = research?.GetMethods(I).FirstOrDefault(m => m.Name == "KnownByPartyList");
            _entityPlayerLocal = _knownByParty?.GetParameters().FirstOrDefault()?.ParameterType;
            _recipeGetName = (_gate?.GetParameters().ElementAtOrDefault(1)?.ParameterType)?.GetMethods(I)
                .FirstOrDefault(m => m.Name == "GetName" && m.GetParameters().Length == 0 && m.ReturnType == typeof(string));

            L.LogInfo(string.Format("resolve: gate={0} getUnlock={1} get={2} ", _gate != null, _getUnlock != null, _get != null)
                + string.Format("knownByParty={0} recipeGetName={1} epl={2}", _knownByParty != null, _recipeGetName != null, _entityPlayerLocal?.Name));

            if (_gate == null || _knownByParty == null || (_getUnlock == null && _get == null) || _recipeGetName == null)
            {
                L.LogError("missing required members; not patching. See resolve line above.");
                return;
            }

            var harmony = new Harmony("ul.party.research");
            harmony.Patch(_gate, postfix: new HarmonyMethod(typeof(Plugin).GetMethod(nameof(GatePostfix), BindingFlags.Static | BindingFlags.NonPublic)));
            L.LogInfo("=== UL Party Research Craft active (patched gate) ===");
        }

        static void GatePostfix(object[] __args, ref bool __result)
        {
            if (__result) return;
            try
            {
                object player = __args[0];
                object recipe = __args[1];
                if (recipe == null) return;

                var name = (string)_recipeGetName.Invoke(recipe, null);
                if (string.IsNullOrEmpty(name)) return;

                object res = _getUnlock?.Invoke(null, new object[] { name }) ?? _get?.Invoke(null, new object[] { name });
                if (res == null) return;

                object local = _entityPlayerLocal.IsInstanceOfType(player) ? player : LocalPlayer();
                if (local == null || !_entityPlayerLocal.IsInstanceOfType(local)) return;

                var args = new object[] { local, false };
                _knownByParty.Invoke(res, args);
                if (args[1] is bool known && known)
                {
                    __result = true;
                    if (_diag < DiagMax) { _diag++; L.LogInfo("[enabled by party] " + name); }
                }
            }
            catch (Exception e)
            {
                if (_diag < DiagMax) { _diag++; L.LogWarning("postfix ex: " + e.Message); }
            }
        }

        static object LocalPlayer()
        {
            try
            {
                var world = Traverse.Create(Traverse.Create(AccessTools.TypeByName("GameManager")).Property("Instance").GetValue())
                    .Property("World").GetValue();
                return world?.GetType().GetMethod("GetPrimaryPlayer")?.Invoke(world, null);
            }
            catch { return null; }
        }
    }
}
