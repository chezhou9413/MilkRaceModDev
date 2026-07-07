using RimWorld;
using Verse;

namespace MunoRaceLib.MunoComp
{
    //为缪诺激光步枪和激光狙击枪的增幅横扫模式提供浓浆消耗校验。
    public class Verb_MunoLaserSweep : Verb_ShootBeam
    {
        private const int SweepCost = 1;

        private Comp_GalactogenStorageWeapon StorageComp
        {
            get { return EquipmentSource?.GetComp<Comp_GalactogenStorageWeapon>(); }
        }

        //检查横扫模式是否拥有足够浓浆；横扫进行中不重复扣费。
        public override bool Available()
        {
            if (!base.Available())
            {
                return false;
            }

            if (state == VerbState.Bursting)
            {
                return true;
            }

            return StorageComp != null && StorageComp.HasEnough(SweepCost);
        }

        //校验目标前先给浓浆不足状态提供明确提示。
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (StorageComp == null || !StorageComp.HasEnough(SweepCost))
            {
                if (showMessages)
                {
                    Messages.Message("激光增幅浓浆槽不足，无法横扫。", CasterPawn, MessageTypeDefOf.RejectInput, false);
                }

                return false;
            }

            return base.ValidateTarget(target, showMessages);
        }

        //暖机完成时扣除一次浓浆，并让原版 Beam 横扫流程继续执行。
        public override void WarmupComplete()
        {
            if (StorageComp == null || !StorageComp.ConsumeSlot(SweepCost))
            {
                Messages.Message("激光增幅浓浆槽不足，无法横扫。", CasterPawn, MessageTypeDefOf.RejectInput, false);
                Reset();
                return;
            }

            base.WarmupComplete();
        }
    }
}
