using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlight_FDMi
{
    public class FDMi_Debug : FDMi_Attributes
    {
        #region DEBUG
        // For Debug
        public string[] debugLog;
        public Text TimeText, DebugText, boolText, airDataText;
        private int updatedIndex = 0;
        private TextGenerator debugTG;
        public string debug
        {
            get { return debugLog[updatedIndex]; }
            set
            {
                var now = System.DateTime.Now;
                debugLog[updatedIndex] = now.Minute + ":" + now.Second + ">" + value + "\n";
                updatedIndex = (updatedIndex + 1) % debugLog.Length;
            }
        }

        private void LateUpdate()
        {
            if (!sharedBool[0]) return;
            var now = System.DateTime.Now;
            boolText.text = "";
            airDataText.text = "";
            TimeText.text = now.ToLongTimeString();

            foreach (var i in sharedBool)
            {
                boolText.text += i.ToString() + "\n";
            }
            foreach (var i in airData)
            {
                airDataText.text += i.ToString() + "\n";
            }

            if (updatedIndex > 0)
            {
                DebugText.text = "";
                for (int i = 0; i < debugLog.Length; i++)
                {
                    DebugText.text += debugLog[(updatedIndex + i) % debugLog.Length];
                }
            }
        }
        #endregion
    }
}