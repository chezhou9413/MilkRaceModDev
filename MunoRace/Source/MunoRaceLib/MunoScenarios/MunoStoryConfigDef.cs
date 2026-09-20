using System.Collections.Generic;
using Verse;

namespace MunoRaceLib.MunoScenarios
{
    //集中配置摄像产量、叛逃追击节奏和结局时间边界。
    public class MunoStoryConfigDef : Def
    {
        public FloatRange recordingIntervalDays = new FloatRange(0.1f, 0.25f);
        public float recordingRadius = 12f;
        public int recordingsPerTape = 4;
        public int recordingDurationTicks = 300;
        public float pursuitGraceDays = 5f;
        public FloatRange pursuitIntervalDays = new FloatRange(5f, 8f);
        public FloatRange pursuitPoints = new FloatRange(100f, 800f);
        public float pursuitGrowthPerWave = 0.15f;
        public float earlyEndingDays = 7f;
        public List<MunoRecordingRewardDef> recordingRewards = new List<MunoRecordingRewardDef>();

        //读取本模组唯一的剧情数值配置。
        public static MunoStoryConfigDef Current => DefDatabase<MunoStoryConfigDef>.GetNamed("Muno_StoryConfig");

        //检查会导致计时无效或奖励无法生成的配置错误。
        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors()) yield return error;
            if (recordingIntervalDays.min <= 0f || recordingIntervalDays.max < recordingIntervalDays.min
                || recordingRadius <= 0f || recordingsPerTape < 1 || recordingDurationTicks < 120)
                yield return "摄像间隔、半径和每带片段数必须为正数，区间必须有序。";
            if (pursuitGraceDays <= 0f || pursuitIntervalDays.min <= 0f
                || pursuitIntervalDays.max < pursuitIntervalDays.min || pursuitPoints.min <= 0f
                || pursuitPoints.max < pursuitPoints.min || pursuitGrowthPerWave < 0f || earlyEndingDays <= 0f)
                yield return "追击和结局配置存在无效数值。";
            if (recordingRewards.Count == 0) yield return "录像奖励池不能为空。";
        }
    }
}
