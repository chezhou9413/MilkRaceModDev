using MunoRaceLib.MunoDefRef;
using RimWorld;
using RuntimeAudioClipLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MunoRaceLib.MunoComp
{
    /// <summary>
    /// 定义乳源质组件的基础容量、恢复速度与界面文本配置。
    /// </summary>
    public class ThingCompProperties_Galactogen : CompProperties
    {
        public float maxGalactogen = 50f;
        public float minGalactogen = 0f;
        public float houseGalactogen = 2f;
        public string GalactogenUIName;
        public string GalactogenUIDes;

        /// <summary>
        /// 构造乳源质组件属性，并绑定实际组件类型。
        /// </summary>
        public ThingCompProperties_Galactogen()
        {
            this.compClass = typeof(ThingComp_Galactogen);
        }
    }

    /// <summary>
    /// 保存缪诺角色的乳源质资源、自动收集阈值，并提供相关 Gizmo 与消耗接口。
    /// </summary>
    public class ThingComp_Galactogen : ThingComp
    {
        public float MaxGalactogen = 50f;
        public float MinGalactogen = 0f;
        public float HouseGalactogen = 2f;
        public float CurrentGalactogen = 0f;
        public float AutoGather = 0.8f;
        public Pawn SelfPawn => parent as Pawn;
        public ThingCompProperties_Galactogen Props => (ThingCompProperties_Galactogen)this.props;
        public bool autoCollectEnabled = true;

        /// <summary>
        /// 初始化乳源质组件的基础数值。
        /// </summary>
        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            this.MaxGalactogen = Props.maxGalactogen;
            this.MinGalactogen = Props.minGalactogen;
            this.HouseGalactogen = Props.houseGalactogen;
        }

        /// <summary>
        /// 按固定周期刷新乳源质自然增减逻辑。
        /// </summary>
        public override void CompTick()
        {
            base.CompTick();
            //一小时执行一次
            if (parent.IsHashIntervalTick(2500))
            {
                CheckGalactogen();
            }
        }

        /// <summary>
        /// 按指定数量移除乳源质，并返回本次实际扣除的数值。
        /// </summary>
        public float ReMoveAutoGalactogen(float count)
        {
            float previousValue = CurrentGalactogen;
            CurrentGalactogen -= count;
            if (CurrentGalactogen < MinGalactogen)
            {
                CurrentGalactogen = MinGalactogen;
            }
            float actualRemoved = previousValue - CurrentGalactogen;
            return actualRemoved;
        }

        /// <summary>
        /// 根据饱食度与属性加成刷新乳源质的自然恢复或反向消耗。
        /// </summary>
        private void CheckGalactogen()
        {
            this.MaxGalactogen = Props.maxGalactogen + SelfPawn.GetStatValue(MunoDefDataRef.Muno_MaxGalactogen);
            this.HouseGalactogen = Props.houseGalactogen + SelfPawn.GetStatValue(MunoDefDataRef.Muno_GalactogenRecovery);

            if (SelfPawn.needs != null && SelfPawn.needs.food != null)
            {
                float curPct = SelfPawn.needs.food.CurLevelPercentage;   // 当前饱食度百分比 (0.0 - 1.0)
                if (curPct > 0.25f)
                {
                    updateGalactogen(HouseGalactogen);
                    return;
                }
                if (curPct <= 0.25f && CurrentGalactogen > 0)
                {
                    updateGalactogen(-10);
                    SelfPawn.needs.food.CurLevelPercentage += 0.01f;
                    return;
                }
            }
        }

        /// <summary>
        /// 直接增减当前乳源质，并保证结果始终落在允许区间内。
        /// </summary>
        public void updateGalactogen(float value)
        {
            this.CurrentGalactogen += value;
            if (CurrentGalactogen < MinGalactogen)
            {
                this.CurrentGalactogen = MinGalactogen;
            }
            if (CurrentGalactogen > MaxGalactogen * 1.2f)
            {
                this.CurrentGalactogen = MaxGalactogen * 1.2f;
            }
        }

        /// <summary>
        /// 返回当前 Pawn 是否应该显示乳源质阈值滑条。
        /// </summary>
        private bool ShouldShowThresholdSlider()
        {
            if (SelfPawn == null || Find.Selector.NumSelected >= 2)
            {
                return false;
            }

            return SelfPawn.Faction == Faction.OfPlayer && SelfPawn.HostFaction == null && !SelfPawn.IsSlave;
        }

        /// <summary>
        /// 生成乳源质相关命令与阈值滑条，供玩家直接操作。
        /// </summary>
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra())
            {
                yield return g;
            }

            if (ShouldShowThresholdSlider())
            {
                yield return new MunoGizmo.Gizmo_GalactogenBar(SelfPawn);
            }

            // 创建切换按钮
            yield return new Command_Toggle
            {
                // 按钮标题
                defaultLabel = "自动收集缪诺乳",
                defaultDesc = "开启后，当乳原制超过阈值时，会自动消耗乳原制转换为缪诺乳",
                icon = ContentFinder<Texture2D>.Get("Item/Resource/MunoRace_MunoMilk/MunoRace_MunoMilk", true),
                isActive = () => autoCollectEnabled,
                toggleAction = () =>
                {
                    autoCollectEnabled = !autoCollectEnabled;
                }
            };
            // 创建收集浓浆按钮
            yield return new Command_Action
            {
                defaultLabel = "收集乳原质浓浆",
                defaultDesc = "将100乳原质转换为1的乳原质浓浆",
                icon = ContentFinder<Texture2D>.Get("Item/Resource/MunoRace_ConcentratedMulacte/MunoRace_ConcentratedMulacte", true),
                Disabled = (SelfPawn.CurJobDef == MunoDefDataRef.JobDriver_SpawnMunoMilk||SelfPawn.Downed||SelfPawn.Drafted||CurrentGalactogen < 100),
                disabledReason = SelfPawn.CurJobDef == MunoDefDataRef.JobDriver_SpawnMunoMilk? "当前正在收集乳原制           " : SelfPawn.Downed?"小人已倒下":SelfPawn.Drafted ? "小人已被征召":"乳源质不足",
                action = () =>
                {
                    Job job = JobMaker.MakeJob(MunoDefDataRef.JobDriver_SpawnConcentratedMulacte, SelfPawn);
                    job.count = 1;
                    SelfPawn.jobs.StartJob(job, JobCondition.InterruptForced);
                }
            };
        }

        /// <summary>
        /// 负责存读乳源质当前状态与自动收集阈值。
        /// </summary>
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref MaxGalactogen, "MaxGalactogen", 50f);
            Scribe_Values.Look(ref MinGalactogen, "MinGalactogen", 0f);
            Scribe_Values.Look(ref HouseGalactogen, "HouseGalactogen", 2f);
            Scribe_Values.Look(ref CurrentGalactogen, "CurrentGalactogen", 0f);
            Scribe_Values.Look(ref AutoGather, "AutoGather", 0.8f);
            Scribe_Values.Look(ref autoCollectEnabled, "autoCollectEnabled", true);
        }
    }
}
