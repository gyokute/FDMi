
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlight_FDMi
{
    public class FDMi_LampIndicator : FDMi_Indicator
    {
        public FDMi_InputObject input;
        public float threshould = 0.5f;
        public bool changeEmittion;
        [ColorUsage(false, true)] public Color offEmissionColor;
        [ColorUsage(false, true)] public Color onEmissionColor;
        Material mat;
        private int emitId;


        void Start()
        {
            mat = GetComponent<Renderer>().material;
            emitId = VRCShader.PropertyToID("_EmissionColor");
            whenChange();
        }

        public override void whenChange()
        {
            if (input.val > threshould)
            {
                if (changeEmittion) mat.SetColor(emitId, onEmissionColor);
            }
            else
            {
                if (changeEmittion) mat.SetColor(emitId, offEmissionColor);
            }

        }

        private void OnDestroy()
        {
            if (mat != null) Destroy(mat);
        }
    }
}