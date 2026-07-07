using RimWorld;
using Verse;

namespace MunoRaceLib.MunoComp
{
    //为缪诺战略步枪的副武器榴弹模式提供弹药校验与单发消耗。
    public class Verb_MunoStrategicGrenade : Verb_Shoot
    {
        private Comp_StrategicGrenadeStorageWeapon StorageComp
        {
            get { return EquipmentSource?.GetComp<Comp_StrategicGrenadeStorageWeapon>(); }
        }

        //检查战略榴弹模式是否可用；空仓仍保留按钮，由目标校验阶段给出明确提示。
        public override bool Available()
        {
            return base.Available() && StorageComp != null;
        }

        //校验目标前先给空仓状态提供明确提示。
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (StorageComp == null || !StorageComp.HasAmmo())
            {
                if (showMessages)
                {
                    Messages.Message("战略步枪榴弹仓为空。", CasterPawn, MessageTypeDefOf.RejectInput, false);
                }

                return false;
            }

            return base.ValidateTarget(target, showMessages);
        }

        //成功发射后消耗一发榴弹。
        protected override bool TryCastShot()
        {
            bool fired = base.TryCastShot();
            if (fired)
            {
                StorageComp?.TryConsumeGrenade(CasterPawn);
            }

            return fired;
        }
    }
}
