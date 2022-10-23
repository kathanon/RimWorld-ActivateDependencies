using HarmonyLib;
using Verse;
using RimWorld;

namespace ActivateDependencies
{
    [StaticConstructorOnStartup]
    public static class Main
    {
        static Main()
        {
            var harmony = new Harmony(Strings.ID);
            harmony.PatchAll();
        }
    }
}
