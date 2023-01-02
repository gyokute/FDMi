
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


namespace SaccFlight_FDMi
{
    public class FDMi_AnimationClipController : UdonSharpBehaviour
    {
        public FDMi_SyncObject input;
        public float min = 0f, max = 0f, val, delay = 1f;
        public AnimationClip clip;
        public GameObject Root;

        void Update()
        {
            float targetVal = Mathf.InverseLerp(min, max, input.val);
            val = Mathf.MoveTowards(val, targetVal, Time.deltaTime / delay);
            clip.SampleAnimation(Root, clip.length * val);
            if (Mathf.Approximately(val, targetVal)) gameObject.SetActive(false);
        }
    }
}