using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimWorld
{
    /// <summary>
    /// 治疗枪目标效果组件的配置数据，负责定义单次治疗数量与包扎质量。
    /// </summary>
    public class CompProperties_TargetEffect_MunoHealGun : CompProperties
    {
        public int maxWoundsPerShot = 3;
        public float tendQuality = 0.8f;
        public float maxTendQuality = 1f;

        /// <summary>
        /// 初始化治疗枪目标效果组件的默认配置，并绑定运行时组件类型。
        /// </summary>
        public CompProperties_TargetEffect_MunoHealGun()
        {
            compClass = typeof(CompTargetEffect_MunoHealGun);
        }
    }

    /// <summary>
    /// 治疗枪目标效果组件，负责判定目标是否可治疗并在命中后处理流血伤口。
    /// </summary>
    public class CompTargetEffect_MunoHealGun : CompTargetEffect
    {
        private CompProperties_TargetEffect_MunoHealGun PropsHealGun => (CompProperties_TargetEffect_MunoHealGun)props;

        /// <summary>
        /// 对命中的目标执行治疗逻辑，按优先级处理若干个最危险的流血伤口。
        /// </summary>
        public override void DoEffectOn(Pawn user, Thing target)
        {
            if (!(target is Pawn pawn))
            {
                return;
            }

            List<Hediff> wounds = GetBleedingWounds(pawn);
            int tendedCount = 0;
            int woundsToTreat = Mathf.Min(PropsHealGun.maxWoundsPerShot, wounds.Count);

            for (int i = 0; i < woundsToTreat; i++)
            {
                wounds[i].Tended(PropsHealGun.tendQuality, PropsHealGun.maxTendQuality, i + 1);
                tendedCount++;
            }

            if (tendedCount > 0)
            {
                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "NumWoundsTended".Translate(tendedCount), 3.65f);
            }
        }

        /// <summary>
        /// 判断目标当前是否存在可被治疗枪处理的流血伤口。
        /// </summary>
        public override bool CanApplyOn(Thing target)
        {
            return target is Pawn pawn && HasTreatableBleedingWound(pawn);
        }

        /// <summary>
        /// 快速扫描 Pawn 是否存在至少一个可处理的流血伤口，供高频选目标阶段使用。
        /// </summary>
        private static bool HasTreatableBleedingWound(Pawn pawn)
        {
            if (pawn?.health?.hediffSet?.hediffs == null)
            {
                return false;
            }

            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                Hediff hediff = hediffs[i];
                if ((hediff is Hediff_Injury || hediff is Hediff_MissingPart) && hediff.TendableNow() && hediff.BleedRate > 0f)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 收集并按危险程度排序 Pawn 的可处理流血伤口，供真正命中后执行治疗时使用。
        /// </summary>
        private static List<Hediff> GetBleedingWounds(Pawn pawn)
        {
            return pawn.health.hediffSet.hediffs
                .Where(hediff => (hediff is Hediff_Injury || hediff is Hediff_MissingPart) && hediff.TendableNow() && hediff.BleedRate > 0f)
                .OrderByDescending(hediff => hediff.BleedRate)
                .ThenByDescending(hediff => hediff.Severity)
                .ToList();
        }
    }
}
