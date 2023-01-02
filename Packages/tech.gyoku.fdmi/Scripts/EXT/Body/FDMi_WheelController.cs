
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlight_FDMi
{
    public class FDMi_WheelController : FDMi_Attributes
    {
        public FDMi_WheelCollider[] wheel;
        public bool isGround;
        public FDMi_GroundSpoiler GS;
        public override void FDMi_Local_Start()
        {
            gameObject.SetActive(false);
        }
        public override void SFEXT_O_PilotEnter()
        {
            gameObject.SetActive(true);
        }
        public override void SFEXT_O_PilotExit() => gameObject.SetActive(false);

        void Update()
        {
            if (!sharedBool[(int)SharedBool.initialized]) return;
            isGround = false;
            for (int i = 0; i < wheel.Length; i++)
            {
                isGround = (isGround || wheel[i].isGrounded);
            }
            if (GS != null && isGround)
                GS.whenLand();
        }

    }
}