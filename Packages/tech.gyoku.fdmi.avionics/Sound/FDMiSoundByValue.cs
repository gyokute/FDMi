
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
        }
        void Update()
        {
            if (moveOnUpdate) OnChange();
        }
        public void OnChange()
        {
            if (turnOnCurve.Evaluate(Value.Data) > 0.5f)
            {
                if (overridePlaying || !sound.isPlaying) sound.Play();
            }
        }
    }
}