using System.Collections.Generic;
using MunoRaceLib.MunoDefRef;
using RimWorld;
using Verse;

namespace MunoRaceLib.MunoBiotech.Production
{
    //消耗装填的缪诺乳自动分离两种产物，并保留尚未排出的成品。
    public class Building_MunoMilkSeparator : Building, IThingHolder
    {
        private const int MilkPerBatch = 25;
        private const int TicksPerBatch = 2500;
        private int processingTicks;
        private int outputCheckTicks;
        private ThingOwner<Thing> products;
        private CompRefuelable milkStorage;
        private CompPowerTrader power;

        //建立随建筑保存的成品容器。
        public Building_MunoMilkSeparator()
        {
            products = new ThingOwner<Thing>(this);
        }

        //缓存生产所需的装填组件和供电组件。
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            milkStorage = GetComp<CompRefuelable>();
            power = GetComp<CompPowerTrader>();
        }

        //出口清空后才开始下一批，断电或原料不足时保留进度。
        protected override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (products.Count > 0)
            {
                outputCheckTicks += delta;
                if (outputCheckTicks >= 250)
                {
                    outputCheckTicks = 0;
                    products.TryDropAll(InteractionCell, Map, ThingPlaceMode.Near);
                }
                return;
            }
            if (!power.PowerOn || milkStorage.Fuel < MilkPerBatch)
                return;

            processingTicks += delta;
            if (processingTicks < TicksPerBatch)
                return;

            StoreProduct(MunoBiotechDefOf.MunoRace_MunoWhey, MilkPerBatch);
            StoreProduct(MunoDefDataRef.MunoRace_ConcentratedMulacte, 1);
            milkStorage.ConsumeFuel(MilkPerBatch);
            processingTicks = 0;
            products.TryDropAll(InteractionCell, Map, ThingPlaceMode.Near);
        }

        //把整批成品写入容器，交由原版容器处理堆叠和落地。
        private void StoreProduct(ThingDef product, int count)
        {
            Thing thing = ThingMaker.MakeThing(product);
            thing.stackCount = count;
            if (!products.TryAdd(thing))
                throw new System.InvalidOperationException("缪诺乳分离机无法保存产物：" + product.defName);
        }

        //保存生产进度与未排出的成品。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref processingTicks, "processingTicks");
            Scribe_Deep.Look(ref products, "products", this);
        }

        //在建筑信息中说明当前阻塞原因与加工进度。
        public override string GetInspectString()
        {
            string status = products.Count > 0 ? "等待排出成品，请清理周围空间。"
                : !power.PowerOn ? "等待供电。"
                : milkStorage.Fuel < MilkPerBatch ? "每批需要 25 份缪诺乳。"
                : "分离进度：" + ((float)processingTicks / TicksPerBatch).ToStringPercent();
            return base.GetInspectString() + "\n" + status;
        }

        //拆除或摧毁时释放容器内已经完成的产物。
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (Spawned && !products.TryDropAll(InteractionCell, Map, ThingPlaceMode.Near))
                Log.Warning("缪诺乳分离机被移除时，周围没有足够空间放置成品。");
            base.Destroy(mode);
        }

        //向存档与容器系统提供成品集合。
        public ThingOwner GetDirectlyHeldThings()
        {
            return products;
        }

        //把成品内部可能包含的容器交给原版遍历。
        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, products);
        }
    }
}
