using RimWorld;
using Verse;

namespace MunoRaceLib.MunoComp
{
    /// <summary>
    /// 可搬迁太阳能板的发电组件。
    ///
    /// 原版 CompPowerPlantSolar.DesiredPowerOutput 里直接读取 parent.Map.skyManager，
    /// 没有对 Map 为空做任何保护：
    ///
    ///     protected override float DesiredPowerOutput =>
    ///         Mathf.Lerp(0f, 0f - Props.PowerConsumption, parent.Map.skyManager.CurSkyGlow) * RoofedPowerOutputFactor;
    ///
    /// 而 MinifiedThing 只实现了 IThingHolder，没有实现 IThingHolderTickable，
    /// 所以 Verse.Thing.DoTick() 末尾那段"递归 tick 内部物品"的逻辑不会跳过它，
    /// 折叠箱里那个未生成的建筑仍然会被 tick：
    ///
    ///     TickList.Tick() -> Pawn.DoTick() -> MinifiedThing.DoTick() -> Building.DoTick()
    ///         -> CompPowerPlant.CompTick() -> UpdateDesiredPowerOutput()
    ///
    /// 此时 parent.Map 为 null，于是抛 NullReferenceException，并且异常会向上打断
    /// 搬运小人的整次 tick（小人会卡住）。原版太阳能板不可搬迁，所以原版踩不到；
    /// 只有给太阳能板加了 minifiedDef 才会暴露这个原版缺陷。
    ///
    /// 本类在"未生成"时直接返回 0 功率并跳过整次 comp tick，从而彻底避开 null Map。
    /// </summary>
    public class CompPowerPlantSolarSafe : CompPowerPlantSolar
    {
        protected override float DesiredPowerOutput
        {
            get
            {
                if (!parent.Spawned)
                {
                    return 0f;
                }
                Map map = parent.Map;
                if (map == null || map.skyManager == null)
                {
                    return 0f;
                }
                return base.DesiredPowerOutput;
            }
        }

        public override void CompTick()
        {
            // 折叠 / 搬运 / 未生成状态下什么都不做：既不去读 Map，也不去碰电网。
            if (!parent.Spawned)
            {
                return;
            }
            base.CompTick();
        }
    }
}
