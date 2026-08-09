using MunoRaceLib.MunoDefRef;
using RimWorld;
using Verse;

namespace MunoRaceLib.MunoComp
{
    //承载激光炮辐射放射能力，并确保施放期间仍装备正确武器且拥有足够浓浆。
    public class Ability_MunoLaserCannonRadiation : Ability
    {
        //供能力存读与反射创建使用。
        public Ability_MunoLaserCannonRadiation()
        {
        }

        //为指定小人创建激光炮辐射放射能力实例。
        public Ability_MunoLaserCannonRadiation(Pawn pawn, AbilityDef def) : base(pawn, def)
        {
        }

        //检查原版能力状态、当前主武器和自身槽加储存罐备用槽是否允许施放。
        public override AcceptanceReport CanCast
        {
            get
            {
                AcceptanceReport report = base.CanCast;
                if (!report.Accepted)
                {
                    return report;
                }

                ThingWithComps weapon = pawn?.equipment?.Primary;
                if (weapon?.def != MunoDefDataRef.Gun_MunoLC)
                {
                    return "未装备缪诺激光炮。";
                }

                VerbProperties_MunoRadiationCone props = verb?.verbProps as VerbProperties_MunoRadiationCone;
                Comp_GalactogenStorageWeapon storage = weapon.GetComp<Comp_GalactogenStorageWeapon>();
                if (props == null || storage == null)
                {
                    return "缪诺激光炮缺少辐射放射配置。";
                }

                if (!GalactogenStorageUtility.HasEnough(pawn, storage, props.storageCost))
                {
                    return "激光炮浓浆槽与储存罐均不足。";
                }

                return true;
            }
        }
    }
}
