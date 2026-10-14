using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    public class FDMiGroundSpoiler : FDMiBehaviour
    {
        public string SpoilerLeverPath = "SpoilerLever";

        [FDMiDataPath(nameof(SpoilerLeverPath))]
        public FDMiFloat SpoilerLever;
        public string GroundSpoilerLeverPath = "GroundSpoilerLever";

        [FDMiDataPath(nameof(GroundSpoilerLeverPath))]
        [FDMiRegisterCallback(nameof(OnChangeGSLever))]
        public FDMiFloat GroundSpoilerLever;
        public string AnyIsGroundPath = "AnyIsGround";

        [FDMiDataPath(nameof(AnyIsGroundPath))]
        public FDMiBool AnyIsGround;

        [SerializeField] float SpoilerLeverMaxMove = 1.2f, MoveSpeed = 1f;

        void Start()
        {
            spoiler = SpoilerLever.data;
            gsLever = GroundSpoilerLever.data;
            anyGround = AnyIsGround.data;
            this.enabled = false; // 既定は無効。レバー操作(コールバック)で復帰させる
        }

        void Update()
        {
            SpoilerLever.Data = Mathf.MoveTowards(spoiler[0], anyGround[0] ? SpoilerLeverMaxMove : 0, MoveSpeed * Time.deltaTime);
            if (Mathf.Approximately(spoiler[0], SpoilerLeverMaxMove)) GroundSpoilerLever.Data = 0f;
        }

        public void OnChangeGSLever()
        {
            this.enabled = gsLever[0] > 0.75f; // Update停止中でもコールバックは到達する
        }

        float[] gsLever, spoiler;
        bool[] anyGround;
    }
}
