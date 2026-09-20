using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace MunoRaceLib.MunoRecording
{
    //负责摄像机的录制提示灯、完成闪光、快门声和对象反馈。
    public static class MunoCameraEffects
    {
        //在取景阶段于摄影者上方显示短促的红色录制指示。
        public static void RecordingPulse(Pawn photographer)
        {
            FleckMaker.Static(photographer.DrawPos + new Vector3(0f, 0f, 0.65f), photographer.Map,
                DefDatabase<FleckDef>.GetNamed("Muno_CameraRecordingLight"), 0.12f);
        }

        //在录制完成且摄像机尚未放下时播放局部闪光与摄影机快门音效。
        public static void Shutter(Pawn photographer, Pawn subject)
        {
            Vector3 direction = (subject.DrawPos - photographer.DrawPos).normalized;
            FleckMaker.Static(photographer.DrawPos + direction * 0.35f, photographer.Map,
                DefDatabase<FleckDef>.GetNamed("Muno_CameraFlash"), 0.45f);
            DefDatabase<SoundDef>.GetNamed("Muno_CameraShutter").PlayOneShot(new TargetInfo(photographer));
        }

        //在收机结束且片段成功提交后显示完成提示。
        public static void Finished(Pawn subject)
        {
            MoteMaker.ThrowText(subject.DrawPos, subject.Map, "片段已记录", new Color(0.6f, 0.9f, 1f), 0.8f);
        }
    }
}
