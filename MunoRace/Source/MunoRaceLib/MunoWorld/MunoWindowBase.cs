using RimWorld;
using Verse;
using Verse.Sound;

namespace MunoRaceLib.MunoWorld
{
    /// <summary>
    /// 负责为缪诺通讯窗口统一提供输入拦截与时间控制快捷键支持。
    /// </summary>
    public abstract class MunoWindowBase : Window
    {
        /// <summary>
        /// 初始化缪诺通讯窗口的基础交互行为。
        /// </summary>
        protected MunoWindowBase()
        {
            forcePause = false;
            absorbInputAroundWindow = true;
            doCloseX = false;
            doCloseButton = false;
            preventCameraMotion = false;
            closeOnClickedOutside = true;
        }

        /// <summary>
        /// 在窗口更新时同步处理时间控制热键，保持与原版主界面一致。
        /// </summary>
        public override void WindowUpdate()
        {
            base.WindowUpdate();
            if (Find.WindowStack.GetsInput(this))
            {
                HandleTimeControls();
            }
        }

        /// <summary>
        /// 响应暂停与倍速快捷键，避免通讯窗口期间失去常用时间控制能力。
        /// </summary>
        private static void HandleTimeControls()
        {
            if (KeyBindingDefOf.TogglePause.JustPressed)
            {
                Find.TickManager.TogglePaused();
                PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Pause, KnowledgeAmount.SpecificInteraction);
                return;
            }

            if (KeyBindingDefOf.TimeSpeed_Normal.JustPressed)
            {
                Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                SoundDefOf.Clock_Normal.PlayOneShotOnCamera();
                PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.TimeControls, KnowledgeAmount.SpecificInteraction);
                return;
            }

            if (KeyBindingDefOf.TimeSpeed_Fast.JustPressed)
            {
                Find.TickManager.CurTimeSpeed = TimeSpeed.Fast;
                SoundDefOf.Clock_Fast.PlayOneShotOnCamera();
                PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.TimeControls, KnowledgeAmount.SpecificInteraction);
                return;
            }

            if (KeyBindingDefOf.TimeSpeed_Superfast.JustPressed)
            {
                Find.TickManager.CurTimeSpeed = TimeSpeed.Superfast;
                SoundDefOf.Clock_Superfast.PlayOneShotOnCamera();
                PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.TimeControls, KnowledgeAmount.SpecificInteraction);
                return;
            }

            if (KeyBindingDefOf.TimeSpeed_Ultrafast.JustPressed)
            {
                Find.TickManager.CurTimeSpeed = TimeSpeed.Ultrafast;
                SoundDefOf.Clock_Superfast.PlayOneShotOnCamera();
            }
        }
    }
}
