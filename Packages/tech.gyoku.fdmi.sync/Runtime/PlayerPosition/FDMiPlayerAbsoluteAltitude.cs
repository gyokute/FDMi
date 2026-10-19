using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using FDMi.core;

namespace FDMi.sync
{
    public class FDMiPlayerAbsoluteAltitude : FDMiBehaviour
    {
        public FDMiFloat LocalPlayerAltitude;
        public FDMiRelativeObjectSyncManager syncManager;
        float[] playerAlt;

        void Start()
        {
            playerAlt = LocalPlayerAltitude.data;
        }
        void Update()
        {
            if (!syncManager.localRootRefPoint || !syncManager.localPlayerPosition) return;
            Vector3 rootTotalM = syncManager.localRootRefPoint.getTotalPositionM();
            Vector3 playerLocalM = syncManager.localPlayerPosition.getTotalPositionM();
            Vector3 ecefM = rootTotalM + syncManager.localRootRefPoint._rotation * playerLocalM;
            // Why-not: 球面化により高度は動径で定義する (旧 Y 合算は平面近似)。
            playerAlt[0] = ecefM.magnitude - FDMiReferencePoint.EARTH_RADIUS_M;
        }
    }
}
