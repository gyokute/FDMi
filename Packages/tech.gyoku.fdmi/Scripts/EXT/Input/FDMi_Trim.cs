
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlight_FDMi
{
    public class FDMi_Trim : FDMi_SyncObject
    {
        public FDMi_InputObject trimInput;
        public float min, max;
        public void LateUpdate()
        {
            if (Mathf.Abs(trimInput.val) > 0.001f)
            {
                val += trimInput.val * Time.deltaTime;
                Val =Mathf.Clamp(val, min, max);
                RequestSerialization();
            }
        }
    }
}