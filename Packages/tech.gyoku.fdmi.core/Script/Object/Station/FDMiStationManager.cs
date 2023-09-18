﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public class FDMiStationManager : FDMiAttribute
    {
        public FDMiStation[] stations;
        public FDMiBool IsPilot, InZone;
        void Start()
        {
            InZone.subscribe(this, "OnChangeInZone");
        }
        #region Owner Transfer
        public void OnChangeInZone()
        {
            if (InZone.data[0] == false)
                TryDelegatePilot();
        }
        public void TryTakePilot(int pilotPriority)
        {
            for (int i = 0; i < stations.Length; i++)
            {
                if (stations[i].pilotPriority > pilotPriority) break;
                if (stations[i].seatedPlayer.isLocal)
                {
                    IsPilot.Data = true;
                    objectManager.takeOwnerOfAllAttributes();
                    return;
                }
                if (stations[i].seatedPlayer != null) return;
            }

        }
        public void TryDelegatePilot()
        {
            if (!Networking.IsOwner(objectManager.gameObject)) return;
            IsPilot.Data = false;
            for (int i = 0; i < stations.Length; i++)
            {
                VRCPlayerApi sp = stations[i].seatedPlayer;
                if (sp != null)
                {
                    if (sp.isLocal) objectManager.takeOwnerOfAllAttributes();
                    return;
                }
            }
        }
        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if (!player.isLocal) IsPilot.Data = false;
        }
        #endregion
    }
}