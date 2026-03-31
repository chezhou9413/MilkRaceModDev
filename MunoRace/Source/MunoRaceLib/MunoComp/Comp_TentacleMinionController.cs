using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace MunoRaceLib.MunoComp
{
    public class CompProperties_TentacleMinionController : CompProperties
    {
        public int lifetimeTicks = 90000;
        public float enemySearchRadius = 40f;

        public CompProperties_TentacleMinionController()
        {
            compClass = typeof(Comp_TentacleMinionController);
        }
    }

    public class Comp_TentacleMinionController : ThingComp
    {
        private int spawnedTick = -1;
        private int nextTargetScanTick;

        private CompProperties_TentacleMinionController Props
        {
            get { return (CompProperties_TentacleMinionController)props; }
        }

        private Pawn Pawn
        {
            get { return parent as Pawn; }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                spawnedTick = Find.TickManager.TicksGame;
                nextTargetScanTick = spawnedTick + 15;
            }

            Pawn pawn = Pawn;
            if (pawn != null && Faction.OfPlayer != null && pawn.Faction != Faction.OfPlayer)
            {
                pawn.SetFaction(Faction.OfPlayer);
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            Pawn pawn = Pawn;
            if (pawn == null || pawn.Destroyed || pawn.Dead || pawn.Map == null)
            {
                return;
            }

            if (spawnedTick < 0)
            {
                spawnedTick = Find.TickManager.TicksGame;
            }

            if (Find.TickManager.TicksGame - spawnedTick >= Props.lifetimeTicks)
            {
                pawn.Destroy(DestroyMode.Vanish);
                return;
            }

            if (Find.TickManager.TicksGame >= nextTargetScanTick)
            {
                nextTargetScanTick = Find.TickManager.TicksGame + 45;
                TryAttackNearestEnemy(pawn);
            }
        }

        private void TryAttackNearestEnemy(Pawn pawn)
        {
            if (pawn.Downed || pawn.stances == null || pawn.jobs == null || pawn.mindState == null)
            {
                return;
            }

            Pawn enemy = FindNearestHostilePawn(pawn);
            if (enemy == null)
            {
                return;
            }

            if (pawn.CurJobDef == JobDefOf.AttackMelee && pawn.CurJob != null && pawn.CurJob.targetA.Thing == enemy)
            {
                return;
            }

            Job attackJob = JobMaker.MakeJob(JobDefOf.AttackMelee, enemy);
            attackJob.expiryInterval = 300;
            attackJob.checkOverrideOnExpire = true;
            attackJob.canBashDoors = true;
            attackJob.canBashFences = true;
            attackJob.playerForced = true;
            attackJob.locomotionUrgency = LocomotionUrgency.Sprint;
            attackJob.killIncappedTarget = true;
            pawn.mindState.enemyTarget = enemy;
            pawn.jobs.StartJob(attackJob, JobCondition.InterruptForced);
        }

        private Pawn FindNearestHostilePawn(Pawn pawn)
        {
            float bestDist = float.MaxValue;
            Pawn bestTarget = null;
            IReadOnlyList<Pawn> pawns = pawn.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn candidate = pawns[i];
                if (candidate == pawn || candidate.Dead || candidate.Downed || candidate.Faction == null)
                {
                    continue;
                }

                if (!candidate.HostileTo(pawn))
                {
                    continue;
                }

                float dist = pawn.Position.DistanceToSquared(candidate.Position);
                if (dist > Props.enemySearchRadius * Props.enemySearchRadius)
                {
                    continue;
                }

                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestTarget = candidate;
                }
            }

            return bestTarget;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref spawnedTick, "spawnedTick", -1);
            Scribe_Values.Look(ref nextTargetScanTick, "nextTargetScanTick", 0);
        }
    }
}
