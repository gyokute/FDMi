using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    [DefaultExecutionOrder(1000)]
    public class FDMiObjectActivateByBoolean : FDMiBehaviour
    {
        public string ValuePath = "~/InZone";

        [FDMiDataPath(nameof(ValuePath)), FDMiRegisterCallback(nameof(OnChange))]
        public FDMiBool Value;

        public bool onWhenDisable;

        private void Start()
        {
            OnChange();
        }

        public void OnChange()
        {
            gameObject.SetActive(Value.Data ^ onWhenDisable);
        }
    }
}
