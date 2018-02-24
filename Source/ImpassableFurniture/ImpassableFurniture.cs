using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ImpassableFurniture
{
    class ImpassableSettings : ModSettings
    {
        public List<ThingDef> enabledDefList = new List<ThingDef>();

        public override void ExposeData()
        {
            base.ExposeData();
            List<string> list = this.enabledDefList?.Select(td => td.defName).ToList() ?? new List<string>();
            Scribe_Collections.Look(ref list, "enabledDefList");
            this.enabledDefList = list.Select(s => DefDatabase<ThingDef>.GetNamedSilentFail(s)).OfType<ThingDef>().ToList();
        }
    }

    [StaticConstructorOnStartup]
    class ImpassableFurnitureMod : Mod
    {
        public static ImpassableFurnitureMod instance;
        ImpassableSettings settings;
        Vector2 leftScrollPosition;
        Vector2 rightScrollPosition;
        string searchTerm = "";
        ThingDef leftSelectedDef;
        ThingDef rightSelectedDef;

        public ImpassableFurnitureMod(ModContentPack content) : base(content) => instance = this;

        internal ImpassableSettings Settings
        {
            get => this.settings ?? (this.settings = GetSettings<ImpassableSettings>());
            set => this.settings = value;
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            Text.Font = GameFont.Medium;
            Rect topRect = inRect.TopPart(0.05f);
            this.searchTerm = Widgets.TextField(topRect.RightPart(0.95f).LeftPart(0.95f), this.searchTerm);
            Rect labelRect = inRect.TopPart(0.1f).BottomHalf();
            Rect bottomRect = inRect.BottomPart(0.9f);

            #region leftSide
            Rect leftRect = bottomRect.LeftHalf().RightPart(0.9f).LeftPart(0.9f);
            GUI.BeginGroup(leftRect, new GUIStyle(GUI.skin.box));
            List<ThingDef> found = DefDatabase<ThingDef>.AllDefs.Where(td => td.building != null && !td.label.Contains("(building)") &&
                td.passability != Traversability.Impassable && (td.defName.Contains(this.searchTerm) || td.label.Contains(this.searchTerm)) && !this.Settings.enabledDefList.Contains(td)).OrderBy(td => td.LabelCap ?? td.defName).ToList();
            float num = 3f;
            Widgets.BeginScrollView(leftRect.AtZero(), ref this.leftScrollPosition, new Rect(0f, 0f, leftRect.width / 10 * 9, found.Count() * 32f));
            if (!found.NullOrEmpty())
            {
                foreach (ThingDef def in found)
                {
                    Rect rowRect = new Rect(5, num, leftRect.width - 6, 30);
                    Widgets.DrawHighlightIfMouseover(rowRect);
                    if (def == this.leftSelectedDef)
                        Widgets.DrawHighlightSelected(rowRect);
                    Widgets.Label(rowRect, def.LabelCap ?? def.defName);
                    if (Widgets.ButtonInvisible(rowRect))
                        this.leftSelectedDef = def;

                    num += 32f;
                }
            }
            Widgets.EndScrollView();
            GUI.EndGroup();
            #endregion


            #region rightSide

            Widgets.Label(labelRect.RightHalf().RightPart(0.9f), "Enable Impassability for:");
            Rect rightRect = bottomRect.RightHalf().RightPart(0.9f).LeftPart(0.9f);
            GUI.BeginGroup(rightRect, GUI.skin.box);
            num = 6f;
            Widgets.BeginScrollView(rightRect.AtZero(), ref this.rightScrollPosition, new Rect(0f, 0f, rightRect.width / 5 * 4, this.Settings.enabledDefList.Count * 32f));
            if (!this.Settings.enabledDefList.NullOrEmpty())
            {
                foreach (ThingDef def in this.Settings.enabledDefList.Where(def => (def.defName.Contains(this.searchTerm) || def.label.Contains(this.searchTerm))))
                {
                    Rect rowRect = new Rect(5, num, leftRect.width - 6, 30);
                    Widgets.DrawHighlightIfMouseover(rowRect);
                    if (def == this.rightSelectedDef)
                        Widgets.DrawHighlightSelected(rowRect);
                    Widgets.Label(rowRect, def.LabelCap ?? def.defName);
                    if (Widgets.ButtonInvisible(rowRect))
                        this.rightSelectedDef = def;

                    num += 32f;
                }
            }
            Widgets.EndScrollView();
            GUI.EndGroup();
            #endregion


            #region buttons
            if (Widgets.ButtonImage(bottomRect.BottomPart(0.6f).TopPart(0.1f).RightPart(0.525f).LeftPart(0.1f), TexUI.ArrowTexRight) && this.leftSelectedDef != null)
            {
                this.Settings.enabledDefList.Add(this.leftSelectedDef);
                this.Settings.enabledDefList = this.Settings.enabledDefList.OrderBy(td => td.LabelCap ?? td.defName).ToList();
                this.rightSelectedDef = this.leftSelectedDef;
                this.leftSelectedDef = null;
                ImpassableFurniture.AddPassability(this.rightSelectedDef);
            }
            if (Widgets.ButtonImage(bottomRect.BottomPart(0.4f).TopPart(0.15f).RightPart(0.525f).LeftPart(0.1f), TexUI.ArrowTexLeft) && this.rightSelectedDef != null)
            {
                this.Settings.enabledDefList.Remove(this.rightSelectedDef);
                this.leftSelectedDef = this.rightSelectedDef;
                this.rightSelectedDef = null;
                ImpassableFurniture.RemovePassability(this.leftSelectedDef);
            }
            #endregion

            this.Settings.Write();
        }

        public override string SettingsCategory() => "Impassable Furniture";
    }

    [StaticConstructorOnStartup]
    static class ImpassableFurniture
    {
        static ImpassableFurniture()
        {

            DefDatabase<ThingDef>.AllDefsListForReading.ForEach(td =>
            {
                if (td.building != null && td.passability == Traversability.Impassable)
                    RemovePassability(td);
            });
            ImpassableFurnitureMod.instance.Settings.enabledDefList.ForEach(td => AddPassability(td));
        }

        public static void AddPassability(ThingDef def)
        {
            def.passability = Traversability.Impassable;
            Regenerate();
        }

        public static void RemovePassability(ThingDef def)
        {
            if (def.passability == Traversability.Impassable)
            {
                def.passability = Traversability.PassThroughOnly;
                Regenerate();
            }
        }

        private static void Regenerate()
        {
            if (Current.ProgramState == ProgramState.Playing)
            {
                foreach (Map map in Find.Maps)
                {
                    map.pathGrid.RecalculateAllPerceivedPathCosts();
                    map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
                }
            }
        }
    }
}
