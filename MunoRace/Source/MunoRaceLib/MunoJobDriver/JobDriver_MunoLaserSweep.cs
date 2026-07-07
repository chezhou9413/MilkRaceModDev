using MunoRaceLib.MunoComp;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace MunoRaceLib.MunoJobDriver
{
    //负责让激光增幅横扫能力调用武器上的 Beam 横扫 Verb。
    public class JobDriver_MunoLaserSweep : JobDriver_CastVerbOnce
    {
        //构建横扫能力的移动、暖机和 Beam 发射流程。
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => job.verbToUse == null || !(job.verbToUse is Verb_MunoLaserSweep));
            this.FailOn(() => job.ability != null && !job.ability.CanCast && !job.ability.Casting);

            yield return Toils_Combat.GotoCastPosition(TargetIndex.A, TargetIndex.B);
            yield return Toils_Combat.CastVerb(TargetIndex.A, TargetIndex.B, canHitNonTargetPawns: false);
        }

        //通知能力已进入施放流程，保持原版能力暖机状态一致。
        public override void Notify_Starting()
        {
            base.Notify_Starting();
            job.ability?.Notify_StartedCasting();
        }
    }
}
