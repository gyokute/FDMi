
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public class FDMiZoneTrigger : FDMiBehaviour
    {
        public FDMiBool Boolean;
        [SerializeField] bool detectEnter = true, detectExit = true;

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (player.isLocal && detectEnter) Boolean.Data = true;
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (player.isLocal && detectExit) Boolean.Data = false;
        }
    }
}
