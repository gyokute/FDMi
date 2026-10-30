using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.input
{
    public class FDMiDefaultInputGroup : FDMiHandInputGroup
    {
        [Tooltip("この入力を受けるコンタクトタグ")]
        public string matchingTag = "FingerIndexL";

        void OnEnable()
        {
            foreach (var t in Trackers)
            {
                t.SetDefaultInput(matchingTag, this);
            }
        }

        void OnDisable()
        {
            foreach (var t in Trackers)
            {
                t.UnsetDefaultInput(this);
            }
        }
    }
}
