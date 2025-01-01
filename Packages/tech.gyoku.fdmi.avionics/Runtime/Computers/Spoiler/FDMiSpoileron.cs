
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.avionics
{

    public class FDMiSpoileron : FDMiBehaviour
    {
        public FDMiFloat SpoilerL, SpoilerR, SpoilerLever, RollOutput;
        [SerializeField] float moveSpeed;
        float[] lever, roll, sl, sr;

        void Start()
        {
            lever = SpoilerLever.data;
            roll = RollOutput.data;
            sl = SpoilerL.data;
            sr = SpoilerR.data;
        }
        float movedt;
        void Update()
        {
            // GroundSpoiler Active
            if (lever[0] > 1f)
            {
                sl[0] = lever[0];
                sr[0] = lever[0];
                return;
            }
            movedt = moveSpeed * Time.deltaTime;
            sl[0] = Mathf.MoveTowards(sl[0], Mathf.Clamp01(lever[0] - roll[0]), movedt);
            sr[0] = Mathf.MoveTowards(sr[0], Mathf.Clamp01(lever[0] + roll[0]), movedt);
        }
    }
}