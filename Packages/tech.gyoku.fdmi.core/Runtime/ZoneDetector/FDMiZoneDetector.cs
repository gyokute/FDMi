using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    public class FDMiZoneDetector : FDMiBehaviour
    {
        [FDMiDataPathConst("~/InZone")]
        public FDMiBool InZone;

        [FDMiDataPathConst("~/IsPilot")]
        public FDMiBool IsPilot;

        [FDMiDataPathConst("~/HasPilot")]
        public FDMiBool HasPilot;

        [SerializeField]
        private bool DetectWhenEnter = true;

        [SerializeField]
        private bool DetectWhenExit = true;

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (player.isLocal && DetectWhenEnter)
                InZone.Data = true;
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (player.isLocal && DetectWhenExit)
            {
                InZone.Data = false;
                if (!IsPilot || !HasPilot)
                    return;
                if (IsPilot.Data == true)
                {
                    IsPilot.Data = false;
                    HasPilot.Data = false;
                }
            }
        }
    }
}
