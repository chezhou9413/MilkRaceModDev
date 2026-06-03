using Verse;

namespace MunoRaceLib.MunoWorld
{
    /// <summary>
    /// 负责保存缪诺通讯界面的可配置 UI 参数。
    /// </summary>
    public class MunoCommUIConfigDef : Def
    {
        public MunoCommPortraitLayout homePortrait;
        public MunoCommPortraitLayout marriagePortrait;
    }

    /// <summary>
    /// 负责描述单个通讯立绘区域的贴图和图片绘制参数。
    /// </summary>
    public class MunoCommPortraitLayout
    {
        public string texPath;
        public float frameInset;
        public float imageInset;
        public float imageOffsetX;
        public float imageOffsetY;
        public float imageScale;
        public bool cover;
    }
}
