
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace tech.gyoku.FDMi.core
{
    public class FDMiObjectActivator : FDMiAttribute
    {
        [SerializeField] FDMiBool InZone, IsPilot;
        [SerializeField] bool enableInZone, enableIsPilot;
        [SerializeField] bool disableInZone, disableIsPilot;
        void Start()
        {
            if (enableInZone || disableInZone) InZone.subscribe(this, "OnChangeInZone");
            if (enableIsPilot || disableIsPilot) IsPilot.subscribe(this, "OnChangeIsPilot");
            OnChangeInZone();
            OnChangeIsPilot();
        }
        public void OnChangeInZone()
        {
            if (enableInZone) gameObject.SetActive(InZone.data[0]);
            if (disableInZone) gameObject.SetActive(!InZone.data[0]);
        }
        public void OnChangeIsPilot()
        {
            if (enableIsPilot) gameObject.SetActive(IsPilot.data[0]);
            if (disableIsPilot) gameObject.SetActive(!IsPilot.data[0]);
        }
    }
}