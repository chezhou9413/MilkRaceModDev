using System.Linq;
using LudeonTK;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace MunoRaceLib.MunoQuests
{
    //在开发者动作菜单中强制发出军事任务邀请或推进当前等待阶段。
    public static class MunoQuestDebugActions
    {
        //强制创建哨站抓捕邀请，保留场地与派系前提。
        [DebugAction("缪诺任务", "发出哨站抓捕邀请", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void OfferCapture()
        {
            Offer("Muno_CaptureOutpost");
        }

        //强制创建受伤突袭队员保护邀请。
        [DebugAction("缪诺任务", "发出伤员保护邀请", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void OfferProtection()
        {
            Offer("Muno_ProtectRaider");
        }

        //通过原版可用任务接口发送邀请，不自动接受也不授予头衔。
        private static void Offer(string name)
        {
            Slate slate = new Slate();
            slate.Set("map", Find.CurrentMap);
            slate.Set("points", StorytellerUtility.DefaultThreatPointsNow(Find.CurrentMap));
            slate.Set("munoDebug", true);
            if (!MunoQuestUtility.CanOffer(name, slate, out string reason)) { MunoQuestUtility.Reject(reason); return; }
            if (name == "Muno_CaptureOutpost" && !MunoQuestUtility.FindSiteTile(MunoQuestUtility.HomeMap(slate), out _))
            { MunoQuestUtility.Reject("基地附近没有可达的哨站地块。"); return; }
            Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(DefDatabase<QuestScriptDef>.GetNamed(name), slate);
            if (quest == null) { MunoQuestUtility.Reject("任务生成失败，请查看日志中的完整异常。"); return; }
            QuestUtility.SendLetterQuestAvailable(quest);
        }

        //提前触发带回目标后正在等待的接收穿梭机。
        [DebugAction("缪诺任务", "立即派出抓捕接收穿梭机", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void CallCaptureShuttle()
        {
            QuestPart_MunoCapture part = FindMission<QuestPart_MunoCapture>();
            if (part == null) MunoQuestUtility.Reject("没有进行中的缪诺哨站抓捕任务。");
            else part.DebugCallShuttle();
        }

        //提前触发当前保护任务的一波正式追兵。
        [DebugAction("缪诺任务", "立即生成保护任务追兵", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SpawnPursuers()
        {
            QuestPart_MunoProtect part = FindMission<QuestPart_MunoProtect>();
            if (part == null) MunoQuestUtility.Reject("没有进行中的缪诺伤员保护任务。");
            else part.DebugSpawnRaid();
        }

        //查找尚未结束且已经接受的指定任务控制器。
        private static T FindMission<T>() where T : QuestPart_MunoMilitary
        {
            return Find.QuestManager.QuestsListForReading.Where(q => q.State == QuestState.Ongoing)
                .SelectMany(q => q.PartsListForReading).OfType<T>().FirstOrDefault();
        }
    }
}
