using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.sync
{
    [DefaultExecutionOrder(1100)]
    public class FDMiPlayerAltitude : FDMiBehaviour
    {
        public string AltitudePath = "PlayerAltitude";

        [FDMiDataPath(nameof(AltitudePath))]
        public FDMiFloat PlayerAltitude;

        public FDMiSyncManager syncManager;
        private FDMiSyncPlayer _localPlayerAnchor;

        float[] playerAlt;
        long[] abs;

        void Start()
        {
            playerAlt = PlayerAltitude.data;
        }

        void Update()
        {
            if (!syncManager._localPlayerAnchor)
                return;
            abs = syncManager._localPlayerAnchor._localAbs;
            Vector3 p = new Vector3((float)abs[0] * 0.001f, (float)abs[1] * 0.001f, (float)abs[2] * 0.001f);
            playerAlt[0] = p.magnitude - 6371000f;
        }
    }
}
