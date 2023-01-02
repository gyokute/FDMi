
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlight_FDMi
{
    public class FDMi_ExteriorLight : FDMi_Avionics
    {
        public FDMi_InputObject SW;
        public GameObject[] turnOnWhenOn, turnOnWhenOff, turnOffWhenOn, turnOffWhenOff;
        public Shader lightShader;
        public float threshould = 0.5f;
        public float flashFrequency = 0f, distanceSizeCoef = 0.01f, emitIntensity = 5f;

        public bool changeEmittion;
        [ColorUsage(false, true)] public Color offEmissionColor;
        [ColorUsage(false, true)] public Color onEmissionColor;

        Material mat;
        private int emitId;


        void Start()
        {
            mat = GetComponent<Renderer>().material;
            emitId = VRCShader.PropertyToID("_EmissionColor");
            mat.SetFloat("_distMul", distanceSizeCoef);
            mat.SetFloat("_freq", flashFrequency);
            mat.SetFloat("_emitIntensity", emitIntensity);
            mat.DisableKeyword("_EMISSION");
            whenChange();
        }

        public override void whenChange()
        {
            if (SW.val > threshould)
            {
                mat.EnableKeyword("_EMISSION");
                foreach (GameObject ton in turnOnWhenOn) ton.SetActive(true);
                foreach (GameObject toff in turnOffWhenOn) toff.SetActive(false);
                if (changeEmittion) mat.SetColor(emitId, onEmissionColor);
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                foreach (GameObject ton in turnOnWhenOff) ton.SetActive(true);
                foreach (GameObject toff in turnOffWhenOff) toff.SetActive(false);
                if (changeEmittion) mat.SetColor(emitId, offEmissionColor);
            }

        }

        private void OnDestroy()
        {
            if (mat != null) Destroy(mat);
        }

    }
}