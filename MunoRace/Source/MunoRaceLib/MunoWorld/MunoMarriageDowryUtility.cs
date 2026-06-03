using MunoRaceLib.MunoDefRef;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MunoRaceLib.MunoWorld
{
    /// <summary>
    /// 负责按 XML 配置与默认池生成缪诺和亲嫁妆。
    /// </summary>
    public static class MunoMarriageDowryUtility
    {
        private const string DowryDefName = "Muno_MarriageDowry";
        private static readonly string[] FallbackWeapons =
        {
            "Gun_MunoLMG", "Gun_MunoAR", "Gun_MunoSR", "Gun_MunoHPP", "Gun_MunoPO", "Gun_MunoMP",
            "Gun_MunoMGL", "Gun_MunoHMG", "Gun_MunoAC", "Gun_MunoRGL", "Gun_MunoLR", "Gun_MunoLSR",
            "Gun_MunoLPO", "Gun_MunoLC"
        };
        private static readonly string[] FallbackArmors = { "MunoRace_Apparel_Tentaclearmor" };
        private static readonly string[] FallbackArchotechParts = { "ArchotechArm", "ArchotechLeg", "ArchotechEye" };
        private static readonly string[] FallbackBionicParts = { "BionicArm", "BionicLeg", "BionicEye", "BionicHeart", "BionicSpine" };

        /// <summary>
        /// 从四类嫁妆中随机一类并生成对应物品。
        /// </summary>
        public static List<Thing> GenerateRandomDowry()
        {
            List<Thing> result = new List<Thing>();
            DowryKind kind = Rand.Element(DowryKind.Weapon, DowryKind.Armor, DowryKind.BodyPart, DowryKind.Techprint);
            switch (kind)
            {
                case DowryKind.Weapon:
                    GenerateThings(result, WeaponPool(), CountRange(Def()?.weaponCount, new IntRange(1, 2)).RandomInRange, true);
                    break;
                case DowryKind.Armor:
                    GenerateThings(result, ArmorPool(), CountRange(Def()?.armorCount, new IntRange(1, 1)).RandomInRange, true);
                    break;
                case DowryKind.BodyPart:
                    GenerateThings(result, BodyPartPool(), CountRange(Def()?.bodyPartCount, new IntRange(1, 3)).RandomInRange, false);
                    break;
                case DowryKind.Techprint:
                    GenerateTechprints(result, CountRange(Def()?.techprintCount, new IntRange(1, 1)).RandomInRange);
                    break;
            }

            if (result.Count == 0)
            {
                GenerateThings(result, WeaponPool(), 1, true);
            }

            return result;
        }

        /// <summary>
        /// 根据指定 Def 池创建物品，并按需要设置品质。
        /// </summary>
        private static void GenerateThings(List<Thing> outThings, List<ThingDef> pool, int count, bool assignQuality)
        {
            if (pool == null || pool.Count == 0)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                ThingDef thingDef = pool.RandomElement();
                Thing thing = ThingMaker.MakeThing(thingDef, GenStuff.DefaultStuffFor(thingDef));
                if (assignQuality)
                {
                    SetConfiguredQuality(thing);
                }

                outThings.Add(thing);
            }
        }

        /// <summary>
        /// 生成玩家当前仍需要的科技蓝图。
        /// </summary>
        private static void GenerateTechprints(List<Thing> outThings, int count)
        {
            Faction munoFaction = Find.FactionManager.FirstFactionOfDef(MunoDefDataRef.MunoColony_Faction);
            List<ThingDef> generated = new List<ThingDef>();
            for (int i = 0; i < count; i++)
            {
                ThingDef techprintDef = TechprintUtility.GetResearchProjectsNeedingTechprintsNow(munoFaction, generated)
                    .RandomElementByWeightWithFallback(TechprintSelectionWeight)
                    ?.Techprint;
                if (techprintDef == null)
                {
                    techprintDef = FallbackTechprint(generated);
                }

                if (techprintDef == null)
                {
                    continue;
                }

                generated.Add(techprintDef);
                outThings.Add(ThingMaker.MakeThing(techprintDef));
            }
        }

        /// <summary>
        /// 返回科技蓝图随机权重。
        /// </summary>
        private static float TechprintSelectionWeight(ResearchProjectDef project)
        {
            return project.techprintCommonality * (project.PrerequisitesCompleted ? 1f : 0.02f);
        }

        /// <summary>
        /// 在派系规则无法提供蓝图时，从所有玩家仍需要的蓝图中回退抽取。
        /// </summary>
        private static ThingDef FallbackTechprint(List<ThingDef> generated)
        {
            return DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => def.HasComp(typeof(CompTechprint)) && !generated.Contains(def))
                .Where(def =>
                {
                    CompProperties_Techprint comp = def.GetCompProperties<CompProperties_Techprint>();
                    return comp?.project != null && !comp.project.IsFinished && !comp.project.TechprintRequirementMet;
                })
                .RandomElementWithFallback();
        }

        /// <summary>
        /// 为带品质组件的装备设置配置范围内的随机品质。
        /// </summary>
        private static void SetConfiguredQuality(Thing thing)
        {
            CompQuality compQuality = thing.TryGetComp<CompQuality>();
            if (compQuality == null)
            {
                return;
            }

            MunoMarriageDowryDef def = Def();
            QualityCategory min = def?.minQuality ?? QualityCategory.Excellent;
            QualityCategory max = def?.maxQuality ?? QualityCategory.Masterwork;
            int value = Rand.RangeInclusive((int)min, (int)max);
            compQuality.SetQuality((QualityCategory)value, ArtGenerationContext.Outsider);
        }

        /// <summary>
        /// 返回可用的缪诺武器池。
        /// </summary>
        private static List<ThingDef> WeaponPool()
        {
            return ConfiguredOrFallback(Def()?.weaponPool, FallbackWeapons).Where(def => def.IsWeapon).ToList();
        }

        /// <summary>
        /// 返回可用的缪诺动力甲池。
        /// </summary>
        private static List<ThingDef> ArmorPool()
        {
            return ConfiguredOrFallback(Def()?.armorPool, FallbackArmors).ToList();
        }

        /// <summary>
        /// 返回高级仿生部件池，超凡池为空时回退普通仿生。
        /// </summary>
        private static List<ThingDef> BodyPartPool()
        {
            List<ThingDef> archotech = ConfiguredOrFallback(Def()?.archotechBodyPartPool, FallbackArchotechParts);
            if (archotech.Count > 0)
            {
                return archotech;
            }

            return ConfiguredOrFallback(Def()?.bionicFallbackPool, FallbackBionicParts);
        }

        /// <summary>
        /// 合并 XML 配置池与默认 defName 池。
        /// </summary>
        private static List<ThingDef> ConfiguredOrFallback(List<ThingDef> configured, string[] fallbackNames)
        {
            if (configured != null && configured.Count > 0)
            {
                return configured.Where(def => def != null).ToList();
            }

            List<ThingDef> result = new List<ThingDef>();
            for (int i = 0; i < fallbackNames.Length; i++)
            {
                ThingDef thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(fallbackNames[i]);
                if (thingDef != null)
                {
                    result.Add(thingDef);
                }
            }

            return result;
        }

        /// <summary>
        /// 返回有效数量范围；配置无效时使用默认范围。
        /// </summary>
        private static IntRange CountRange(IntRange? configured, IntRange fallback)
        {
            if (!configured.HasValue || configured.Value.min <= 0 || configured.Value.max < configured.Value.min)
            {
                return fallback;
            }

            return configured.Value;
        }

        /// <summary>
        /// 返回当前加载的嫁妆配置 Def。
        /// </summary>
        private static MunoMarriageDowryDef Def()
        {
            return DefDatabase<MunoMarriageDowryDef>.GetNamedSilentFail(DowryDefName);
        }

        /// <summary>
        /// 表示本次嫁妆随机到的类别。
        /// </summary>
        private enum DowryKind
        {
            Weapon,
            Armor,
            BodyPart,
            Techprint
        }
    }
}
