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
        [Tooltip("高度の出力パス")]
        public string AltitudePath = "PlayerAltitude";

        [FDMiDataPath(nameof(AltitudePath))]
        [Tooltip("プレイヤーの高度 [m]。球面近似")]
        public FDMiFloat PlayerAltitude;

        [Tooltip("プレイヤーの絶対位置を読む Manager")]
        public FDMiSyncManager syncManager;

        float[] playerAlt;

        void Start()
        {
            playerAlt = PlayerAltitude.data;
        }

        void Update()
        {
            FDMiSyncPlayer anchor = syncManager._localPlayerAnchor;
            if (!anchor)
                return;

            long[] abs = anchor._localAbs;
            Vector3 p = new Vector3((float)abs[0] * 0.001f, (float)abs[1] * 0.001f, (float)abs[2] * 0.001f);
            playerAlt[0] = p.magnitude - 6371000f;
        }
    }
}
