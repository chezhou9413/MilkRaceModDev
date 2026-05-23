using RimWorld;
using Verse;

namespace MunoRaceLib.MunoComp
{
    /// <summary>
    /// 负责为触手装甲相关 Gizmo 统一生成带剩余时间的按钮文本。
    /// </summary>
    public static class TentacleArmorGizmoUtility
    {
        /// <summary>
        /// 根据指定 Hediff 的消失组件，为按钮标签追加当前剩余时间。
        /// </summary>
        public static string BuildTimedLabel(Pawn pawn, HediffDef hediffDef, string baseLabel)
        {
            HediffComp_Disappears disappears = GetDisappearComp(pawn, hediffDef);
            if (disappears == null)
            {
                return baseLabel;
            }

            return baseLabel + "\n剩余时间: " + GetRemainingTimeText(disappears);
        }

        /// <summary>
        /// 读取指定 Hediff 当前的剩余持续时间文本。
        /// </summary>
        public static string GetRemainingTimeText(Pawn pawn, HediffDef hediffDef)
        {
            return GetRemainingTimeText(GetDisappearComp(pawn, hediffDef));
        }

        /// <summary>
        /// 将消失组件中的剩余 Tick 转换为游戏内一致的时间文本。
        /// </summary>
        public static string GetRemainingTimeText(HediffComp_Disappears disappears)
        {
            if (disappears == null)
            {
                return string.Empty;
            }

            int ticks = disappears.EffectiveTicksToDisappear;
            if (ticks < 2500)
            {
                return ticks.ToStringSecondsFromTicks("F0");
            }

            return ticks.ToStringTicksToPeriod(allowSeconds: true, shortForm: true, canUseDecimals: true, allowYears: true, disappears.Props.canUseDecimalsShortForm);
        }

        /// <summary>
        /// 获取小人指定 Hediff 上的消失组件，供按钮与描述复用。
        /// </summary>
        private static HediffComp_Disappears GetDisappearComp(Pawn pawn, HediffDef hediffDef)
        {
            if (pawn?.health?.hediffSet == null || hediffDef == null)
            {
                return null;
            }

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            return hediff?.TryGetComp<HediffComp_Disappears>();
        }
    }
}
