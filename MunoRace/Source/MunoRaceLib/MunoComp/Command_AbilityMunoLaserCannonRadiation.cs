using RimWorld;
using Verse;

namespace MunoRaceLib.MunoComp
{
    //显示激光炮辐射放射按钮，并在按钮右上角显示武器自身槽位数量。
    public class Command_AbilityMunoLaserCannonRadiation : Command_Ability
    {
        //绑定原版能力命令需要的能力与施放者。
        public Command_AbilityMunoLaserCannonRadiation(Ability ability, Pawn pawn) : base(ability, pawn)
        {
        }

        //返回激光炮自身浓浆槽数量，共享储存罐数量由独立槽位 Gizmo 展示。
        public override string TopRightLabel
        {
            get
            {
                Comp_GalactogenStorageWeapon storage = Pawn?.equipment?.Primary?.GetComp<Comp_GalactogenStorageWeapon>();
                return storage == null ? base.TopRightLabel : storage.SlotCount + "/" + storage.SlotCapacity;
            }
        }
    }
}
