
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
        private bool havePilot = false;
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
        public void TryTakePilot()
        {
            if (havePilot) return;
            IsPilot.Data = true;
            objectManager.takeOwnerOfAllAttributes();
        }
        public void TryDelegatePilot()
        {
            for (int i = 0; i < stations.Length; i++)
            {
                VRCPlayerApi sp = stations[i].seatedPlayer;
                if (sp != null)
                {
                    if(sp.isLocal) objectManager.takeOwnerOfAllAttributes();
                    return;
                }
            }
            // If pilot is not found.
            havePilot = false;
        }
        #endregion
    }
}