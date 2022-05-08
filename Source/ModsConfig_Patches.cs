using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Steam;

namespace ActivateDependencies
{
    public enum Anchor { TL, TC, TR, CL, C, CR, BL, BC, BR }

    [HarmonyPatch(typeof(Page_ModsConfig))]
    public static class ModsConfig_Patches
    {
        public const float Margin = 17f;
        public const float ModListAreaWidth = 350f;
        public const float ModAreaX = ModListAreaWidth + Margin;
        public const float ModAreaY = 0f;
        public const float ModAreaWidthAdj = ModAreaX;
        public const float ModAreaHeightAdj = Page.BottomButHeight + Margin;
        public const float ListAreaHeightAdj = ModAreaHeightAdj + 7f;
        public const float ListButtonHeight = 30f;
        public const float ListButtonWidth = 316f;
        public const float TitleHeight = 40f;
        public const float SteamButtonsHeight = 25f;
        public const float ButtonHeight = WidgetRow.IconSize;
        public const float ButtonExtraWidth = WidgetRow.ButtonExtraSpace + WidgetRow.DefaultGap;
        public const float Gap = 10f;
        public const float CheckboxGap = 4f;
        public const float CheckboxSize = Widgets.CheckboxSize;

        private static Dictionary<string, string> cache;

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Page_ModsConfig.DoWindowContents))]
        public static void DoWindowContents(
            Rect rect, Page_ModsConfig __instance, Dictionary<string, string> ___truncatedModNamesCache, bool ___anyReqsCached)
        {
            cache = ___truncatedModNamesCache;
            bool hasReqs = ___anyReqsCached;
            ModMetaData mod = __instance.selectedMod;
            if (mod != null && !mod.Official)
            {
                // Controls in the selected mod area
                Rect modArea = new Rect(ModAreaX, ModAreaY, rect.width - ModAreaWidthAdj, rect.height - ModAreaHeightAdj);
                if (FindCrossPromotionMods.IsCrossPromotion(mod))
                {
                    DoCrossPromotionContent(modArea, mod);
                }
                else
                {
                    DoNormalContent(modArea, mod, hasReqs);
                }
            }

            ActivateAllButton(rect);
        }

        [HarmonyPrefix]
        [HarmonyPatch("DrawRequirementEntry")]
        public static void DrawRequirementEntry(ModRequirement entry, Rect entryRect)
        {
            var mod = ModDependencyInfo.ModFor(entry as ModDependency);
            if (mod != null)
            {
                Rect icon = new Rect(entryRect.xMax - CheckboxSize - CheckboxGap, entryRect.y + 1f, CheckboxSize, CheckboxSize);
                if (Widgets.ButtonInvisible(icon))
                {
                    ToggleMod(mod, !mod.Active);
                }
                ModDependency_Patches.InIcon = Mouse.IsOver(icon);
            }
        }

        private static void DoNormalContent(Rect modArea, ModMetaData mod, bool hasReqs)
        {
            float stepOver = TitleHeight + Gap;

            // Calculate image height
            Texture2D image = mod.PreviewImage;
            if (image != null)
            {
                float maxHeight = Mathf.Min(Mathf.Ceil(modArea.height * 0.37f), 300f);
                float width = Mathf.Min(image.width, modArea.width);
                float height = Mathf.Min(image.height * (width / image.width), maxHeight);
                stepOver += height;
            }
            else
            {
                stepOver += 20f;
            }

            // Position us below image
            Rect lowerModArea = modArea;
            lowerModArea.yMin += stepOver;
            Widgets.BeginGroup(lowerModArea);
            stepOver = 0f;

            Text.Font = GameFont.Small;
            bool steamButtons = SteamManager.Initialized && mod.OnSteamWorkshop;

            // Add "Active" check box
            if (steamButtons) stepOver += SteamButtonsHeight;
            if (!mod.IsCoreMod) stepOver += Text.LineHeight;
            var anchor = hasReqs ? Anchor.TC : Anchor.BC;
            ActiveCheck(modArea.width / 2, stepOver, anchor, mod);

            // Do requirements
            if (hasReqs)
            {
                var dep = ModDependencyInfo.For(mod);
                if (!dep.DeepAllActive)
                {
                    float x = steamButtons ? modArea.width - SteamButtonsWidth : modArea.width / 2;
                    anchor = steamButtons ? Anchor.TR : Anchor.TC;
                    if (ActivateMissing(x, 0f, anchor, dep))
                    {
                        cache.Clear();
                    }
                }
            }

            Widgets.EndGroup();
        }

        private static void DoCrossPromotionContent(Rect modArea, ModMetaData mod)
        {
            modArea.width = modArea.width * 2 / 3 - 20f;
            modArea.yMin += modArea.width * mod.PreviewImage.height / mod.PreviewImage.width + 6f;
            ActiveCheck(modArea.x, modArea.y, Anchor.TL, mod);
        }

        private static void ActiveCheck(float x, float y, Anchor anchor, ModMetaData mod)
        {
            bool active = mod.Active;
            Text.Anchor = TextAnchor.UpperLeft;
            float activeWidth = Text.CalcSize(Strings.Active).x + CheckboxGap + CheckboxSize;
            Rect rect = Anchored(anchor, x, y, activeWidth, CheckboxSize);
            Widgets.CheckboxLabeled(rect, Strings.Active, ref active);
            if (active != mod.Active) ToggleMod(mod, active);
        }

        private static bool ActivateMissing(float x, float y, Anchor anchor, ModDependencyInfo dependencies)
        {
            var size = Text.CalcSize(Strings.Activate);
            Rect rect = Anchored(anchor, x, y, size.x + WidgetRow.ButtonExtraSpace, size.y + WidgetRow.LabelGap);
            bool change = false;
            if (Widgets.ButtonText(rect, Strings.Activate))
            {
                change = ActivateAllFor(dependencies);
            }
            return change;
        }

        private static void ActivateAllButton(Rect rect)
        {
            Rect buttonRect = new Rect(rect.x + Margin, rect.yMax - ListAreaHeightAdj, ListButtonWidth, ListButtonHeight);
            Color color = GUI.color;
            bool active = ModDependencyInfo.AnyToActivate;
            if (!active)
            {
                var dimmed = color * 0.4f;
                dimmed.a = color.a;
                GUI.color = dimmed;
            }
            if (Widgets.ButtonText(buttonRect, Strings.ActivateAll, doMouseoverSound: active, active: active))
            {
                bool change = false;
                foreach (var info in ModDependencyInfo.ToActivate)
                {
                    if (ActivateAllFor(info)) change = true;
                }
                if (change)
                {
                    cache.Clear();
                    ModsConfig.TrySortMods();
                }
            }
            ModDependencyInfo.ClearUnfulfilled();
            GUI.color = color;
        }

        private static bool ActivateAllFor(ModDependencyInfo dependencies)
        {
            bool change = false;
            foreach (var dep in dependencies.DeepInactive)
            {
                var info = ModDependencyInfo.For(dep);
                if (info != null && info.Installed && !info.Active)
                {
                    ToggleMod(info.Mod, true, false);
                    change = true;
                }
            }

            return change;
        }

        private static Rect Anchored(Anchor anchor, float x, float y, float width, float height)
        {
            x -= (((int) anchor) % 3) * 0.5f * width;
            y -= (((int) anchor) / 3) * 0.5f * height;
            return new Rect(x, y, width, height);
        }

        private static float SteamButtonsWidth => 
            2f * ButtonExtraWidth + Text.CalcSize("Unsubscribe".Translate()).x + Text.CalcSize("WorkshopPage".Translate()).x;

        private static void ToggleMod(ModMetaData mod, bool active, bool clearCache = true)
        {
            if (active)
            {
                foreach (ModMetaData item in ModsConfig.ActiveModsInLoadOrder)
                {
                    if (item.PackageIdNonUnique.Equals(mod.PackageIdNonUnique, StringComparison.InvariantCultureIgnoreCase))
                    {
                        Find.WindowStack.Add(new Dialog_MessageBox("MessageModWithPackageIdAlreadyEnabled".Translate(mod.PackageIdPlayerFacing, item.Name)));
                        return;
                    }
                }
            }

            mod.Active = active;
            if (clearCache) cache?.Clear();
        }
    }
}
