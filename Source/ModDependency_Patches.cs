using HarmonyLib;
using RimWorld;
using Steamworks;
using System.Text.RegularExpressions;
using Verse;

namespace ActivateDependencies
{
    [HarmonyPatch(typeof(ModDependency))]
    public static class ModDependency_Patches
    {
        public static bool InIcon = false;

        private const string steamURI = "steam://url/CommunityFilePage/";
        private static readonly Regex uriRe = 
            new Regex(@"^https?://steamcommunity\.com/(sharedfiles|workshop)/filedetails/\?id=([0-9]+)$");
        private static readonly Regex tipRe = 
            new Regex($"({Regex.Escape(Strings.CoreClickWeb)}|{Regex.Escape(Strings.CoreClickSel)})");

        [HarmonyPostfix]
        [HarmonyPatch(nameof(ModDependency.Url), MethodType.Getter)]
        public static string Url(string orig)
        {
            if (!SteamAPI.IsSteamRunning()) return orig;
            var match = uriRe.Match(orig);
            return match.Success ? steamURI + match.Groups[2] : orig;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(ModDependency.Tooltip), MethodType.Getter)]
        public static string Tooltip(string orig, ModDependency __instance)
        {
            var mod = ModDependencyInfo.ModFor(__instance);
            if (mod != null)
            {
                if (InIcon)
                {
                    string add = mod.Active ? Strings.ClickDeactivate : Strings.ClickActivate;
                    var m = tipRe.Match(orig);
                    return m.Success ? orig.Substring(0, m.Index) + add : $"{orig}\n\n{add}";
                }
                else if (mod.Active)
                {
                    return $"{orig}\n\n{Strings.CoreClickSel}";
                }
            }
            return orig;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(ModDependency.OnClicked))]
        public static void OnClicked(Page_ModsConfig window, ModDependency __instance)
        {
            ModMetaData mod = ModLister.GetModWithIdentifier(__instance.packageId, ignorePostfix: true);
            if (mod != null && mod.Active)
            {
                window.SelectMod(mod);
            }
        }
    }
}
