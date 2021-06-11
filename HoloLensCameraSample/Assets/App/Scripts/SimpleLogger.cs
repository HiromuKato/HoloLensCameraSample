using UnityEngine;

namespace HoloLensCameraSample
{
    /// <summary>
    /// シンプルなログ表示クラス
    /// </summary>
    public class SimpleLogger : MonoBehaviour
    {
        [SerializeField]
        private TextMesh textMesh;

        private int lineCount = 0;

        private void OnEnable()
        {
            textMesh.text = "";
            Application.logMessageReceived += OnReceiveLog;
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnReceiveLog;
        }

        /// <summary>
        /// ログを受け取った時の処理
        /// </summary>
        /// <param name="logText"></param>
        /// <param name="stackTrace"></param>
        /// <param name="logType"></param>
        private void OnReceiveLog(string logText, string stackTrace, LogType logType)
        {
            // 100行を超えたら消す
            lineCount++;
            if (lineCount > 100)
            {
                lineCount = 0;
                textMesh.text = "";
            }

            if (logType == LogType.Error || logType == LogType.Exception)
            {
                var colBegin = "<color='red'>";
                var colEnd = "</color>";
                textMesh.text += colBegin + logText + colEnd + "\n";
            }
            else if (logType == LogType.Warning)
            {
                var colBegin = "<color='yellow'>";
                var colEnd = "</color>";
                textMesh.text += colBegin + logText + colEnd + "\n";
            }
            else
            {
                textMesh.text += logText + "\n";
            }
        }
    }
}
