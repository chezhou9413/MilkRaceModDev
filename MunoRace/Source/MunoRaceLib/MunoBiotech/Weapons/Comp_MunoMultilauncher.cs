using Verse;

namespace MunoRaceLib.MunoBiotech.Weapons
{
    //保存多功能发射器的模式，并提供单个循环切换按钮。
    public class Comp_MunoMultilauncher : ThingComp
    {
        private MunoSprayMode mode;

        public ThingDef SelectedProjectile => mode == MunoSprayMode.AcidGel ? MunoBiotechDefOf.Bullet_MunoAcidGel
            : mode == MunoSprayMode.MedicalFoam ? MunoBiotechDefOf.Bullet_MunoMedicalFoam
            : MunoBiotechDefOf.Bullet_MunoStrongGel;

        public string ModeLabel => mode == MunoSprayMode.AcidGel ? "酸液黏胶"
            : mode == MunoSprayMode.MedicalFoam ? "医疗泡沫" : "强力黏胶";

        //切换只影响下一轮喷射，正在飞行的弹丸保持原有效果。
        public Gizmo ModeGizmo()
        {
            return new Command_Action
            {
                defaultLabel = "喷射模式：" + ModeLabel,
                defaultDesc = "点击依次切换强力黏胶、酸液黏胶和医疗泡沫。每轮消耗 1 份乳源质浓浆，所有模式均需手动选择目标。喷射范围内的友军也会受到影响。",
                icon = SelectedProjectile.uiIcon,
                action = () => mode = (MunoSprayMode)(((int)mode + 1) % 3)
            };
        }

        //保存武器自己的模式，转交持有者后仍保留选择。
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref mode, "sprayMode");
        }

        //在物品信息中显示目前选择的喷射材料。
        public override string CompInspectStringExtra()
        {
            return "喷射模式：" + ModeLabel;
        }
    }
}
