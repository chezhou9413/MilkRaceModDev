using MunoRaceLib.MunoDefRef;
using RimWorld;
using Verse;

namespace MunoRaceLib.MunoWorld
{
    /// <summary>
    /// 负责根据和亲接受奖励与虐待惩罚，为缪诺派系提供自然好感倾向偏移。
    /// </summary>
    public class MunoMarriageGoodwillSituationWorker : GoodwillSituationWorker
    {
        /// <summary>
        /// 根据和亲外交存档状态返回缪诺派系对玩家的自然好感偏移。
        /// </summary>
        public override int GetNaturalGoodwillOffset(Faction other)
        {
            if (other?.def != MunoDefDataRef.MunoColony_Faction || Current.Game == null)
            {
                return 0;
            }

            MunoMarriageDiplomacyComponent component = Current.Game.GetComponent<MunoMarriageDiplomacyComponent>();
            if (component == null)
            {
                return 0;
            }

            int offset = component.NaturalGoodwillRewardActive ? 20 : 0;
            if (component.NaturalGoodwillPenaltyActive)
            {
                offset -= 30;
            }

            return offset;
        }
    }
}
