using System;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace MunoRaceLib.MunoQuests
{
    //生成具有稳定战斗技能的缪诺突袭队员，并施加不会立即致死或断肢的随机伤势。
    public static class MunoRaiderUtility
    {
        //使用专用成年战斗模板生成受保护人物。
        public static Pawn Generate(Faction faction, Map map, MunoQuestConfig config)
        {
            Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                DefDatabase<PawnKindDef>.GetNamed("MunoRace_QuestRaider"), faction, tile: map.Tile,
                forceGenerateNewPawn: true, canGeneratePawnRelations: false, mustBeCapableOfViolence: true,
                allowPregnant: false, allowAddictions: false, fixedBiologicalAge: Rand.Range(22, 38),
                developmentalStages: DevelopmentalStage.Adult));
            try
            {
                pawn.skills.GetSkill(SkillDefOf.Shooting).Level = config.combatSkill.RandomInRange;
                pawn.skills.GetSkill(SkillDefOf.Melee).Level = config.combatSkill.RandomInRange;
                AddInjuries(pawn, config);
                return pawn;
            }
            catch
            {
                //人物尚未交给地图或任务持有，生成中断时只清理本次临时人物。
                pawn.Destroy(DestroyMode.Vanish);
                throw;
            }
        }

        //按原版伤害入口逐次添加有限伤势，预先排除会导致死亡或摧毁部位的伤害量。
        private static void AddInjuries(Pawn pawn, MunoQuestConfig config)
        {
            int count = config.injuryCount.RandomInRange;
            bool oldForceDowned = pawn.health.forceDowned;
            pawn.health.forceDowned = true;
            try
            {
                for (int i = 0; i < count; i++)
                {
                    var parts = pawn.health.hediffSet.GetNotMissingParts()
                        .Where(p => p.depth == BodyPartDepth.Outside && pawn.health.hediffSet.GetPartHealth(p) > 8f).ToList();
                    if (parts.Count == 0) break;
                    BodyPartRecord part = parts.RandomElement();
                    float factor = pawn.GetStatValue(StatDefOf.IncomingDamageFactor);
                    if (factor <= 0f) throw new InvalidOperationException("突袭队员无法承受用于任务初始化的伤害。");
                    float damage = Mathf.Min(config.injuryDamage.RandomInRange,
                        (pawn.health.hediffSet.GetPartHealth(part) - 4f) / factor);
                    DamageDef type = Rand.Bool ? DamageDefOf.Cut : DamageDefOf.Blunt;
                    HediffDef hediff = HealthUtility.GetHediffDefFromDamage(type, pawn, part);
                    if (pawn.health.WouldDieAfterAddingHediff(hediff, part, damage * factor)) continue;
                    DamageInfo info = new DamageInfo(type, damage, 999f, -1f, null, part);
                    info.SetAllowDamagePropagation(false);
                    pawn.TakeDamage(info);
                }
            }
            finally { pawn.health.forceDowned = oldForceDowned; }
        }
    }
}
