using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ActivateDependencies
{
    public class ModDependencyInfo
    {
        private readonly static IEnumerable<ModDependency> emptyList = new List<ModDependency>();
        private readonly static Dictionary<ModMetaData, ModDependencyInfo> dict = new Dictionary<ModMetaData, ModDependencyInfo>();

        public readonly static ModDependencyInfo Empty = new ModDependencyInfo();

        public static bool AnyUnfulfilled => Unfulfilled.Any();

        private static IEnumerable<ModDependencyInfo> unfulfilledCached;
        public static IEnumerable<ModDependencyInfo> Unfulfilled
        {
            get
            {
                if (unfulfilledCached == null)
                {
                    unfulfilledCached = ModLister.AllInstalledMods
                        .Where(m => m.Active)
                        .Select(For)
                        .Where(m => !m.DeepSatisfied);
                }
                return unfulfilledCached;
            }
        }

        public static void ClearUnfulfilled() => unfulfilledCached = null;

        public static ModDependencyInfo For(ModDependency dep) => For(ModLister.GetModWithIdentifier(dep.packageId, ignorePostfix: true));

        public static ModDependencyInfo For(ModMetaData mod)
        {
            if (mod == null) return Empty;
            if (!dict.ContainsKey(mod))
            {
              dict[mod] = new ModDependencyInfo(mod);
            }
            return dict[mod];
        }

        private List<ModDependency> deepCache;

        public readonly ModMetaData Mod;

        public IEnumerable<ModDependency> Direct { get => Mod?.Dependencies ?? emptyList; }

        public IEnumerable<ModDependency> Deep
        {
            get
            {
                if (deepCache == null)
                {
                    deepCache = new List<ModDependency>();
                    BuildDeep(deepCache, new HashSet<ModDependency>());
                }
                return deepCache;
            }
        }

        public IEnumerable<ModDependency> Unsatisfied => Direct.Where(dep => !dep.IsSatisfied);
        public IEnumerable<ModDependency> DeepUnsatisfied => Deep.Where(dep => !dep.IsSatisfied);

        public bool Satisfied => !Unsatisfied.Any();
        public bool DeepSatisfied => !DeepUnsatisfied.Any();

        public bool Installed => Mod != null;
        public bool Active => Mod.Active;

        private void BuildDeep(List<ModDependency> list, HashSet<ModDependency> seen)
        {
            foreach (var dep in Direct)
            {
                if (!seen.Contains(dep))
                {
                    seen.Add(dep);
                    list.Add(dep);
                    For(dep).BuildDeep(list, seen);
                }
            }
        }

        public ModDependencyInfo(ModMetaData mod)
        {
            Mod = mod;
        }

        private ModDependencyInfo() { }
    }

    public static class ModDependencyExtensions
    {
        public static IEnumerable<ModDependency> Unsatisfied(this IEnumerable<ModDependency> input) => input.Where(dep => !dep.IsSatisfied);
    }
}
