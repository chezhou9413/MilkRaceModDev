using MunoRaceLib.MunoDefRef;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MunoRaceLib.MunoWorld
{
    /// <summary>
    /// 负责生成和清理和亲候选公主 Pawn。
    /// </summary>
    public static class MunoMarriageCandidateUtility
    {
        /// <summary>
        /// 生成一个新的和亲公主候选人。
        /// </summary>
        public static Pawn GeneratePrincessPawn()
        {
            Faction munoFaction = Find.FactionManager.FirstFactionOfDef(MunoDefDataRef.MunoColony_Faction);
            PawnGenerationRequest request = new PawnGenerationRequest(
                MunoDefDataRef.MunoRace_MarriagePrincess,
                munoFaction,
                PawnGenerationContext.NonPlayer,
                -1,
                forceGenerateNewPawn: true,
                allowDead: false,
                allowDowned: false,
                canGeneratePawnRelations: false,
                mustBeCapableOfViolence: false,
                colonistRelationChanceFactor: 0f);

            Pawn pawn = PawnGenerator.GeneratePawn(request);
            EnsureHighSkills(pawn);
            return pawn;
        }

        /// <summary>
        /// 确保和亲公主至少拥有三项高水平可用技能。
        /// </summary>
        private static void EnsureHighSkills(Pawn pawn)
        {
            if (pawn?.skills == null)
            {
                return;
            }

            List<SkillRecord> validSkills = pawn.skills.skills.Where(skill => skill != null && !skill.TotallyDisabled).ToList();
            for (int i = 0; i < 3 && validSkills.Count > 0; i++)
            {
                SkillRecord skill = validSkills.RandomElement();
                validSkills.Remove(skill);
                skill.Level = Rand.RangeInclusive(14, 18);
                skill.passion = Rand.Chance(0.35f) ? Passion.Major : Passion.Minor;
                skill.xpSinceLastLevel = 0f;
            }
        }

        /// <summary>
        /// 清理未被接受的临时候选 Pawn。
        /// </summary>
        public static void CleanupGeneratedCandidates(List<MunoMarriageCandidateRecord> records)
        {
            if (records == null)
            {
                return;
            }

            for (int i = 0; i < records.Count; i++)
            {
                Pawn pawn = records[i]?.pawn;
                if (pawn == null || pawn.Destroyed)
                {
                    continue;
                }

                if (Find.WorldPawns.Contains(pawn))
                {
                    Find.WorldPawns.RemoveAndDiscardPawnViaGC(pawn);
                }
                else if (!pawn.Spawned)
                {
                    pawn.Destroy();
                }
            }
        }
    }
}
