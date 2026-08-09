using MunoRaceLib.MunoDefRef;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MunoRaceLib.MunoComp
{
    //定义乳源质组件的基础容量、恢复速度与界面文本配置。
    public class ThingCompProperties_Galactogen : CompProperties
    {
        public float maxGalactogen = 50f;
        public float minGalactogen = 0f;
        public float houseGalactogen = 2f;
        public string GalactogenUIName;
        public string GalactogenUIDes;

        //构造乳源质组件属性，并绑定实际组件类型。
        public ThingCompProperties_Galactogen()
        {
            compClass = typeof(ThingComp_Galactogen);
        }
    }

    //保存缪诺角色的乳源质资源、自动收集阈值和自动汲取模式，并提供相关操作接口。
    public class ThingComp_Galactogen : ThingComp
    {
        public float MaxGalactogen = 50f;
        public float MinGalactogen = 0f;
        public float HouseGalactogen = 2f;
        public float CurrentGalactogen;
        public float AutoGather = 0.8f;
        public bool autoCollectEnabled = true;
        public GalactogenAutoCollectMode autoCollectMode = GalactogenAutoCollectMode.Milk;

        //返回当前乳源质组件所属的小人。
        public Pawn SelfPawn => parent as Pawn;

        //返回当前乳源质组件使用的 XML 属性。
        public ThingCompProperties_Galactogen Props => (ThingCompProperties_Galactogen)props;

        //初始化乳源质组件的基础数值。
        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            MaxGalactogen = Props.maxGalactogen;
            MinGalactogen = Props.minGalactogen;
            HouseGalactogen = Props.houseGalactogen;
        }

        //按固定周期刷新乳源质自然增减逻辑。
        public override void CompTick()
        {
            base.CompTick();
            if (parent.IsHashIntervalTick(2500))
            {
                CheckGalactogen();
            }
        }

        //按指定数量移除乳源质，并返回本次实际扣除数值。
        public float ReMoveAutoGalactogen(float count)
        {
            float previousValue = CurrentGalactogen;
            CurrentGalactogen = Mathf.Max(MinGalactogen, CurrentGalactogen - count);
            return previousValue - CurrentGalactogen;
        }

        //直接增减当前乳源质，并保证结果始终落在允许区间内。
        public void updateGalactogen(float value)
        {
            CurrentGalactogen = Mathf.Clamp(CurrentGalactogen + value, MinGalactogen, MaxGalactogen * 1.2f);
        }

        //判断指定工作是否属于乳源质收集流程。
        public static bool IsCollectionJob(JobDef jobDef)
        {
            return jobDef == MunoDefDataRef.JobDriver_SpawnMunoMilk
                || jobDef == MunoDefDataRef.JobDriver_SpawnConcentratedMulacte
                || jobDef == MunoDefDataRef.JobDef_AutoExtractMunoMilk
                || jobDef == MunoDefDataRef.JobDef_AutoExtractConcentratedMulacte;
        }

        //生成乳源质状态、启停、阈值、模式切换和手动浓浆收集命令。
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }

            if (ShouldShowThresholdSlider())
            {
                yield return new MunoGizmo.Gizmo_GalactogenBar(SelfPawn);
            }

            bool hasExtractor = GalactogenExtractorUtility.HasActiveExtractor(SelfPawn);
            yield return BuildAutoCollectToggle(hasExtractor);
            if (hasExtractor)
            {
                yield return BuildModeCommand();
            }

            yield return BuildManualSlurryCommand();
        }

        //保存和读取乳源质当前状态、阈值、启停状态与自动汲取模式。
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref MaxGalactogen, "MaxGalactogen", 50f);
            Scribe_Values.Look(ref MinGalactogen, "MinGalactogen", 0f);
            Scribe_Values.Look(ref HouseGalactogen, "HouseGalactogen", 2f);
            Scribe_Values.Look(ref CurrentGalactogen, "CurrentGalactogen", 0f);
            Scribe_Values.Look(ref AutoGather, "AutoGather", 0.8f);
            Scribe_Values.Look(ref autoCollectEnabled, "autoCollectEnabled", true);
            Scribe_Values.Look(ref autoCollectMode, "autoCollectMode", GalactogenAutoCollectMode.Milk);
        }

        //根据饱食度与属性加成刷新乳源质的自然恢复或反向消耗。
        private void CheckGalactogen()
        {
            MaxGalactogen = Props.maxGalactogen + SelfPawn.GetStatValue(MunoDefDataRef.Muno_MaxGalactogen);
            HouseGalactogen = Props.houseGalactogen + SelfPawn.GetStatValue(MunoDefDataRef.Muno_GalactogenRecovery);
            if (SelfPawn.needs?.food == null)
            {
                return;
            }

            float foodPercentage = SelfPawn.needs.food.CurLevelPercentage;
            if (foodPercentage > 0.25f)
            {
                updateGalactogen(HouseGalactogen);
                return;
            }

            if (CurrentGalactogen > 0f)
            {
                updateGalactogen(-10f);
                SelfPawn.needs.food.CurLevelPercentage += 0.01f;
            }
        }

        //返回当前 Pawn 是否应该显示乳源质阈值滑条。
        private bool ShouldShowThresholdSlider()
        {
            if (SelfPawn == null || Find.Selector.NumSelected >= 2)
            {
                return false;
            }

            return SelfPawn.Faction == Faction.OfPlayer && SelfPawn.HostFaction == null && !SelfPawn.IsSlave;
        }

        //创建自动收集启停按钮，并根据是否穿戴汲取装备显示当前产物。
        private Command_Toggle BuildAutoCollectToggle(bool hasExtractor)
        {
            string product = autoCollectMode == GalactogenAutoCollectMode.Milk ? "缪诺乳" : "乳源质浓浆";
            return new Command_Toggle
            {
                defaultLabel = hasExtractor ? "自动汲取：" + product : "自动收集缪诺乳",
                defaultDesc = hasExtractor
                    ? "开启后，乳源质超过设定阈值时自动汲取为" + product + "。"
                    : "开启后，乳源质达到设定阈值时自动转换为缪诺乳。",
                icon = ProductIcon(hasExtractor ? autoCollectMode : GalactogenAutoCollectMode.Milk),
                isActive = () => autoCollectEnabled,
                toggleAction = () => autoCollectEnabled = !autoCollectEnabled
            };
        }

        //创建缪诺乳与乳源质浓浆之间的自动汲取模式切换按钮。
        private Command_Action BuildModeCommand()
        {
            GalactogenAutoCollectMode nextMode = autoCollectMode == GalactogenAutoCollectMode.Milk
                ? GalactogenAutoCollectMode.ConcentratedSlurry
                : GalactogenAutoCollectMode.Milk;
            string currentName = autoCollectMode == GalactogenAutoCollectMode.Milk ? "缪诺乳" : "乳源质浓浆";
            string nextName = nextMode == GalactogenAutoCollectMode.Milk ? "缪诺乳" : "乳源质浓浆";
            return new Command_Action
            {
                defaultLabel = "汲取模式：" + currentName,
                defaultDesc = "当前自动汲取产物为" + currentName + "。点击切换为" + nextName + "。",
                icon = ProductIcon(nextMode),
                action = () => autoCollectMode = nextMode
            };
        }

        //创建立即消耗一百乳源质并在脚下生成一份浓浆的手动命令。
        private Command_Action BuildManualSlurryCommand()
        {
            bool collecting = IsCollectionJob(SelfPawn.CurJobDef);
            return new Command_Action
            {
                defaultLabel = "收集乳源质浓浆",
                defaultDesc = "将 100 点乳源质转换为 1 份乳源质浓浆。",
                icon = ProductIcon(GalactogenAutoCollectMode.ConcentratedSlurry),
                Disabled = collecting || SelfPawn.Downed || SelfPawn.Drafted || CurrentGalactogen < 100f,
                disabledReason = collecting
                    ? "当前正在收集乳源质"
                    : SelfPawn.Downed ? "小人已倒下" : SelfPawn.Drafted ? "小人已被征召" : "乳源质不足",
                action = delegate
                {
                    Job job = JobMaker.MakeJob(MunoDefDataRef.JobDriver_SpawnConcentratedMulacte, SelfPawn);
                    job.count = 1;
                    SelfPawn.jobs.StartJob(job, JobCondition.InterruptForced);
                }
            };
        }

        //返回指定自动汲取模式对应的产物图标。
        private static Texture2D ProductIcon(GalactogenAutoCollectMode mode)
        {
            string path = mode == GalactogenAutoCollectMode.Milk
                ? "Item/Resource/MunoRace_MunoMilk/MunoRace_MunoMilk"
                : "Item/Resource/MunoRace_ConcentratedMulacte/MunoRace_ConcentratedMulacte";
            return ContentFinder<Texture2D>.Get(path, true);
        }
    }
}
