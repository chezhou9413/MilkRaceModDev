using System.Collections.Generic;
using Verse;

namespace MunoRaceLib.MunoWorld
{
    /// <summary>
    /// 负责保存缪诺通讯窗口可配置的对话文本池。
    /// </summary>
    public class MunoCommDialogueDef : Def
    {
        public List<string> greetings;
        public List<MunoCommDialogueLine> greetingLines;
        public List<string> managerPrompts;
        public List<MunoCommDialogueLine> managerPromptLines;
        public List<string> noRequestReplies;
        public List<MunoCommDialogueLine> noRequestReplyLines;
        public List<string> requestLetters;
        public List<string> expiredRejectReplies;
        public List<string> acceptReplies;
        public List<MunoCommDialogueLine> acceptReplyLines;
        public List<string> rejectReplies;
        public List<MunoCommDialogueLine> rejectReplyLines;
        public List<string> deliveryReplies;
    }

    /// <summary>
    /// 负责描述一条通讯对话文本以及这条文本希望临时切换的立绘路径。
    /// </summary>
    public class MunoCommDialogueLine
    {
        public string text;
        public string portraitTexPath;
    }

    /// <summary>
    /// 负责承载一次随机抽取出的通讯文本和可选立绘覆盖路径。
    /// </summary>
    public class MunoCommDialogueResult
    {
        public string text;
        public string portraitTexPath;

        /// <summary>
        /// 用给定文本和立绘路径创建通讯文本结果。
        /// </summary>
        public MunoCommDialogueResult(string text, string portraitTexPath = null)
        {
            this.text = text;
            this.portraitTexPath = portraitTexPath;
        }
    }
}
