using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    public class FDMiSoundByValue : FDMiBehaviour
    {
        [SerializeField] AudioSource sound;
        public string ValuePath = "Value";

        [FDMiDataPath(nameof(ValuePath))]
        [FDMiRegisterCallback(nameof(OnChange))]
        public FDMiFloat Value;
        [SerializeField] AnimationCurve turnOnCurve;
        [SerializeField] private bool moveOnUpdate, overridePlaying;

        void Start()
        {
            val = Value.data;
            sound.gameObject.SetActive(false);
        }

        void Update()
        {
            if (!sound.isPlaying)
            {
                if (sound.gameObject.activeSelf) sound.gameObject.SetActive(false);
            }
            if (moveOnUpdate) OnChange();
        }

        public void OnChange()
        {
            if (turnOnCurve.Evaluate(val[0]) > 0.5f)
            {
                if (!sound.gameObject.activeSelf) sound.gameObject.SetActive(true);
                if (!sound.isPlaying || overridePlaying) sound.Play();
            }
        }

        float[] val;
    }
}
