using MunoRaceLib.MunoWorld;
using RimWorld;
using Verse;

namespace MunoRaceLib.MunoScenarios
{
    //直接进入聚落管理员的通讯终端。
    public static class MunoStoryContacts
    {
        //显示当前外交状态允许的聚落服务。
        public static void Open(Pawn negotiator)
        {
            Faction faction = MunoStoryUtility.Faction;
            if (negotiator?.Map == null || !negotiator.IsColonistPlayerControlled || negotiator.Downed
                || faction == null || faction.defeated || faction.deactivated)
            {
                Messages.Message("当前无法建立缪诺聚落通讯，请确认操作者与聚落仍然有效。", MessageTypeDefOf.RejectInput, false);
                return;
            }
            Find.WindowStack.Add(new Dialog_MunoMarriageComms(negotiator));
        }
    }
}
