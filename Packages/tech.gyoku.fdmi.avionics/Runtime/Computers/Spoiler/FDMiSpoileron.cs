using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    public class FDMiSpoileron : FDMiBehaviour
    {
        public string SpoilerLPath = "SpoilerL";

        [FDMiDataPath(nameof(SpoilerLPath))]
        public FDMiFloat SpoilerL;
        public string SpoilerRPath = "SpoilerR";

        [FDMiDataPath(nameof(SpoilerRPath))]
        public FDMiFloat SpoilerR;
        public string SpoilerLeverPath = "SpoilerLever";

        [FDMiDataPath(nameof(SpoilerLeverPath))]
        public FDMiFloat SpoilerLever;
        public string RollOutputPath = "RollOutput";

        [FDMiDataPath(nameof(RollOutputPath))]
        public FDMiFloat RollOutput;

        [SerializeField] float moveSpeed;

        void Start()
        {
            lever = SpoilerLever.data;
            roll = RollOutput.data;
            sl = SpoilerL.data;
            sr = SpoilerR.data;
        }

        void Update()
        {
            // グラウンドスポイラー展開中は左右同値
            if (lever[0] > 1f)
            {
                SpoilerL.Data = lever[0];
                SpoilerR.Data = lever[0];
                return;
            }
            float movedt = moveSpeed * Time.deltaTime;
            SpoilerL.Data = Mathf.MoveTowards(sl[0], Mathf.Clamp01(lever[0] - roll[0]), movedt);
            SpoilerR.Data = Mathf.MoveTowards(sr[0], Mathf.Clamp01(lever[0] + roll[0]), movedt);
        }

        float[] lever, roll, sl, sr;
    }
}
