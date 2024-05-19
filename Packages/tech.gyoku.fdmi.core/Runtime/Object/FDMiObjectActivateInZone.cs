
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public class FDMiObjectActivateInZone : FDMiBehaviour
    {
        public FDMiBool InZone;
        public bool onWhenDisable;
        void Start()
        {
            InZone.subscribe(this, "OnChange");
            OnChange();
        }
        public void OnChange() { gameObject.SetActive(InZone.data[0] ^ onWhenDisable); }
    }
}