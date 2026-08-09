using MunoRaceLib.MunoComp;
using MunoRaceLib.MunoDefRef;
using MunoRaceLib.Tool;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace MunoRaceLib.MunoJobDriver
{
    //执行未穿戴自动汲取装备时的传统缪诺乳收集工作，并把产物生成在脚下。
    public class JobDriver_SpawnMunoMilk : JobDriver
    {
        private const int DurationTicks = 150;

        //传统挤奶只操作执行者自身，不需要额外预留。
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        //生成等待表现与最终乳源质扣除、物品产出及心情记忆流程。
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => pawn.Drafted || !pawn.Spawned || pawn.Downed);
            //设置执行位置在原地
            this.FailOnDespawnedOrNull(TargetIndex.A);
            Toil waitToil = Toils_General.Wait(DurationTicks);
            waitToil.WithProgressBarToilDelay(TargetIndex.A);
            waitToil.handlingFacing = true;
            waitToil.tickAction = () =>
            {
                if (pawn.IsHashIntervalTick(5))
                {
                    FilthGalactogenTool.SpawnMilkSplatter(pawn.Position.ToVector3Shifted(), pawn.Map, 4);
                }
                if (pawn.IsHashIntervalTick(30))
                {
                    if (Rand.Chance(0.4f))
                    {
                        FilthGalactogenTool.SpawnFilthGalactogen(pawn);
                    }
                }
            };
            yield return waitToil;
            //产生物品
            Toil spawnToil = Toils_General.Do(() =>
            {
                ThingComp_Galactogen galactogen = pawn.GetComp<ThingComp_Galactogen>();

                // 检查组件还在，且当前位置有效
                if (galactogen != null && pawn.Map != null)
                {
                    ThingDef itemDef = MunoDefDataRef.MunoRace_MunoMilk;
                    int producedCount = (int)galactogen.ReMoveAutoGalactogen(job.count);
                    if (producedCount <= 0)
                    {
                        return;
                    }

                    Thing item = ThingMaker.MakeThing(itemDef);
                    item.stackCount = producedCount;
                    //在Pawn当前位置生成奶
                    GenSpawn.Spawn(item, pawn.Position, pawn.Map);
                    MunoMilkingUtility.GiveMilkingMood(pawn);
                    Messages.Message(pawn.LabelShort + "产生了" + MunoDefDataRef.MunoRace_MunoMilk.label, pawn, MessageTypeDefOf.PositiveEvent);
                }
            });
            yield return spawnToil;
        }
    }
}
