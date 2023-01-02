
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlight_FDMi
{
    public class FDMi_GroundSpoiler : FDMi_SyncObject
    {
        public FDMi_Lever SpoilerLever;
        public float targetDetent;
        private bool gotoTgt = false;
        public void whenLand()
        {
            if (val > 0.7f)
                gotoTgt = true;
            if (gotoTgt)
            {
                Val =0f;
                RequestSerialization();
                SpoilerLever.Val = Mathf.MoveTowards(SpoilerLever.val, targetDetent, Time.deltaTime);
                if (Mathf.Approximately(SpoilerLever.val, targetDetent)) gotoTgt = false;
                SpoilerLever.RequestSerialization();
            }
        }
    }
}