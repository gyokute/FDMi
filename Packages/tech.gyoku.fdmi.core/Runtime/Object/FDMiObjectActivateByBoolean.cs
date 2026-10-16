using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    public class FDMiObjectActivateByBoolean : FDMiBehaviour
    {
        public string ValuePath = "~/InZone";

        [FDMiDataPath(nameof(ValuePath)), FDMiRegisterCallback(nameof(OnChange))]
        public FDMiBool Value;

        public bool onWhenDisable;

        public void OnChange()
        {
            gameObject.SetActive(Value.Data ^ onWhenDisable);
        }
    }
}
