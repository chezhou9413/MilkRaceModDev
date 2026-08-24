using Verse;

namespace MunoRaceLib.MunoWorld
{
    //负责为缪诺通讯窗口统一提供自动暂停与输入拦截行为。
    public abstract class MunoWindowBase : Window
    {
        //初始化缪诺通讯窗口的暂停、关闭和镜头交互配置。
        protected MunoWindowBase()
        {
            forcePause = true;
            absorbInputAroundWindow = true;
            doCloseX = false;
            doCloseButton = false;
            preventCameraMotion = false;
            closeOnClickedOutside = true;
        }
    }
}
