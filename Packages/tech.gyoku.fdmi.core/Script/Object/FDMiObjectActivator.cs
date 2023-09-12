
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace tech.gyoku.FDMi.core
{
    public class FDMiObjectActivator : FDMiAttribute
    {
        public FDMiBool InZone, IsPilot;
        public bool enableInZone, enableIsPilot;
        void Start()
        {
            InZone.subscribe(this, "OnChangeInZone");
            gameObject.SetActive(false);
        }
        public void OnChangeInZone()
        {
            if (enableInZone) gameObject.SetActive(InZone.data[0]);
        }
        public void OnChangeIsPilot() { }
    }
}