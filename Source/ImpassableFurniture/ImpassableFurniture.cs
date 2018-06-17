using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ImpassableFurniture
{
    internal class ImpassableSettings : ModSettings
    {
        public List<ThingDef> enabledDefList = new List<ThingDef>();

        public override void ExposeData()
        {
            base.ExposeData();
            List<string> list = this.enabledDefList?.Select(selector: td => td.defName).ToList() ?? new List<string>();
            Scribe_Collections.Look(list: ref list, label: "enabledDefList");
            this.enabledDefList = list.Select(selector: DefDatabase<ThingDef>.GetNamedSilentFail).Where(predicate: td => td != null).ToList();
        }
    }

    [StaticConstructorOnStartup]
    internal class ImpassableFurnitureMod : Mod
    {
        public static ImpassableFurnitureMod instance;
        private ImpassableSettings settings;
        private Vector2 leftScrollPosition;
        private Vector2 rightScrollPosition;
        private string searchTerm = "";
        private ThingDef leftSelectedDef;
        private ThingDef rightSelectedDef;

        public ImpassableFurnitureMod(ModContentPack content) : base(content: content) => instance = this;

        internal ImpassableSettings Settings
        {
            get => this.settings ?? (this.settings = this.GetSettings<ImpassableSettings>());
            set => this.settings = value;
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect: inRect);
            Text.Font = GameFont.Medium;
            Rect topRect = inRect.TopPart(pct: 0.05f);
            this.searchTerm = Widgets.TextField(rect: topRect.RightPart(pct: 0.95f).LeftPart(pct: 0.95f), text: this.searchTerm);
            Rect labelRect = inRect.TopPart(pct: 0.1f).BottomHalf();
            Rect bottomRect = inRect.BottomPart(pct: 0.9f);

            #region leftSide
            Rect leftRect = bottomRect.LeftHalf().RightPart(pct: 0.9f).LeftPart(pct: 0.9f);
            GUI.BeginGroup(position: leftRect, style: new GUIStyle(other: GUI.skin.box));
            List<ThingDef> found = DefDatabase<ThingDef>.AllDefs.Where(predicate: td => td.building != null && !td.label.Contains(value: "(building)") &&
                td.passability != Traversability.Impassable && (td.defName.Contains(value: this.searchTerm) || td.label.Contains(value: this.searchTerm)) && !this.Settings.enabledDefList.Contains(item: td)).OrderBy(keySelector: td => td.LabelCap ?? td.defName).ToList();
            float num = 3f;
            Widgets.BeginScrollView(outRect: leftRect.AtZero(), scrollPosition: ref this.leftScrollPosition, viewRect: new Rect(x: 0f, y: 0f, width: leftRect.width / 10 * 9, height: found.Count * 32f));
            if (!found.NullOrEmpty())
            {
                foreach (ThingDef def in found)
                {
                    Rect rowRect = new Rect(x: 5, y: num, width: leftRect.width - 6, height: 30);
                    Widgets.DrawHighlightIfMouseover(rect: rowRect);
                    if (def == this.leftSelectedDef)
                        Widgets.DrawHighlightSelected(rect: rowRect);
                    Widgets.Label(rect: rowRect, label: def.LabelCap ?? def.defName);
                    if (Widgets.ButtonInvisible(butRect: rowRect))
                        this.leftSelectedDef = def;

                    num += 32f;
                }
            }
            Widgets.EndScrollView();
            GUI.EndGroup();
            #endregion


            #region rightSide

            Widgets.Label(rect: labelRect.RightHalf().RightPart(pct: 0.9f), label: "Enable Impassability for:");
            Rect rightRect = bottomRect.RightHalf().RightPart(pct: 0.9f).LeftPart(pct: 0.9f);
            GUI.BeginGroup(position: rightRect, style: GUI.skin.box);
            num = 6f;
            Widgets.BeginScrollView(outRect: rightRect.AtZero(), scrollPosition: ref this.rightScrollPosition, viewRect: new Rect(x: 0f, y: 0f, width: rightRect.width / 5 * 4, height: this.Settings.enabledDefList.Count * 32f));
            if (!this.Settings.enabledDefList.NullOrEmpty())
            {
                foreach (ThingDef def in this.Settings.enabledDefList.Where(predicate: def => (def.defName.Contains(value: this.searchTerm) || def.label.Contains(value: this.searchTerm))))
                {
                    Rect rowRect = new Rect(x: 5, y: num, width: leftRect.width - 6, height: 30);
                    Widgets.DrawHighlightIfMouseover(rect: rowRect);
                    if (def == this.rightSelectedDef)
                        Widgets.DrawHighlightSelected(rect: rowRect);
                    Widgets.Label(rect: rowRect, label: def.LabelCap ?? def.defName);
                    if (Widgets.ButtonInvisible(butRect: rowRect))
                        this.rightSelectedDef = def;

                    num += 32f;
                }
            }
            Widgets.EndScrollView();
            GUI.EndGroup();
            #endregion


            #region buttons
            if (Widgets.ButtonImage(butRect: bottomRect.BottomPart(pct: 0.6f).TopPart(pct: 0.1f).RightPart(pct: 0.525f).LeftPart(pct: 0.1f), tex: TexUI.ArrowTexRight) && this.leftSelectedDef != null)
            {
                this.Settings.enabledDefList.Add(item: this.leftSelectedDef);
                this.Settings.enabledDefList = this.Settings.enabledDefList.OrderBy(keySelector: td => td.LabelCap ?? td.defName).ToList();
                this.rightSelectedDef = this.leftSelectedDef;
                this.leftSelectedDef = null;
                ImpassableFurniture.AddPassability(def: this.rightSelectedDef);
            }
            if (Widgets.ButtonImage(butRect: bottomRect.BottomPart(pct: 0.4f).TopPart(pct: 0.15f).RightPart(pct: 0.525f).LeftPart(pct: 0.1f), tex: TexUI.ArrowTexLeft) && this.rightSelectedDef != null)
            {
                this.Settings.enabledDefList.Remove(item: this.rightSelectedDef);
                this.leftSelectedDef = this.rightSelectedDef;
                this.rightSelectedDef = null;
                ImpassableFurniture.RemovePassability(def: this.leftSelectedDef);
            }
            #endregion

            this.Settings.Write();
        }

        public override string SettingsCategory() => "Impassable Furniture";
    }

    [StaticConstructorOnStartup]
    internal static class ImpassableFurniture
    {
        static ImpassableFurniture()
        {
            if (ImpassableFurnitureMod.instance.Settings.enabledDefList.NullOrEmpty())
            {
                ImpassableFurnitureMod.instance.Settings.enabledDefList = DefDatabase<ThingDef>.AllDefsListForReading.Where(predicate: td => td.building != null && td.passability == Traversability.Impassable).ToList();
            }
            else
            {
                DefDatabase<ThingDef>.AllDefsListForReading.ForEach(action: td =>
                {
                    if (td.building != null && td.passability == Traversability.Impassable)
                        RemovePassability(def: td);
                });
                ImpassableFurnitureMod.instance.Settings.enabledDefList.ForEach(action: AddPassability);
            }
        }

        public static void AddPassability(ThingDef def)
        {
            def.passability = Traversability.Impassable;
            Regenerate();
        }

        public static void RemovePassability(ThingDef def)
        {
            if (def.passability != Traversability.Impassable) return;
            def.passability = Traversability.PassThroughOnly;
            Regenerate();
        }

        private static void Regenerate()
        {
            if (Current.ProgramState != ProgramState.Playing) return;
            foreach (Map map in Find.Maps)
            {
                map.pathGrid.RecalculateAllPerceivedPathCosts();
                map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
            }
        }
    }
}
