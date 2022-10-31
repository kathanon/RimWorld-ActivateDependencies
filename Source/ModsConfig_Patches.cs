using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Steam;

namespace ActivateDependencies {
    public enum Anchor { TL, TC, TR, CL, C, CR, BL, BC, BR }

    [HarmonyPatch(typeof(Page_ModsConfig))]
    public static class ModsConfig_Patches {
        public const float BottomButtonWidth  = 150f;
        public const float BottomButtonHeight =  26f;
        public const float ActivateAllWidth   = 200f;
        public const int   BottomButtonNumber =   3;
        public const float TitleHeight        =  40f;
        public const float Gap                =  10f;
        public const float SmallGap           =   4f;
        public const float CheckboxSize       = Widgets.CheckboxSize;
        public const float BottomButtonGap    = WidgetRow.DefaultGap + WidgetRow.ButtonExtraSpace;

        private static Dictionary<string, string> cache;

        private static readonly Type[] trySetModArgs = new Type[] { typeof(ModMetaData) };
        private static Page_ModsConfig page;
        private static bool hasReqs;
        private static Traverse TrySetModActive;
        private static Traverse TrySetModInactive;

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Page_ModsConfig.DoWindowContents))]
        public static void DoWindowContents(Page_ModsConfig __instance, 
                                            Dictionary<string, string> ___truncatedStringCache,
                                            bool ___anyReqsCached) {
            if (page != __instance) {
                page = __instance;
                TrySetModActive   = Traverse.Create(page).Method("TrySetModActive",   trySetModArgs);
                TrySetModInactive = Traverse.Create(page).Method("TrySetModInactive", trySetModArgs);
            }
            cache = ___truncatedStringCache;
            hasReqs = ___anyReqsCached;
        }

        [HarmonyPrefix]
        [HarmonyPatch("DrawRequirementEntry")]
        public static void DrawRequirementEntry(ModRequirement entry, Rect entryRect) {
            var mod = ModDependencyInfo.ModFor(entry as ModDependency);
            if (mod != null) {
                Rect icon = new Rect(entryRect.xMax - CheckboxSize - SmallGap, entryRect.y + 1f, CheckboxSize, CheckboxSize);
                if (Widgets.ButtonInvisible(icon)) {
                    ToggleMod(mod, !mod.Active);
                }
                ModDependency_Patches.InIcon = Mouse.IsOver(icon);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("DoBottomButtons")]
        public static void DoBottomButtons(Rect r) {
            var xAdj = BottomButtonNumber * (BottomButtonWidth + BottomButtonGap);
            Rect buttonRect = new Rect(r.x + xAdj, r.y, ActivateAllWidth, BottomButtonHeight);
            Color color = GUI.color;
            bool active = ModDependencyInfo.AnyToActivate;
            if (!active) {
                var dimmed = color * 0.4f;
                dimmed.a = color.a;
                GUI.color = dimmed;
            }
            if (Widgets.ButtonText(buttonRect, Strings.ActivateAll, doMouseoverSound: active, active: active)) {
                bool change = false;
                foreach (var info in ModDependencyInfo.ToActivate) {
                    if (ActivateAllFor(info)) change = true;
                }
                if (change) {
                    cache.Clear();
                    ModsConfig.TrySortMods();
                }
            }
            ModDependencyInfo.ClearUnfulfilled();
            GUI.color = color;
        }

        [HarmonyPostfix]
        [HarmonyPatch("DoModInfo")]
        public static void DoModInfo(Rect r, ModMetaData mod) {
            if (mod.Official) return;

            float stepOver = TitleHeight;

            // Calculate image height
            Texture2D image = mod.PreviewImage;
            if (image != null) {
                float maxHeight = Mathf.Ceil(r.height * 0.35f);
                float width = Mathf.Min(image.width, r.width);
                float height = Mathf.Min(image.height * (width / image.width), maxHeight);
                stepOver += height + Gap;
            }

            // Do requirements
            if (hasReqs) {
                var dep = ModDependencyInfo.For(mod);
                if (!dep.DeepAllActive) {
                    var font = Text.Font;
                    Text.Font = GameFont.Medium;
                    var titleSize = Text.CalcSize(mod.Name);

                    Text.Font = GameFont.Small;
                    if (ActivateMissing(r.xMax, r.y + stepOver, dep, r.width - titleSize.x)) {
                        cache.Clear();
                    }
                    Text.Font = font;
                }
            }
        }

        private static bool ActivateMissing(float x, float y, ModDependencyInfo dependencies, float titleSpace) {
            var size = Text.CalcSize(Strings.Activate);
            size.x += WidgetRow.ButtonExtraSpace;
            size.y += WidgetRow.LabelGap;
            bool below = titleSpace - SmallGap < size.x;
            y += below ? size.y + CheckboxSize + 1f : -WidgetRow.LabelGap;
            Rect rect = Anchored(below ? Anchor.TR : Anchor.BR, x, y, size.x, size.y);
            bool change = false;
            if (Widgets.ButtonText(rect, Strings.Activate)) {
                change = ActivateAllFor(dependencies);
            }
            return change;
        }

        private static bool ActivateAllFor(ModDependencyInfo dependencies) {
            bool change = false;
            foreach (var dep in dependencies.DeepInactive) {
                var info = ModDependencyInfo.For(dep);
                if (info != null && info.Installed && !info.Active) {
                    ToggleMod(info.Mod, true, false);
                    change = true;
                }
            }

            return change;
        }

        private static Rect Anchored(Anchor anchor, float x, float y, float width, float height) {
            x -= (((int) anchor) % 3) * 0.5f * width;
            y -= (((int) anchor) / 3) * 0.5f * height;
            return new Rect(x, y, width, height);
        }

        private static void ToggleMod(ModMetaData mod, bool active, bool clearCache = true) {
            var activeStr = active ? "active" : "inactive";
            Log.Message($"Set {mod.Name} to {activeStr}");
            var func = active ? TrySetModActive : TrySetModInactive;
            func.GetValue(new object[] { mod });
            if (clearCache) cache?.Clear();
        }
    }
}
