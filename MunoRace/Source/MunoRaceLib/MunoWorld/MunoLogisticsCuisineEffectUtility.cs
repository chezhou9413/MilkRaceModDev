using MunoRaceLib.MunoDefRef;
using RimWorld;
using Verse;

namespace MunoRaceLib.MunoWorld
{
    //负责为吃下分子料理的小人抽取并应用随机效果。
    public static class MunoLogisticsCuisineEffectUtility
    {
        //按当前概率表为指定小人应用一项分子料理效果，并返回给玩家看的结果文本。
        public static string ApplyRandomEffect(Pawn pawn, bool boosted)
        {
            if (pawn == null)
            {
                return "没有有效的试吃者，料理效果没有结算。";
            }

            float roll = Rand.Value;
            if (boosted)
            {
                return ApplyBoostedEffect(pawn, roll);
            }

            return ApplyNormalEffect(pawn, roll);
        }

        //按普通概率表应用料理效果。
        private static string ApplyNormalEffect(Pawn pawn, float roll)
        {
            if (roll < 0.40f)
            {
                GiveThought(pawn, MunoDefDataRef.Muno_MolecularCuisineDelicious);
                return "心情 +8：美味的分子料理。";
            }

            if (roll < 0.80f)
            {
                GiveThought(pawn, MunoDefDataRef.Muno_MolecularCuisineAwful);
                return "心情 -10：难吃的分子料理。";
            }

            if (roll < 0.96f)
            {
                GiveHediff(pawn, MunoDefDataRef.Muno_MolecularCuisineConsciousnessUp);
                return "五天内意识 +10%。";
            }

            if (roll < 0.99f)
            {
                GiveHediff(pawn, MunoDefDataRef.Muno_MolecularCuisineConsciousnessDown);
                return "五天内意识 -10%。";
            }

            GiveHediff(pawn, MunoDefDataRef.Muno_MolecularCuisineConsciousnessPermanent);
            return "永久性意识 +15%。";
        }

        //按强化概率表应用料理效果。
        private static string ApplyBoostedEffect(Pawn pawn, float roll)
        {
            if (roll < 0.45f)
            {
                GiveThought(pawn, MunoDefDataRef.Muno_MolecularCuisineDelicious);
                return "强化概率生效，心情 +8：美味的分子料理。";
            }

            if (roll < 0.80f)
            {
                GiveThought(pawn, MunoDefDataRef.Muno_MolecularCuisineAwful);
                return "强化概率生效，心情 -10：难吃的分子料理。";
            }

            if (roll < 0.98f)
            {
                GiveHediff(pawn, MunoDefDataRef.Muno_MolecularCuisineConsciousnessUp);
                return "强化概率生效，五天内意识 +10%。";
            }

            if (roll < 0.99f)
            {
                GiveHediff(pawn, MunoDefDataRef.Muno_MolecularCuisineConsciousnessDown);
                return "强化概率生效，五天内意识 -10%。";
            }

            GiveHediff(pawn, MunoDefDataRef.Muno_MolecularCuisineConsciousnessPermanent);
            return "强化概率生效，永久性意识 +15%。";
        }

        //给有心情需求的小人添加分子料理心情记忆。
        private static void GiveThought(Pawn pawn, ThoughtDef thoughtDef)
        {
            if (thoughtDef == null)
            {
                return;
            }

            pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(thoughtDef);
        }

        //给小人添加分子料理健康效果。
        private static void GiveHediff(Pawn pawn, HediffDef hediffDef)
        {
            if (hediffDef == null)
            {
                return;
            }

            Hediff hediff = HediffMaker.MakeHediff(hediffDef, pawn);
            pawn.health.AddHediff(hediff);
        }
    }
}
