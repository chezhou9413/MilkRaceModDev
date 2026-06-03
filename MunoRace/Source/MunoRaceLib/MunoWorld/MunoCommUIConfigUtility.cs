using UnityEngine;
using Verse;

namespace MunoRaceLib.MunoWorld
{
    /// <summary>
    /// 负责读取缪诺通讯 UI 配置，并提供带默认值的立绘绘制工具。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class MunoCommUIConfigUtility
    {
        private const string ConfigDefName = "Muno_CommUIConfig";

        /// <summary>
        /// 返回首页立绘配置。
        /// </summary>
        public static MunoCommPortraitLayout HomePortrait()
        {
            return Config()?.homePortrait;
        }

        /// <summary>
        /// 返回和亲页立绘配置。
        /// </summary>
        public static MunoCommPortraitLayout MarriagePortrait()
        {
            return Config()?.marriagePortrait;
        }

        /// <summary>
        /// 返回立绘栏宽度；立绘配置只允许调整图片，布局宽度始终使用代码默认值。
        /// </summary>
        public static float ColumnWidth(MunoCommPortraitLayout layout, float fallback)
        {
            return fallback;
        }

        /// <summary>
        /// 按代码布局计算立绘面板区域；XML 不再控制框的位置和大小。
        /// </summary>
        public static Rect PanelRect(Rect parent, MunoCommPortraitLayout layout, float fallbackX, float fallbackY, float fallbackWidth, float fallbackHeight)
        {
            return new Rect(parent.x + fallbackX, parent.y + fallbackY, fallbackWidth, fallbackHeight);
        }

        /// <summary>
        /// 复制基础立绘配置并临时替换贴图路径。
        /// </summary>
        public static MunoCommPortraitLayout WithPortraitPath(MunoCommPortraitLayout layout, string texPath)
        {
            if (texPath.NullOrEmpty())
            {
                return layout;
            }

            MunoCommPortraitLayout copiedLayout = new MunoCommPortraitLayout();
            if (layout != null)
            {
                copiedLayout.texPath = layout.texPath;
                copiedLayout.frameInset = layout.frameInset;
                copiedLayout.imageInset = layout.imageInset;
                copiedLayout.imageOffsetX = layout.imageOffsetX;
                copiedLayout.imageOffsetY = layout.imageOffsetY;
                copiedLayout.imageScale = layout.imageScale;
                copiedLayout.cover = layout.cover;
            }

            copiedLayout.texPath = texPath;
            return copiedLayout;
        }

        /// <summary>
        /// 按配置绘制带边框和底色的立绘面板。
        /// </summary>
        public static void DrawPortraitPanel(Rect rect, MunoCommPortraitLayout layout, string fallbackPath)
        {
            float frameInset = layout != null && layout.frameInset > 0f ? layout.frameInset : 10f;
            float imageInset = layout != null && layout.imageInset > 0f ? layout.imageInset : 14f;

            MunoCommUIStyle.DrawPanel(rect);
            MunoCommUIStyle.DrawLightPanel(rect.ContractedBy(frameInset));
            DrawPortraitTexture(rect.ContractedBy(imageInset), layout, fallbackPath);
        }

        /// <summary>
        /// 按配置绘制立绘贴图，并支持路径、缩放和偏移调整。
        /// </summary>
        private static void DrawPortraitTexture(Rect rect, MunoCommPortraitLayout layout, string fallbackPath)
        {
            Texture2D texture = PortraitTexture(layout, fallbackPath);
            float scale = layout != null && layout.imageScale > 0f ? layout.imageScale : 1f;
            bool cover = layout != null && layout.cover;
            float fitScale = cover ? Mathf.Max(rect.width / texture.width, rect.height / texture.height) : Mathf.Min(rect.width / texture.width, rect.height / texture.height);
            float drawWidth = texture.width * fitScale * scale;
            float drawHeight = texture.height * fitScale * scale;
            float offsetX = layout?.imageOffsetX ?? 0f;
            float offsetY = layout?.imageOffsetY ?? 0f;
            Rect drawRect = new Rect(rect.x + (rect.width - drawWidth) * 0.5f + offsetX, rect.y + (rect.height - drawHeight) * 0.5f + offsetY, drawWidth, drawHeight);
            GUI.DrawTexture(drawRect, texture, ScaleMode.StretchToFill, alphaBlend: true);
        }

        /// <summary>
        /// 返回配置指定的立绘贴图；路径缺失或加载失败时使用默认贴图。
        /// </summary>
        private static Texture2D PortraitTexture(MunoCommPortraitLayout layout, string fallbackPath)
        {
            string texPath = layout != null && !layout.texPath.NullOrEmpty() ? layout.texPath : fallbackPath;
            return ContentFinder<Texture2D>.Get(texPath, false) ?? BaseContent.BadTex;
        }

        /// <summary>
        /// 返回当前加载的通讯 UI 配置 Def。
        /// </summary>
        private static MunoCommUIConfigDef Config()
        {
            return DefDatabase<MunoCommUIConfigDef>.GetNamedSilentFail(ConfigDefName);
        }
    }
}
