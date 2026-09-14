using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace DeepBlueYieldPatchSetting
{
    public class DeepBlueYieldPatchSetting : ModSettings
    {
        public int deepBlueExtracterCrudeSpeed = 1;
        public int deepBlueExtracterSpeed = 2;
        public int deepBlueExtracterAbysstechSpeed = 4;
        public int deepblueStationSpawnCount = 24;
        public bool abyssteelAirtight = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref deepBlueExtracterCrudeSpeed, "deepBlueExtracterCrudeSpeed", 1);
            Scribe_Values.Look(ref deepBlueExtracterSpeed, "deepBlueExtracterSpeed", 2);
            Scribe_Values.Look(ref deepBlueExtracterAbysstechSpeed, "deepBlueExtracterAbysstechSpeed", 4);
            Scribe_Values.Look(ref deepblueStationSpawnCount, "deepblueStationSpawnCount", 24);
            deepblueStationSpawnCount = Mathf.Max(1, deepblueStationSpawnCount);
            Scribe_Values.Look(ref abyssteelAirtight, "abyssteelAirtight", true);
        }
    }

    public class DeepBlueYieldPatch : Mod
    {
        public static DeepBlueYieldPatchSetting settings;
        private const string DeepblueStationInputName = "DeepBlueStationSpawnCount";
        private string deepblueStationInputBuffer;
        private bool deepblueStationInputFocused;
        public DeepBlueYieldPatch(ModContentPack content) : base(content)
        {
            settings = GetSettings<DeepBlueYieldPatchSetting>();
        }

        public override string SettingsCategory()
        {
            return "We need more DeepBlue";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            // 1. 조잡한 딥블루 추출기
            listing.Label($"{"deepBlueExtracterCrudeSpeed".Translate()} {settings.deepBlueExtracterCrudeSpeed}");
            settings.deepBlueExtracterCrudeSpeed = (int)Mathf.Round(listing.Slider(settings.deepBlueExtracterCrudeSpeed, 1f, 10f));
            listing.Gap(8f);

            // 2. 딥블루 추출기
            listing.Label($"{"deepBlueExtracterSpeed".Translate()} {settings.deepBlueExtracterSpeed}");
            settings.deepBlueExtracterSpeed = (int)Mathf.Round(listing.Slider(settings.deepBlueExtracterSpeed, 1f, 10f));
            listing.Gap(8f);

            // 3. 어비스테크 딥블루 추출기
            listing.Label($"{"deepBlueExtracterAbysstechSpeed".Translate()} {settings.deepBlueExtracterAbysstechSpeed}");
            settings.deepBlueExtracterAbysstechSpeed = (int)Mathf.Round(listing.Slider(settings.deepBlueExtracterAbysstechSpeed, 1f, 10f));
            listing.Gap(16f);

            // 4. 딥블루 스테이션
            string deepblueStationLabel = "deepblueStationSpawnCount".Translate();
            Rect deepblueStationRect = listing.GetRect(28f);
            Rect deepblueStationLabelRect = deepblueStationRect.LeftPartPixels(Verse.Text.CalcSize(deepblueStationLabel).x);
            Widgets.Label(deepblueStationLabelRect, deepblueStationLabel);
            Rect deepblueStationInputRect = new Rect(deepblueStationLabelRect.xMax + 10f, deepblueStationRect.y, 80f, deepblueStationRect.height);

            if (deepblueStationInputBuffer == null)
                deepblueStationInputBuffer = settings.deepblueStationSpawnCount.ToString();

            bool wasFocused = deepblueStationInputFocused || GUI.GetNameOfFocusedControl() == DeepblueStationInputName;
            bool pressedEnter = Event.current.type == EventType.KeyDown
                && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);
            bool clickedOutside = Event.current.rawType == EventType.MouseDown
                && !deepblueStationInputRect.Contains(Event.current.mousePosition);
            if (wasFocused && (pressedEnter || clickedOutside))
            {
                CommitStationInput();
                if (GUI.GetNameOfFocusedControl() == DeepblueStationInputName)
                    GUI.FocusControl(null);
                if (pressedEnter)
                    Event.current.Use();
            }

            GUI.SetNextControlName(DeepblueStationInputName);
            deepblueStationInputBuffer = Widgets.TextField(deepblueStationInputRect, deepblueStationInputBuffer, 3);
            deepblueStationInputFocused = GUI.GetNameOfFocusedControl() == DeepblueStationInputName;
            if (wasFocused && !deepblueStationInputFocused)
                CommitStationInput();
            listing.Gap(16f);

            // 5. 어비스틸 진공밀폐여부
            string airtightLabel = "abyssteelAirtight".Translate();
            Rect airtightRect = listing.GetRect(28f);
            airtightRect.width = Mathf.Min(airtightRect.width, Verse.Text.CalcSize(airtightLabel).x + 10f + 24f);
            Widgets.CheckboxLabeled(airtightRect, airtightLabel, ref settings.abyssteelAirtight);

            listing.End();
            base.DoSettingsWindowContents(inRect);
        }

        private void CommitStationInput()
        {
            if (deepblueStationInputBuffer == null) return;

            if (string.IsNullOrWhiteSpace(deepblueStationInputBuffer))
                settings.deepblueStationSpawnCount = 1;
            else if (int.TryParse(deepblueStationInputBuffer, out int parsedValue))
                settings.deepblueStationSpawnCount = Mathf.Max(1, parsedValue);

            deepblueStationInputBuffer = settings.deepblueStationSpawnCount.ToString();
        }

        public override void WriteSettings()
        {
            CommitStationInput();
            deepblueStationInputBuffer = null;
            deepblueStationInputFocused = false;
            base.WriteSettings();
            DeepBlueYieldPatchUtility.ApplySettings();
        }
    }

    [StaticConstructorOnStartup]
    public static class DeepBlueYieldPatchInitializer
    {
        static DeepBlueYieldPatchInitializer()
        {
            DeepBlueYieldPatchUtility.ApplySettings();
        }

    }
    public static class DeepBlueYieldPatchUtility
    {
        public static void ApplySettings()
        {
            var s = DeepBlueYieldPatch.settings;
            if (s == null) return;

            ApplyExtractSpeed("Moyo2_DeepBlueExtracter_Crude", s.deepBlueExtracterCrudeSpeed);
            ApplyExtractSpeed("Moyo2_DeepBlueExtracter", s.deepBlueExtracterSpeed);
            ApplyExtractSpeed("Moyo2_DeepBlueExtracter_Abysstech", s.deepBlueExtracterAbysstechSpeed);
            ApplyStationSpawnCount(s.deepblueStationSpawnCount);
            ApplyAbyssteelAirtight(s.abyssteelAirtight);
        }

        private static void ApplyExtractSpeed(string defName, int value)
        {
            var extractDef = DefDatabase<HediffDef>.GetNamedSilentFail(defName);
            var speedStat = DefDatabase<StatDef>.GetNamedSilentFail("Moyo2_DeepblueStatSpeed");
            var statModifier = extractDef?.stages?.FirstOrDefault()?.statOffsets?.Find(s => s.stat == speedStat);
            if (statModifier != null)
                statModifier.value = value;
        }

        private static void ApplyStationSpawnCount(int value)
        {
            ThingDef stationDef = DefDatabase<ThingDef>.GetNamedSilentFail("Moyo2_DeepblueStation");
            if (stationDef != null)
            {
                var spawnerProps = stationDef.GetCompProperties<CompProperties_Spawner>();
                if (spawnerProps != null)
                    spawnerProps.spawnCount = Mathf.Max(1, value);
            }
        }

        private static void ApplyAbyssteelAirtight(bool value)
        {
            var stuffProps = DefDatabase<ThingDef>.GetNamedSilentFail("Moyo2_Abyssteel")?.stuffProps;
            if (stuffProps == null || stuffProps.isAirtight == value) return;

            stuffProps.isAirtight = value;

            if (Current.Game == null || !ModsConfig.OdysseyActive) return;
            foreach (var map in Find.Maps)
                map.GetComponent<VacuumComponent>()?.Dirty();
        }

    }
}
