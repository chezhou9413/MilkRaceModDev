using Verse;

namespace MunoRaceLib.MunoRecording
{
    //将穿戴式摄像机绑定到录像组件。
    public class CompProperties_MunoCamera : CompProperties
    {
        //指定负责拍摄进度与操作入口的组件类型。
        public CompProperties_MunoCamera() { compClass = typeof(Comp_MunoCamera); }
    }
}
