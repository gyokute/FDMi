
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;
namespace tech.gyoku.FDMi.avionics
{
    public class FDMiSoundByValue : FDMiBehaviour
    {
        [SerializeField] AudioSource sound;
        [SerializeField] FDMiFloat Value;
        [SerializeField] AnimationCurve turnOnCurve;
        [SerializeField] private bool moveOnValueChange, moveOnUpdate, overridePlaying;
        void Start()
        {
            if (moveOnValueChange) Value.subscribe(this, "OnChange");
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
            if (turnOnCurve.Evaluate(Value.Data) > 0.5f)
            {
                if (!sound.gameObject.activeSelf) sound.gameObject.SetActive(true);
                if (overridePlaying) { sound.Play(); }
            }
        }
    }
}