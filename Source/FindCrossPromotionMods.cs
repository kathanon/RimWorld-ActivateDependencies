using HarmonyLib;
using Verse;
using Verse.Steam;

namespace ActivateDependencies
{
    internal static class FindCrossPromotionMods
    {
        private static bool active;
        private static bool setupDone;

        private static void Setup()
        {
            active = Harmony.HasAnyPatches(Strings.CrossPromotionID);
            setupDone = true;
        }

        internal static bool IsCrossPromotion(ModMetaData mod)
        {
            if (!setupDone) Setup();
            if (!active || !SteamManager.Initialized) return false;

            // TODO: Figure out what mods uses CrossPromotion, and what user IDs it is active for
            return mod.PackageId.StartsWith("brrainz.");
        }
    }
}
