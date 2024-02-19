
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public class FDMiDialFunction : FDMiInputAddon
    {
        [HideInInspector] public Vector2 dfuncAxis;
        public FDMiInputAddon[] inputAddons;
        [SerializeField] float enterThreshold = 0.8f;
        float dfuncAngle, dfuncMagnitude, anglePerIndex;
        int selectedIndex;
        KeyCode childTriggerKey = KeyCode.JoystickButton14;

        protected override void Start()
        {
            base.Start();
            if (inputAddons.Length > 0) anglePerIndex = 360f / inputAddons.Length;
            else anglePerIndex = 360f;
        }
        protected override void Update()
        {
            base.Update();

            dfuncAxis[0] = input[(int)InputButton.PadH];
            dfuncAxis[1] = input[(int)InputButton.PadV];
            dfuncMagnitude = dfuncAxis.magnitude;
            // Select inputAddon
            if (dfuncMagnitude > enterThreshold)
            {
                dfuncAngle = -Vector2.SignedAngle(Vector2.up, dfuncAxis);
                selectedIndex = Mathf.RoundToInt(Mathf.Repeat(dfuncAngle, 360.1f) / anglePerIndex);
                selectedIndex %= inputAddons.Length;
            }
            if (inputAddons[selectedIndex] && Input.GetKeyDown(childTriggerKey))
                inputAddons[selectedIndex].OnCalled(handType);
        }
        public override void OnCalled(VRCPlayerApi.TrackingDataType trackType)
        {
            base.OnCalled(trackType);
            if (handType == VRCPlayerApi.TrackingDataType.LeftHand) childTriggerKey = KeyCode.JoystickButton14;
            if (handType == VRCPlayerApi.TrackingDataType.RightHand) childTriggerKey = KeyCode.JoystickButton15;
        }
    }
}