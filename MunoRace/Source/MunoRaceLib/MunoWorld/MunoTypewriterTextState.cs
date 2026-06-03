using UnityEngine;

namespace MunoRaceLib.MunoWorld
{
    /// <summary>
    /// 负责驱动通讯文本按打字机效果逐字显示。
    /// </summary>
    public class MunoTypewriterTextState
    {
        private const float CharactersPerSecond = 28f;

        private string fullText = string.Empty;
        private float startTime;
        private bool completed;

        /// <summary>
        /// 返回当前文本是否已经完整显示。
        /// </summary>
        public bool Completed => completed || VisibleCharacterCount() >= fullText.Length;

        /// <summary>
        /// 设置新的完整文本，并从开头重新播放打字机效果。
        /// </summary>
        public void SetText(string text)
        {
            fullText = text ?? string.Empty;
            startTime = Time.realtimeSinceStartup;
            completed = fullText.Length == 0;
        }

        /// <summary>
        /// 立即显示完整文本。
        /// </summary>
        public void Complete()
        {
            completed = true;
        }

        /// <summary>
        /// 返回当前应绘制的可见文本片段。
        /// </summary>
        public string VisibleText()
        {
            if (completed || fullText.Length == 0)
            {
                return fullText;
            }

            int count = Mathf.Clamp(VisibleCharacterCount(), 0, fullText.Length);
            return fullText.Substring(0, count);
        }

        /// <summary>
        /// 计算按当前时间应显示的字符数量。
        /// </summary>
        private int VisibleCharacterCount()
        {
            return Mathf.FloorToInt((Time.realtimeSinceStartup - startTime) * CharactersPerSecond);
        }
    }
}
