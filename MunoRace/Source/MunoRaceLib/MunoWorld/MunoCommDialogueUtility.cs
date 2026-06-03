using System.Collections.Generic;
using Verse;

namespace MunoRaceLib.MunoWorld
{
    /// <summary>
    /// 负责从 XML 通讯文本池中选择随机文本，并在缺失时提供默认文本。
    /// </summary>
    public static class MunoCommDialogueUtility
    {
        private const string DialogueDefName = "Muno_CommDialogue";

        /// <summary>
        /// 随机返回首页问候文本。
        /// </summary>
        public static string RandomGreeting()
        {
            return RandomGreetingLine().text;
        }

        /// <summary>
        /// 随机返回首页问候文本与可选立绘覆盖。
        /// </summary>
        public static MunoCommDialogueResult RandomGreetingLine()
        {
            MunoCommDialogueDef def = Def();
            return RandomLineFromList(def?.greetingLines, def?.greetings, "缪诺通讯链路已经接通。\n\n这里是缪诺聚落管理终端，请选择本次通讯事项。");
        }

        /// <summary>
        /// 随机返回和亲管理员提示文本。
        /// </summary>
        public static string RandomManagerPrompt()
        {
            return RandomManagerPromptLine().text;
        }

        /// <summary>
        /// 随机返回和亲管理员提示文本与可选立绘覆盖。
        /// </summary>
        public static MunoCommDialogueResult RandomManagerPromptLine()
        {
            MunoCommDialogueDef def = Def();
            return RandomLineFromList(def?.managerPromptLines, def?.managerPrompts, "真是太好了，我们可以立刻安排。\n请选择一位和亲公主吧。");
        }

        /// <summary>
        /// 随机返回当前没有和亲请求时的回复文本。
        /// </summary>
        public static string RandomNoRequestReply()
        {
            return RandomNoRequestReplyLine().text;
        }

        /// <summary>
        /// 随机返回当前没有和亲请求时的回复文本与可选立绘覆盖。
        /// </summary>
        public static MunoCommDialogueResult RandomNoRequestReplyLine()
        {
            MunoCommDialogueDef def = Def();
            return RandomLineFromList(def?.noRequestReplyLines, def?.noRequestReplies, "当前没有和亲请求。请等待缪诺方面主动发来请求。");
        }

        /// <summary>
        /// 随机返回主动和亲请求蓝信封文本。
        /// </summary>
        public static string RandomRequestLetter()
        {
            return RandomFromList(Def()?.requestLetters, "缪诺方面发来正式和亲请求。\n\n她们希望你在 {DAYS} 天内使用通讯台联系缪诺势力，确认是否接受一名和亲公主。若逾期未联系，本次请求将视为拒绝。");
        }

        /// <summary>
        /// 随机返回主动请求超时后的自动拒绝文本。
        /// </summary>
        public static string RandomExpiredRejectReply()
        {
            return RandomFromList(Def()?.expiredRejectReplies, "缪诺和亲请求已经过期。\n\n由于你没有在期限内通过通讯台回复，缪诺方面将本次安排记录为拒绝。");
        }

        /// <summary>
        /// 随机返回接受和亲后的回复文本。
        /// </summary>
        public static string RandomAcceptReply()
        {
            return RandomAcceptReplyLine().text;
        }

        /// <summary>
        /// 随机返回接受和亲后的回复文本与可选立绘覆盖。
        /// </summary>
        public static MunoCommDialogueResult RandomAcceptReplyLine()
        {
            MunoCommDialogueDef def = Def();
            return RandomLineFromList(def?.acceptReplyLines, def?.acceptReplies, "我们也希望如此。和亲穿梭机已经启程，请妥善迎接她。");
        }

        /// <summary>
        /// 随机返回拒绝和亲后的回复文本。
        /// </summary>
        public static string RandomRejectReply()
        {
            return RandomRejectReplyLine().text;
        }

        /// <summary>
        /// 随机返回拒绝和亲后的回复文本与可选立绘覆盖。
        /// </summary>
        public static MunoCommDialogueResult RandomRejectReplyLine()
        {
            MunoCommDialogueDef def = Def();
            return RandomLineFromList(def?.rejectReplyLines, def?.rejectReplies, "真是遗憾……希望以后我们能达成一致。");
        }

        /// <summary>
        /// 随机返回和亲公主送达后的提示文本。
        /// </summary>
        public static string RandomDeliveryReply()
        {
            return RandomFromList(Def()?.deliveryReplies, "缪诺和亲穿梭机已经抵达。\n\n和亲公主 {PAWN} 与随行嫁妆已经交付，请妥善安置她。");
        }

        /// <summary>
        /// 返回当前存档加载到的通讯文本 Def。
        /// </summary>
        private static MunoCommDialogueDef Def()
        {
            return DefDatabase<MunoCommDialogueDef>.GetNamedSilentFail(DialogueDefName);
        }

        /// <summary>
        /// 从指定文本列表中随机取一条有效文本；列表为空时返回默认文本。
        /// </summary>
        private static string RandomFromList(List<string> texts, string fallback)
        {
            if (texts == null || texts.Count == 0)
            {
                return fallback;
            }

            List<string> validTexts = new List<string>();
            for (int i = 0; i < texts.Count; i++)
            {
                if (!texts[i].NullOrEmpty())
                {
                    validTexts.Add(texts[i]);
                }
            }

            string selectedText = validTexts.Count > 0 ? validTexts.RandomElement() : fallback;
            return selectedText.Replace("\\n", "\n");
        }

        /// <summary>
        /// 从带立绘的对话条目中随机取一条；缺失时回退到旧文本列表。
        /// </summary>
        private static MunoCommDialogueResult RandomLineFromList(List<MunoCommDialogueLine> lines, List<string> legacyTexts, string fallback)
        {
            if (lines != null && lines.Count > 0)
            {
                List<MunoCommDialogueLine> validLines = new List<MunoCommDialogueLine>();
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i] != null && !lines[i].text.NullOrEmpty())
                    {
                        validLines.Add(lines[i]);
                    }
                }

                if (validLines.Count > 0)
                {
                    MunoCommDialogueLine selectedLine = validLines.RandomElement();
                    return new MunoCommDialogueResult(selectedLine.text.Replace("\\n", "\n"), selectedLine.portraitTexPath);
                }
            }

            return new MunoCommDialogueResult(RandomFromList(legacyTexts, fallback));
        }
    }
}
