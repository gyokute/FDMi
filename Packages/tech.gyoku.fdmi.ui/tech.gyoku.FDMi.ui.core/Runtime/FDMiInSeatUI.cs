
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace tech.gyoku.FDMi.ui.core
{
    public class FDMiInSeatUI : UdonSharpBehaviour
    {
        public Transform UIParentInStandby, UIParentInSeat;
        public Vector3 uiAnchorPos;

        public void FDMiOnSeatEnter()
        {
            if (!UIParentInSeat) return;
            transform.parent = UIParentInSeat;

            transform.localPosition = uiAnchorPos;
            transform.localRotation = Quaternion.identity;
            transform.localScale = UIParentInSeat.localScale;
        }
        public void FDMiOnSeatExit()
        {
            if (!UIParentInStandby) return;
            transform.parent = UIParentInStandby;

        }
    }
}