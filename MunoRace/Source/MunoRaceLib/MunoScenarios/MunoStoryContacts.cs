using System.Collections.Generic;
using MunoRaceLib.MunoEndings;
using MunoRaceLib.MunoRecording;
using MunoRaceLib.MunoWorld;
using RimWorld;
using Verse;

namespace MunoRaceLib.MunoScenarios
{
    //组织聚落常规事务、录像兑换和主动结局的通讯入口。
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
            bool hostile = faction.HostileTo(Faction.OfPlayer);
            var options = new List<FloatMenuOption>
            {
                new FloatMenuOption(hostile ? "聚落事务（敌对期间暂停）" : "聚落事务", hostile ? (System.Action)null
                    : () => Find.WindowStack.Add(new Dialog_MunoMarriageComms(negotiator))),
                new FloatMenuOption(hostile ? "上交录像带（敌对期间暂停）" : "上交录像带", hostile ? (System.Action)null
                    : () => MunoRecordingExchange.Open(negotiator))
            };
            if (Current.Game.GetComponent<MunoStoryComponent>().mode != MunoStartMode.None)
                options.Add(new FloatMenuOption("申请融入缪诺聚落", () => MunoEndingService.Open(negotiator)));
            Find.WindowStack.Add(new FloatMenu(options));
        }
    }
}
