
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.sync
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
        float alt;
        void Update()
        {
            if (!syncManager.localRootRefPoint || !syncManager.localPlayerPosition) return;
            alt = syncManager.localPlayerPosition._position.y + syncManager.localRootRefPoint._position.y;
            alt += 1000f * (syncManager.localPlayerPosition._kmPosition.y + syncManager.localRootRefPoint._kmPosition.y);
            playerAlt[0] = alt;
        }
    }
}