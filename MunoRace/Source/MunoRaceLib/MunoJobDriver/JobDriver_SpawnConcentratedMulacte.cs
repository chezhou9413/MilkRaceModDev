using MunoRaceLib.MunoComp;
using MunoRaceLib.MunoDefRef;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace MunoRaceLib.MunoJobDriver
{
    public class JobDriver_SpawnConcentratedMulacte : JobDriver
    {
        private const int DurationTicks = 200;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => pawn.Drafted || !pawn.Spawned || pawn.Downed);
            //设置执行位置在原地
            this.FailOnDespawnedOrNull(TargetIndex.A);
            Toil waitToil = Toils_General.Wait(DurationTicks);
            //显示进度条
            waitToil.WithProgressBarToilDelay(TargetIndex.A);
            //角色面向方向
            waitToil.handlingFacing = true;
            yield return waitToil;
            //产生物品
            Toil spawnToil = Toils_General.Do(() =>
            {
            ThingComp_Galactogen galactogen = pawn.GetComp<ThingComp_Galactogen>();

                // 检查组件还在，且当前位置有效
                if (galactogen != null && pawn.Map != null)
                {
                    ThingDef itemDef = MunoDefDataRef.MunoRace_ConcentratedMulacte;
                    Thing item = ThingMaker.MakeThing(itemDef);
                    item.stackCount = this.job.count;
                    int removedAmount = (int)galactogen.ReMoveAutoGalactogen(100);
                    if (removedAmount >= 100)
                    {
                        GenSpawn.Spawn(item, pawn.Position, pawn.Map);
                        Messages.Message(pawn.LabelShort + "产生了" + MunoDefDataRef.MunoRace_ConcentratedMulacte.label, pawn, MessageTypeDefOf.PositiveEvent);
                    }
                    else
                    {
                        item.Destroy();
                        galactogen.updateGalactogen(removedAmount);
                        Messages.Message(pawn.LabelShort + "的乳源制不足，无法产生" + MunoDefDataRef.MunoRace_ConcentratedMulacte.label, pawn, MessageTypeDefOf.NegativeEvent);
                    }
                }
            });
            yield return spawnToil;
        }
    }
}
