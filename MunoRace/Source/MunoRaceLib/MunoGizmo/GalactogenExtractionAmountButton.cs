using MunoRaceLib.MunoComp;
using MunoRaceLib.MunoDefRef;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace MunoRaceLib.MunoGizmo
{
    //在资源条标题右侧绘制自动汲取产物图标与数量循环按钮。
    [StaticConstructorOnStartup]
    public static class GalactogenExtractionAmountButton
    {
        private static readonly Texture2D MilkIcon = ContentFinder<Texture2D>.Get("Item/Resource/MunoRace_MunoMilk/MunoRace_MunoMilk");
        private static readonly Texture2D SlurryIcon = ContentFinder<Texture2D>.Get("Item/Resource/MunoRace_ConcentratedMulacte/MunoRace_ConcentratedMulacte");

        //根据当前字号为图标及最大数量预留固定宽度，避免切换档位时布局跳动。
        public static float RequiredWidth()
        {
            return Text.LineHeight + Text.CalcSize("100").x + 10f;
        }

        //绘制当前产物数量并处理点击，仅修改之后发起的自动汲取任务。
        public static void Draw(Rect rect, ThingComp_Galactogen comp)
        {
            TextAnchor oldAnchor = Text.Anchor;
            bool oldWordWrap = Text.WordWrap;
            Color oldColor = GUI.color;
            try
            {
                bool milk = comp.autoCollectMode == GalactogenAutoCollectMode.Milk;
                ThingDef product = milk ? MunoDefDataRef.MunoRace_MunoMilk : MunoDefDataRef.MunoRace_ConcentratedMulacte;
                string amount = GalactogenExtractionUtility.SelectedAmount(comp).ToString();
                float iconSize = Text.LineHeight;
                float numberWidth = Text.CalcSize(amount).x;
                float numberLeft = rect.xMax - numberWidth - 2f;
                //整组右对齐，图标紧贴当前数字，使用单件贴图避免物品堆叠图干扰数量含义。
                Rect iconRect = new Rect(numberLeft - iconSize - 3f, rect.center.y - iconSize * 0.5f, iconSize, iconSize);
                GUI.color = Color.white;
                GUI.DrawTexture(iconRect, milk ? MilkIcon : SlurryIcon, ScaleMode.ScaleToFit);
                Text.Anchor = TextAnchor.MiddleLeft;
                Text.WordWrap = false;
                Widgets.Label(new Rect(numberLeft, rect.y, numberWidth, rect.height), amount);
                if (Mouse.IsOver(rect)) Widgets.DrawHighlight(rect);
                TooltipHandler.TipRegion(rect, "单次自动汲取：" + GalactogenExtractionUtility.SelectedAmount(comp)
                    + " 份" + product.label + "\n点击循环切换：" + (milk ? "5 → 25 → 50 → 100" : "1 → 2 → 5")
                    + "\n每份消耗 " + (milk ? "1" : "100") + " 点乳源质。资源不足时仅产出可负担的整份数量。");
                if (Widgets.ButtonInvisible(rect))
                {
                    GalactogenExtractionUtility.CycleAmount(comp);
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                }
            }
            finally
            {
                Text.Anchor = oldAnchor;
                Text.WordWrap = oldWordWrap;
                GUI.color = oldColor;
            }
        }
    }
}
