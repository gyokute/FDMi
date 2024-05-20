
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public class FDMiSoundController : FDMiBehaviour
    {
        [SerializeField] AudioSource sound;
        public FDMiFloat SoundVolume, SoundPitch;
        [SerializeField] AnimationCurve gainCurve, pitchCurve;
        [SerializeField] bool useDopplerEffect;
        float[] vol, pitch;
        VRCPlayerApi local;

        void Start()
        {
            vol = SoundVolume.data;
            pitch = SoundPitch.data;
        }
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (player.isLocal) local = player;
        }
        Vector3 movingVec, prevPos;
        float relativeSpeed, dopplerPitch = 1f;
        void Update()
        {
            if (local != null && useDopplerEffect)
            {
                movingVec = (transform.position - local.GetPosition() - prevPos) / Time.deltaTime;
                relativeSpeed = Mathf.Max(Vector3.Dot(movingVec, prevPos.normalized), -255f);
                prevPos = transform.position - local.GetPosition();
                dopplerPitch = Mathf.MoveTowards(dopplerPitch, 340f / (340f + relativeSpeed), Time.deltaTime);

            }
            sound.volume = gainCurve.Evaluate(vol[0]);
            sound.pitch = pitchCurve.Evaluate(pitch[0]) * dopplerPitch;

        }

    }
}
