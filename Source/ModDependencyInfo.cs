using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ActivateDependencies
{
    public class ModDependencyInfo
    {
        private readonly static IEnumerable<ModDependency> emptyList = new List<ModDependency>();
        private readonly static Dictionary<ModMetaData, ModDependencyInfo> info = new Dictionary<ModMetaData, ModDependencyInfo>();
        private readonly static Dictionary<string, ModMetaData> mods = new Dictionary<string, ModMetaData>();

        public readonly static ModDependencyInfo Empty = new ModDependencyInfo();

        public static bool AnyToActivate => ToActivate.Any();

        private static IEnumerable<ModDependencyInfo> unfulfilledCached;
        public static IEnumerable<ModDependencyInfo> ToActivate
        {
            get
            {
                if (unfulfilledCached == null)
                {
                    unfulfilledCached = ModLister.AllInstalledMods
                        .Where(mod => mod.Active)
                        .Select(For)
                        .Where(info => !info.DeepAllActive);
                }
                return unfulfilledCached;
            }
        }

        public static void ClearUnfulfilled() => unfulfilledCached = null;

        public static ModDependencyInfo For(ModDependency dep) => For(ModFor(dep?.packageId));

        public static ModDependencyInfo For(ModMetaData mod)
        {
            if (mod == null) return Empty;
            if (!info.ContainsKey(mod))
            {
              info[mod] = new ModDependencyInfo(mod);
            }
            return info[mod];
        }

        public static ModMetaData ModFor(ModDependency dep) => ModFor(dep?.packageId);

        public static ModMetaData ModFor(string id)
        {
            if (id == null) return null;
            if (!mods.ContainsKey(id))
            {
                var mod = ModLister.GetModWithIdentifier(id, ignorePostfix: true);
                if (mod == null) return null;
                mods[id] = mod;
            }
            return mods[id];
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

        public IEnumerable<ModDependency> DirectUnsatisfied => Direct.Where(dep => !dep.IsSatisfied);
        public IEnumerable<ModDependency> DeepUnsatisfied => Deep.Where(dep => !dep.IsSatisfied);

        public bool DirectSatisfied => !DirectUnsatisfied.Any();
        public bool DeepSatisfied => !DeepUnsatisfied.Any();


        public IEnumerable<ModDependency> DirectInactive => Direct.Where(dep => dep.Inactive());
        public IEnumerable<ModDependency> DeepInactive => Deep.Where(dep => dep.Inactive());

        public bool DirectAllActive => !DirectInactive.Any();
        public bool DeepAllActive => !DeepInactive.Any();


        public bool Installed => Mod != null;
        public bool Active => Mod?.Active ?? false;
        public bool Inactive => Installed && !Mod.Active;

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

        public static bool Inactive(this ModDependency input) => ModDependencyInfo.For(input).Inactive;
    }
}
