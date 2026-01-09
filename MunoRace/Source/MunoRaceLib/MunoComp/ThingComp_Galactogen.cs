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
    public class ThingCompProperties_Galactogen : CompProperties
    {
        public float maxGalactogen = 50f;
        public float minGalactogen = 0f;
        public float houseGalactogen = 2f;
        public string GalactogenUIName;
        public string GalactogenUIDes;
        public ThingCompProperties_Galactogen()
        {
            this.compClass = typeof(ThingComp_Galactogen);
        }
    }
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
        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            this.MaxGalactogen = Props.maxGalactogen;
            this.MinGalactogen = Props.minGalactogen;
            this.HouseGalactogen = Props.houseGalactogen;
        }

        public override void CompTick()
        {
            base.CompTick();
            //一小时执行一次
            if (parent.IsHashIntervalTick(2500))
            {
                CheckGalactogen();
            }
        }

        //用于获取乳源制删除的实际数量
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

        private void CheckGalactogen()
        {
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

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra())
            {
                yield return g;
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
