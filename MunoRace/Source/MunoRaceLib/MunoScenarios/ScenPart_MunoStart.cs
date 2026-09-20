using System;
using System.Linq;
using RimWorld;
using Verse;

namespace MunoRaceLib.MunoScenarios
{
    //负责生成可在准备页面重随机的固定构成人员，并初始化开局剧情。
    public class ScenPart_MunoStart : ScenPart_ConfigPage_ConfigureStartingPawnsBase
    {
        public MunoStartMode mode;

        //根据剧情模式确定实际登场人数。
        protected override int TotalPawnCount => mode == MunoStartMode.Expedition ? 3
            : mode == MunoStartMode.SurvivalShow ? 5 : 2;

        //生成每个位置的人员请求，让原版重随机保留种族和性别约束。
        protected override void GenerateStartingPawns()
        {
            pawnChoiceCount = TotalPawnCount;
            Find.GameInitData.allowedDevelopmentalStages = DevelopmentalStage.Adult;
            StartingPawnUtility.ClearAllStartingPawns();
            for (int i = 0; i < TotalPawnCount; i++)
            {
                bool muno = mode == MunoStartMode.Expedition || i == 0;
                PawnKindDef kind = muno
                    ? DefDatabase<PawnKindDef>.GetNamed(mode == MunoStartMode.Expedition
                        ? "MunoRace_Colonist" : "MunoRace_StorySurvivor")
                    : PawnKindDefOf.Colonist;
                Gender? gender = muno ? Gender.Female
                    : mode == MunoStartMode.SurvivalShow ? (Gender?)Gender.Male : null;
                PawnGenerationRequest request = new PawnGenerationRequest(kind, Faction.OfPlayer,
                    PawnGenerationContext.PlayerStarter, forceGenerateNewPawn: true,
                    allowPregnant: false, allowAddictions: false, fixedGender: gender,
                    biologicalAgeRange: new FloatRange(20f, 35f), dontGiveWeapon: true);
                StartingPawnUtility.SetGenerationRequest(i, request);
                StartingPawnUtility.AddNewPawn(i);
            }
        }

        //在地图生成前检查准备页面最终人员，避免外部编辑悄悄破坏开局组成。
        public override void PreMapGenerate()
        {
            var pawns = Find.GameInitData.startingAndOptionalPawns.Take(Find.GameInitData.startingPawnCount).ToList();
            int munoCount = pawns.Count(MunoStoryUtility.IsMuno);
            int expectedMuno = mode == MunoStartMode.Expedition ? 3 : 1;
            if (pawns.Count != TotalPawnCount || munoCount != expectedMuno
                || pawns.Any(p => !p.DevelopmentalStage.Adult())
                || pawns.Any(p => !MunoStoryUtility.IsMuno(p) && p.def != ThingDefOf.Human)
                || (mode == MunoStartMode.SurvivalShow
                    && pawns.Any(p => !MunoStoryUtility.IsMuno(p) && (p.def != ThingDefOf.Human || p.gender != Gender.Male))))
                throw new InvalidOperationException("缪诺开局人员不符合剧本要求，请恢复规定的人数、种族和性别。");
        }

        //记录实际开局时刻、逃亡者名单和指定派系的外交关系。
        public override void PostGameStart()
        {
            Current.Game.GetComponent<MunoStoryComponent>().Begin(mode,
                Find.GameInitData.startingAndOptionalPawns.Take(Find.GameInitData.startingPawnCount).ToList());
        }

        //在开局摘要中展示固定人员构成。
        public override string Summary(Scenario scen)
        {
            return mode == MunoStartMode.Expedition ? "三名成年缪诺开拓者。"
                : mode == MunoStartMode.SurvivalShow ? "一名成年缪诺与四名成年男性人类。"
                : "一名成年缪诺与一名成年人类同伴。";
        }

        //保存自定义剧本及存档中的开局类型。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref mode, "mode");
        }
    }
}
