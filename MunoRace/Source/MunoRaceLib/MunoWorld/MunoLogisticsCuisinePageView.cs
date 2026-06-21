using UnityEngine;
using Verse;

namespace MunoRaceLib.MunoWorld
{
    //负责绘制后勤管理员料理试吃通讯页面。
    public static class MunoLogisticsCuisinePageView
    {
        private const float ButtonWidth = 360f;
        private const float ButtonHeight = 42f;
        private const float FooterHeight = 58f;
        private const float ButtonGap = 12f;

        //表示后勤料理页面在本帧触发的用户动作。
        public enum PageAction
        {
            None,
            RequestCuisine,
            RefuseCuisine,
            FeedbackDelicious,
            FeedbackOrdinary,
            FeedbackAwful,
            Close
        }

        //绘制后勤料理页面主体并返回本帧触发的操作。
        public static PageAction Draw(Rect rect, MunoTypewriterTextState typewriter, MunoLogisticsCuisineState state, bool replied)
        {
            float footerY = rect.yMax - FooterHeight;
            float dialogueHeight = Mathf.Min(180f, footerY - rect.y - 14f);
            Rect dialogueRect = new Rect(rect.x, rect.y, rect.width, dialogueHeight);
            Rect buttonAreaRect = new Rect(rect.x, dialogueRect.yMax + 14f, rect.width, footerY - dialogueRect.yMax - 26f);
            Rect footerRect = new Rect(rect.x, footerY, rect.width, FooterHeight);

            DrawDialoguePanel(dialogueRect, typewriter);
            PageAction action = replied ? PageAction.None : DrawStateButtons(buttonAreaRect, state);
            if (action != PageAction.None)
            {
                return action;
            }

            return DrawFooterButton(footerRect);
        }

        //返回后勤管理员初次提出料理试吃邀请的文本。
        public static string DialogueForState(MunoLogisticsCuisineState state)
        {
            switch (state)
            {
                case MunoLogisticsCuisineState.AwaitingTaste:
                    return MunoLogisticsCuisineText.AwaitingTasteText;
                case MunoLogisticsCuisineState.PendingFeedback:
                    return MunoLogisticsCuisineText.PendingFeedbackText;
                case MunoLogisticsCuisineState.Cooldown:
                    return MunoLogisticsCuisineText.CooldownText;
                default:
                    return MunoLogisticsCuisineText.RequestText;
            }
        }

        //绘制料理邀请和回复文本面板，并允许点击立即完成打字机文本。
        private static void DrawDialoguePanel(Rect rect, MunoTypewriterTextState typewriter)
        {
            MunoCommUIStyle.DrawPanel(rect);
            Rect inner = rect.ContractedBy(18f);

            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            bool oldWordWrap = Text.WordWrap;
            Color oldColor = GUI.color;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            GUI.color = MunoCommUIStyle.TextColor;
            Widgets.Label(inner, typewriter.VisibleText());
            if (Widgets.ButtonInvisible(inner))
            {
                typewriter.Complete();
            }

            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
            Text.WordWrap = oldWordWrap;
            GUI.color = oldColor;
        }

        //按当前状态绘制可用操作按钮。
        private static PageAction DrawStateButtons(Rect rect, MunoLogisticsCuisineState state)
        {
            if (state == MunoLogisticsCuisineState.Available)
            {
                return DrawRequestButtons(rect);
            }

            if (state == MunoLogisticsCuisineState.PendingFeedback)
            {
                return DrawFeedbackButtons(rect);
            }

            return PageAction.None;
        }

        //绘制玩家是否接受试吃的两个选择按钮。
        private static PageAction DrawRequestButtons(Rect rect)
        {
            float buttonWidth = Mathf.Min(ButtonWidth, rect.width - 24f);
            float totalHeight = ButtonHeight * 2f + ButtonGap;
            float x = rect.x + (rect.width - buttonWidth) * 0.5f;
            float y = rect.y + Mathf.Max(18f, (rect.height - totalHeight) * 0.45f);

            if (MunoCommUIStyle.DrawButton(new Rect(x, y, buttonWidth, ButtonHeight), "好啊"))
            {
                return PageAction.RequestCuisine;
            }

            if (MunoCommUIStyle.DrawButton(new Rect(x, y + ButtonHeight + ButtonGap, buttonWidth, ButtonHeight), "不需要"))
            {
                return PageAction.RefuseCuisine;
            }

            return PageAction.None;
        }

        //绘制玩家试吃后的三项反馈按钮。
        private static PageAction DrawFeedbackButtons(Rect rect)
        {
            float buttonWidth = Mathf.Min(ButtonWidth, rect.width - 24f);
            float totalHeight = ButtonHeight * 3f + ButtonGap * 2f;
            float x = rect.x + (rect.width - buttonWidth) * 0.5f;
            float y = rect.y + Mathf.Max(12f, (rect.height - totalHeight) * 0.45f);

            if (MunoCommUIStyle.DrawButton(new Rect(x, y, buttonWidth, ButtonHeight), "这料理非常美味"))
            {
                return PageAction.FeedbackDelicious;
            }

            if (MunoCommUIStyle.DrawButton(new Rect(x, y + ButtonHeight + ButtonGap, buttonWidth, ButtonHeight), "平平无奇，没什么特别的"))
            {
                return PageAction.FeedbackOrdinary;
            }

            if (MunoCommUIStyle.DrawButton(new Rect(x, y + (ButtonHeight + ButtonGap) * 2f, buttonWidth, ButtonHeight), "是不是变质了？好难吃..."))
            {
                return PageAction.FeedbackAwful;
            }

            return PageAction.None;
        }

        //绘制底部断开通讯按钮。
        private static PageAction DrawFooterButton(Rect rect)
        {
            MunoCommUIStyle.DrawPanel(rect);
            Rect buttonRect = new Rect(rect.x + (rect.width - ButtonWidth) * 0.5f, rect.y + (rect.height - ButtonHeight) * 0.5f, ButtonWidth, ButtonHeight);
            return MunoCommUIStyle.DrawButton(buttonRect, "断开通讯") ? PageAction.Close : PageAction.None;
        }
    }
}
