using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace DeepBlueYieldPatchSetting
{
    /// <summary>모드에서 조정할 설정값 정의 및 ExposeData 재정의.</summary>
    public class DeepBlueYieldPatchSetting : ModSettings
    {
        public int deepBlueExtracterCrudeSpeed = 1;
        public int deepBlueExtracterSpeed = 2;
        public int deepBlueExtracterAbysstechSpeed = 4;
        public int deepblueStationSpawnCount = 24;
        public bool abyssteelAirtight = true;
        public bool moyoGeneWorkSpeedExclusionEnabled = false;

        /// <summary>설정값을 RimWorld에 저장 및 불러오는 메서드.</summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref deepBlueExtracterCrudeSpeed, "deepBlueExtracterCrudeSpeed", 1);
            Scribe_Values.Look(ref deepBlueExtracterSpeed, "deepBlueExtracterSpeed", 2);
            Scribe_Values.Look(ref deepBlueExtracterAbysstechSpeed, "deepBlueExtracterAbysstechSpeed", 4);
            Scribe_Values.Look(ref deepblueStationSpawnCount, "deepblueStationSpawnCount", 24);
            deepblueStationSpawnCount = Mathf.Max(1, deepblueStationSpawnCount);
            Scribe_Values.Look(ref abyssteelAirtight, "abyssteelAirtight", true);
            Scribe_Values.Look(ref moyoGeneWorkSpeedExclusionEnabled, "workSpeedGenesMutuallyExclusive", false);
        }
    }

    /// <summary>모드 main.</summary>
    public class DeepBlueYieldPatch : Mod
    {
        public static DeepBlueYieldPatchSetting settings;
        private const string DeepblueStationInputName = "DeepBlueStationSpawnCount";
        private const string CrudeExtracterDefName = "Moyo2_DeepBlueExtracter_Crude";
        private const string ExtracterDefName = "Moyo2_DeepBlueExtracter";
        private const string AbysstechExtracterDefName = "Moyo2_DeepBlueExtracter_Abysstech";
        private string deepblueStationInputBuffer;
        private bool deepblueStationInputFocused;

        /// <summary>모드 설정 불러오기.</summary>
        public DeepBlueYieldPatch(ModContentPack content) : base(content)
        {
            settings = GetSettings<DeepBlueYieldPatchSetting>();
        }

        /// <summary>모드 설정 목록에 표시될 라벨.</summary>
        public override string SettingsCategory()
        {
            return "We need more DeepBlue";
        }

        /// <summary>모드 설정창 그리는 곳.</summary>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            // 추출기
            if (DeepBlueYieldPatchUtility.HasDeepblueSpeedStat())
            {
                // 조잡한 딥블루 추출기
                if (DeepBlueYieldPatchUtility.HasExtracter(CrudeExtracterDefName))
                {
                    listing.Label($"{"deepBlueExtracterCrudeSpeed".Translate()} {settings.deepBlueExtracterCrudeSpeed}");
                    settings.deepBlueExtracterCrudeSpeed = (int)Mathf.Round(listing.Slider(settings.deepBlueExtracterCrudeSpeed, 1f, 10f));
                    listing.Gap(8f);
                }

                // 딥블루 추출기
                if (DeepBlueYieldPatchUtility.HasExtracter(ExtracterDefName))
                {
                    listing.Label($"{"deepBlueExtracterSpeed".Translate()} {settings.deepBlueExtracterSpeed}");
                    settings.deepBlueExtracterSpeed = (int)Mathf.Round(listing.Slider(settings.deepBlueExtracterSpeed, 1f, 10f));
                    listing.Gap(8f);
                }

                // 어비스 테크 딥블루 추출기
                if (DeepBlueYieldPatchUtility.HasExtracter(AbysstechExtracterDefName))
                {
                    listing.Label($"{"deepBlueExtracterAbysstechSpeed".Translate()} {settings.deepBlueExtracterAbysstechSpeed}");
                    settings.deepBlueExtracterAbysstechSpeed = (int)Mathf.Round(listing.Slider(settings.deepBlueExtracterAbysstechSpeed, 1f, 10f));
                    listing.Gap(16f);
                }
            }

            // 딥블루 스테이션
            if (DeepBlueYieldPatchUtility.CanConfigureStation())
            {
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
            }

            // 어비스틸
            if (DeepBlueYieldPatchUtility.CanConfigureAbyssteel())
            {
                string airtightLabel = "abyssteelAirtight".Translate();
                Rect airtightRect = listing.GetRect(28f);
                airtightRect.width = Mathf.Min(airtightRect.width, Verse.Text.CalcSize(airtightLabel).x + 10f + 24f);
                Widgets.CheckboxLabeled(airtightRect, airtightLabel, ref settings.abyssteelAirtight);
                listing.Gap(8f);
            }

            // 신중한일꾼, 효율적인 일꾼 유전자 배타성 추가
            if (DeepBlueYieldPatchUtility.CanConfigureMoyoGeneWorkSpeedExclusion())
            {
                string moyoGeneWorkSpeedExclusionLabel = "workSpeedGenesMutuallyExclusive".Translate();
                Rect moyoGeneWorkSpeedExclusionRect = listing.GetRect(28f);
                moyoGeneWorkSpeedExclusionRect.width = Mathf.Min(moyoGeneWorkSpeedExclusionRect.width, Verse.Text.CalcSize(moyoGeneWorkSpeedExclusionLabel).x + 10f + 24f);
                Widgets.CheckboxLabeled(moyoGeneWorkSpeedExclusionRect, moyoGeneWorkSpeedExclusionLabel, ref settings.moyoGeneWorkSpeedExclusionEnabled);
            }

            listing.End();
            base.DoSettingsWindowContents(inRect);
        }

        /// <summary>
        /// 딥블루 스테이션 설정값 확정하는 메서드 <br/>
        /// 입력 편의성을 위해서 포커스가 벗어나는 순간에만 설정값 확정
        /// </summary>
        private void CommitStationInput()
        {
            if (deepblueStationInputBuffer == null) return;

            if (string.IsNullOrWhiteSpace(deepblueStationInputBuffer))
                settings.deepblueStationSpawnCount = 1;
            else if (int.TryParse(deepblueStationInputBuffer, out int parsedValue))
                settings.deepblueStationSpawnCount = Mathf.Max(1, parsedValue);

            deepblueStationInputBuffer = settings.deepblueStationSpawnCount.ToString();
        }

        /// <summary>설정을 저장 및 인게임에 반영하는 메서드.</summary>
        public override void WriteSettings()
        {
            if (DeepBlueYieldPatchUtility.CanConfigureStation())
                CommitStationInput();
            deepblueStationInputBuffer = null;
            deepblueStationInputFocused = false;
            base.WriteSettings();
            DeepBlueYieldPatchUtility.ApplySettings();
        }
    }

    /// <summary>게임의 Def 로드가 완료된 뒤 저장된 설정을 처음 적용.</summary>
    [StaticConstructorOnStartup]
    public static class DeepBlueYieldPatchInitializer
    {
        /// <summary>초기 설정 적용.</summary>
        static DeepBlueYieldPatchInitializer()
        {
            DeepBlueYieldPatchUtility.ApplySettings();
        }

    }

    public static class DeepBlueYieldPatchUtility
    {
        private const string DeepblueSpeedStatDefName = "Moyo2_DeepblueStatSpeed";
        private const string DeepblueStationDefName = "Moyo2_DeepblueStation";
        private const string AbyssteelDefName = "Moyo2_Abyssteel";
        private const string MoyoGeneWorkSpeedExclusionSlowGeneDefName = "Moyo2_WorkSpeed_Slow";
        private const string MoyoGeneWorkSpeedExclusionFastGeneDefName = "Moyo2_WorkSpeed_Fast";
        private const string MoyoGeneWorkSpeedExclusionTag = "WeNeedMoreDeepBlue_WorkSpeed";

        /// <summary>Moyo2_DeepblueStatSpeed의 존재 여부를 반환합니다.</summary>
        public static bool HasDeepblueSpeedStat()
        {
            return DefDatabase<StatDef>.GetNamedSilentFail(DeepblueSpeedStatDefName) != null;
        }

        /// <summary>지정한 딥블루 추출기 Hediff Def 존재 여부를 반환.</summary>
        public static bool HasExtracter(string defName)
        {
            return DefDatabase<HediffDef>.GetNamedSilentFail(defName) != null;
        }

        /// <summary>딥블루 스테이션의 생산 수량을 변경 가능 여부 반환.</summary>
        public static bool CanConfigureStation()
        {
            return DefDatabase<ThingDef>.GetNamedSilentFail(DeepblueStationDefName)?.GetCompProperties<CompProperties_Spawner>() != null;
        }

        /// <summary>어비스틸 진공 밀폐 속성을 변경 가능 여부 반환.</summary>
        public static bool CanConfigureAbyssteel()
        {
            return DefDatabase<ThingDef>.GetNamedSilentFail(AbyssteelDefName)?.stuffProps != null;
        }

        /// <summary>유전자 상호 배타 설정 가능여부 반환.</summary>
        public static bool CanConfigureMoyoGeneWorkSpeedExclusion()
        {
            return DefDatabase<GeneDef>.GetNamedSilentFail(MoyoGeneWorkSpeedExclusionSlowGeneDefName) != null
                && DefDatabase<GeneDef>.GetNamedSilentFail(MoyoGeneWorkSpeedExclusionFastGeneDefName) != null;
        }

        /// <summary>인게임 적용 메서드.</summary>
        public static void ApplySettings()
        {
            var s = DeepBlueYieldPatch.settings;
            if (s == null) return;

            if (HasDeepblueSpeedStat())
            {
                ApplyExtractSpeed("Moyo2_DeepBlueExtracter_Crude", s.deepBlueExtracterCrudeSpeed);
                ApplyExtractSpeed("Moyo2_DeepBlueExtracter", s.deepBlueExtracterSpeed);
                ApplyExtractSpeed("Moyo2_DeepBlueExtracter_Abysstech", s.deepBlueExtracterAbysstechSpeed);
            }
            ApplyStationSpawnCount(s.deepblueStationSpawnCount);
            ApplyAbyssteelAirtight(s.abyssteelAirtight);
            ApplyMoyoGeneWorkSpeedExclusion(s.moyoGeneWorkSpeedExclusionEnabled);
        }

        /// <summary>추출기 인게임 적용 메서드.</summary>
        private static void ApplyExtractSpeed(string defName, int value)
        {
            if (!HasExtracter(defName)) return;

            var extractDef = DefDatabase<HediffDef>.GetNamedSilentFail(defName);
            var speedStat = DefDatabase<StatDef>.GetNamedSilentFail(DeepblueSpeedStatDefName);
            var statModifier = extractDef?.stages?.FirstOrDefault()?.statOffsets?.Find(s => s.stat == speedStat);
            if (statModifier != null)
                statModifier.value = value;
        }

        /// <summary>딥블루 스테이션 인게임 적용 메서드.</summary>
        private static void ApplyStationSpawnCount(int value)
        {
            if (!CanConfigureStation()) return;

            ThingDef stationDef = DefDatabase<ThingDef>.GetNamedSilentFail(DeepblueStationDefName);
            if (stationDef != null)
            {
                var spawnerProps = stationDef.GetCompProperties<CompProperties_Spawner>();
                if (spawnerProps != null)
                    spawnerProps.spawnCount = Mathf.Max(1, value);
            }
        }

        /// <summary>어비스틸 인게임 적용 메서드.</summary>
        private static void ApplyAbyssteelAirtight(bool value)
        {
            if (!CanConfigureAbyssteel()) return;

            var stuffProps = DefDatabase<ThingDef>.GetNamedSilentFail(AbyssteelDefName)?.stuffProps;
            if (stuffProps == null || stuffProps.isAirtight == value) return;

            stuffProps.isAirtight = value;

            if (Current.Game == null || !ModsConfig.OdysseyActive) return;
            foreach (var map in Find.Maps)
                map.GetComponent<VacuumComponent>()?.Dirty();
        }

        /// <summary>유전자 배타성 여부 인게임 적용 메서드.</summary>
        private static void ApplyMoyoGeneWorkSpeedExclusion(bool enabled)
        {
            if (!CanConfigureMoyoGeneWorkSpeedExclusion()) return;

            SetMoyoGeneWorkSpeedExclusionTag(DefDatabase<GeneDef>.GetNamedSilentFail(MoyoGeneWorkSpeedExclusionSlowGeneDefName), enabled);
            SetMoyoGeneWorkSpeedExclusionTag(DefDatabase<GeneDef>.GetNamedSilentFail(MoyoGeneWorkSpeedExclusionFastGeneDefName), enabled);
        }
        /// <summary>유전자 배타성 여부 인게임 적용 메서드.</summary>
        private static void SetMoyoGeneWorkSpeedExclusionTag(GeneDef geneDef, bool enabled)
        {
            if (geneDef == null) return;

            if (enabled)
            {
                if (geneDef.exclusionTags == null)
                    geneDef.exclusionTags = new List<string>();
                if (!geneDef.exclusionTags.Contains(MoyoGeneWorkSpeedExclusionTag))
                    geneDef.exclusionTags.Add(MoyoGeneWorkSpeedExclusionTag);
            }
            else
            {
                geneDef.exclusionTags?.Remove(MoyoGeneWorkSpeedExclusionTag);
            }
        }

    }
}
