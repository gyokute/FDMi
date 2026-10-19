
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using FDMi.core;

namespace FDMi.sync
{
    public class FDMiSkyboxMaterialtByAltitude : FDMiBehaviour
    {
        public FDMiFloat LocalPlayerAltitude;
        [SerializeField] private Renderer renderer;
        [SerializeField] private AnimationCurve exposureCurve;
        private int exposureId;
        float[] playerAlt;
        private Material mat;
        void Start()
        {
            mat = renderer.material;
            playerAlt = LocalPlayerAltitude.data;
            exposureId = VRCShader.PropertyToID("_Exposure");
        }
        void Update()
        {
            mat.SetFloat(exposureId, exposureCurve.Evaluate(playerAlt[0]));
        }
    }
}