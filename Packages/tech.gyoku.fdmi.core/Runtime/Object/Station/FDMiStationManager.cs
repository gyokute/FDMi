
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public class FDMiStationManager : FDMiAttribute
    {
        // public Transform onlyIsRoot;
        public FDMiStation[] stations;
        public FDMiBool IsPilot, InZone;
        void Start()
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            InZone.subscribe(this, "OnChangeInZone");
        }
        #region Owner Transfer
        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (player.isLocal) TryDelegatePilot();
        }
        public void OnChangeInZone()
        {
            if (InZone.data[0] != true) TryDelegatePilot();
        }

        public void TryTakePilot(int pilotPriority)
        {
            for (int i = 0; i < stations.Length; i++)
            {
                if (stations[i].seatedPlayer == null)
                {
                    if (stations[i].pilotPriority > pilotPriority) return;
                    else continue;
                }
                if (stations[i].seatedPlayer.isLocal)
                {
                    IsPilot.Data = true;
                    objectManager.takeOwnerOfAllAttributes();
                }
                return;
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