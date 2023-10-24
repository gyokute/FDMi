
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace tech.gyoku.FDMi.core
{
    public class FDMiObjectActivator : FDMiBehaviour
    {
        [SerializeField] FDMiBool Boolean;
        [SerializeField] bool onWhenDisable;
        void Start()
        {
            Boolean.subscribe(this, "OnChange");
            OnChange();
        }
        public void OnChange() { gameObject.SetActive(Boolean.data[0] ^ onWhenDisable); }

    }
}