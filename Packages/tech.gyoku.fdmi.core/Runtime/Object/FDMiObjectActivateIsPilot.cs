
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public class FDMiObjectActivateIsPilot : FDMiBehaviour
    {
        public FDMiBool IsPilot;
        public bool onWhenDisable;
        void Start()
        {
            IsPilot.subscribe(this, "OnChange");
            OnChange();
        }
        public void OnChange() { gameObject.SetActive(IsPilot.data[0] ^ onWhenDisable); }
    }
}