
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.avionics
{

    public class FDMiGroundSpoiler : FDMiBehaviour
    {
        [SerializeField] FDMiFloat SpoilerLever, GroundSpoilerLever;
        [SerializeField] FDMiBool[] IsGround;
        [SerializeField] float SpoilerLeverMaxMove = 1.2f, MoveSpeed = 1f;
        float[] gsLever, spoiler;
        private bool armed = false;
        void Start()
        {
            spoiler = SpoilerLever.data;
            gsLever = GroundSpoilerLever.data;
            GroundSpoilerLever.subscribe(this, nameof(OnChangeGSLever));
            gameObject.SetActive(false);
        }
        int i;
        void Update()
        {
            armed = false;
            for (i = 0; i < IsGround.Length; i++)
            {
                armed = armed || IsGround[i].data[0];
            }
            SpoilerLever.Data = Mathf.MoveTowards(spoiler[0], armed ? SpoilerLeverMaxMove : 0, MoveSpeed * Time.deltaTime);
            if (Mathf.Approximately(spoiler[0], SpoilerLeverMaxMove)) GroundSpoilerLever.Data = 0f;
        }

        public void OnChangeGSLever()
        {
            gameObject.SetActive(gsLever[0] > 0.75f);
        }
    }
}