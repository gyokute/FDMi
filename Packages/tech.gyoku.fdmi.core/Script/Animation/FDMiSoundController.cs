
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public class FDMiSoundController : FDMiBehaviour
    {
        [SerializeField] AudioSource sound;
        [SerializeField] FDMiFloat SoundVolume, SoundPitch;
        [SerializeField] AnimationCurve gainCurve, pitchCurve;
        float[] vol, pitch;

        void Start()
        {
            vol = SoundVolume.data;
            pitch = SoundPitch.data;
        }

        void Update()
        {
            sound.volume = gainCurve.Evaluate(vol[0]);
            sound.pitch = pitchCurve.Evaluate(pitch[0]);
        }

    }
}
