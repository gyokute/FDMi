using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.sync
{
    public class FDMiBodyKinematicManagement : FDMiBehaviour
    {
        public string IsRootPath = "~/IsRoot";

        [FDMiDataPath(nameof(IsRootPath)), FDMiRegisterCallback(nameof(OnChange))]
        public FDMiBool IsRoot;

        public string IsPilotPath = "~/IsPilot";

        [FDMiDataPath(nameof(IsPilotPath)), FDMiRegisterCallback(nameof(OnChange))]
        public FDMiBool IsPilot;
        public Rigidbody body;

        public FDMiBehaviour[] inPlaneBehaviours;

        public void OnChange()
        {
            body.isKinematic = !(IsRoot.Data && IsPilot.Data);

            if (!(IsRoot.Data && IsPilot.Data))
                return;

            VRCPlayerApi player = Networking.LocalPlayer;
            foreach (var i in inPlaneBehaviours)
            {
                Networking.SetOwner(player, i.gameObject);
            }
        }
    }
}
