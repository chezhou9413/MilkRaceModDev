using System;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace MunoRaceLib.MunoWorld
{
    //用缪诺通讯样式显示服务对话，沿用原版节点的跳转和执行逻辑。
    [StaticConstructorOnStartup]
    public class Dialog_MunoCommNodeTree : Dialog_NodeTree
    {
        private static readonly Texture2D Logo = ContentFinder<Texture2D>.Get("UI/MunoLogo", true);
        private static readonly AccessTools.FieldRef<DiaOption, string> OptionText =
            AccessTools.FieldRefAccess<DiaOption, string>("text");
        private static readonly Action<DiaOption> ActivateOption =
            AccessTools.MethodDelegate<Action<DiaOption>>(AccessTools.Method(typeof(DiaOption), "Activate"));

        private readonly string portraitPath;
        private readonly float interactiveAt;
        private DiaNode displayedNode;
        private Vector2 textScroll;
        private Vector2 optionScroll;

        //设置管理员立绘、通讯音效及结局窗口的防误触延迟。
        public Dialog_MunoCommNodeTree(DiaNode root, string title,
            string portraitPath = "UI/MunoCommManagers", bool delayInteractivity = false)
            : base(root, delayInteractivity, radioMode: true, title: title)
        {
            this.portraitPath = portraitPath;
            interactiveAt = Time.realtimeSinceStartup + (delayInteractivity ? 1f : 0f);
            doCloseX = false;
            doCloseButton = false;
            preventCameraMotion = false;
            closeOnCancel = !delayInteractivity;
            closeOnClickedOutside = !delayInteractivity;
        }

        //与通讯终端保持相同的窗口尺寸。
        public override Vector2 InitialSize => new Vector2(1024f, 680f);

        //为融入申请创建同样式的确认和取消选项。
        public static Dialog_MunoCommNodeTree CreateConfirmation(string text, Action confirm)
        {
            var node = new DiaNode(text);
            node.options.Add(new DiaOption("Confirm".Translate()) { action = confirm, resolveTree = true });
            node.options.Add(new DiaOption("GoBack".Translate()) { resolveTree = true });
            return new Dialog_MunoCommNodeTree(node, "缪诺聚落管理员");
        }

        //为正文和选项分别留出滚动区域，退出绘制后才执行选中的事务。
        public override void DoWindowContents(Rect inRect)
        {
            if (displayedNode != curNode)
            {
                displayedNode = curNode;
                textScroll = Vector2.zero;
                optionScroll = Vector2.zero;
            }

            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            bool oldWordWrap = Text.WordWrap;
            Color oldColor = GUI.color;
            DiaOption selected = null;
            try
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                Text.WordWrap = true;
                GUI.color = Color.white;
                MunoCommUIStyle.DrawBackground(inRect);
                bool close = MunoCommUIStyle.DrawTerminalHeader(inRect, title, Logo);
                if (close && Time.realtimeSinceStartup >= interactiveAt)
                {
                    Close();
                    return;
                }

                Rect body = new Rect(inRect.x + 12f, inRect.y + 64f, inRect.width - 24f, inRect.height - 76f);
                float portraitWidth = Mathf.Min(280f, body.width * 0.32f);
                Rect portrait = new Rect(body.x, body.y, portraitWidth, body.height);
                MunoCommPortraitLayout layout = MunoCommUIConfigUtility.WithPortraitPath(
                    MunoCommUIConfigUtility.HomePortrait(), portraitPath);
                MunoCommUIConfigUtility.DrawPortraitPanel(portrait, layout, portraitPath);

                Rect content = new Rect(portrait.xMax + 14f, body.y, body.width - portraitWidth - 14f, body.height);
                float optionWidth = content.width - 40f;
                float totalHeight = 0f;
                foreach (DiaOption option in curNode.options) totalHeight += OptionHeight(option, optionWidth);
                float optionHeight = Mathf.Min(totalHeight + 24f, content.height * 0.55f);
                Rect options = new Rect(content.x, content.yMax - optionHeight, content.width, optionHeight);
                DrawDialogue(new Rect(content.x, content.y, content.width, options.y - content.y - 12f));
                selected = DrawOptions(options, totalHeight);
            }
            finally
            {
                Text.Font = oldFont;
                Text.Anchor = oldAnchor;
                Text.WordWrap = oldWordWrap;
                GUI.color = oldColor;
            }

            //保留原版扣费、冷却、音效和返回节点，避免复制业务判断。
            if (selected != null) ActivateOption(selected);
        }

        //按实际文字高度显示说明，长名单和费用说明可以滚动查看。
        private void DrawDialogue(Rect rect)
        {
            MunoCommUIStyle.DrawPanel(rect);
            Rect viewport = rect.ContractedBy(14f);
            string text = curNode.text.Resolve();
            float width = viewport.width - 16f;
            Rect view = new Rect(0f, 0f, width, Mathf.Max(viewport.height, Text.CalcHeight(text, width)));
            Widgets.BeginScrollView(viewport, ref textScroll, view);
            GUI.color = MunoCommUIStyle.TextColor;
            Widgets.Label(view, text);
            GUI.color = Color.white;
            Widgets.EndScrollView();
        }

        //完整显示按钮与不可用原因，按钮点击区域不随按下动画变化。
        private DiaOption DrawOptions(Rect rect, float totalHeight)
        {
            MunoCommUIStyle.DrawPanel(rect);
            Rect viewport = rect.ContractedBy(12f);
            float width = viewport.width - 16f;
            Rect view = new Rect(0f, 0f, width, Mathf.Max(viewport.height, totalHeight));
            DiaOption selected = null;
            Widgets.BeginScrollView(viewport, ref optionScroll, view);
            float y = 0f;
            foreach (DiaOption option in curNode.options)
            {
                string label = OptionText(option);
                float buttonHeight = ButtonHeight(label, width);
                Rect button = new Rect(0f, y, width, buttonHeight);
                bool active = !option.disabled && Time.realtimeSinceStartup >= interactiveAt;
                if (MunoCommUIStyle.DrawButton(button, label, active, wordWrap: true)) selected = option;
                if (option.disabled && !option.disabledReason.NullOrEmpty())
                {
                    float reasonHeight = Text.CalcHeight(option.disabledReason, width - 16f);
                    GUI.color = MunoCommUIStyle.SubtleTextColor;
                    Widgets.Label(new Rect(8f, y + buttonHeight + 4f, width - 16f, reasonHeight), option.disabledReason);
                    GUI.color = Color.white;
                }
                y += OptionHeight(option, width);
            }
            Widgets.EndScrollView();
            return selected;
        }

        //根据换行后的按钮文字计算高度。
        private static float ButtonHeight(string label, float width)
        {
            return Mathf.Max(44f, Text.CalcHeight(label, width - 16f) + 16f);
        }

        //将禁用原因和行间距计入滚动内容高度。
        private static float OptionHeight(DiaOption option, float width)
        {
            float height = ButtonHeight(OptionText(option), width) + 10f;
            if (option.disabled && !option.disabledReason.NullOrEmpty())
                height += 4f + Text.CalcHeight(option.disabledReason, width - 16f);
            return height;
        }
    }
}
