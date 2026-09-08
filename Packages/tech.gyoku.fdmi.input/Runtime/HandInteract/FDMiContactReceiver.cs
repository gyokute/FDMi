using FDMi.core;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDKBase;

namespace FDMi.input
{
    [UnityEngine.RequireComponent(typeof(VRCContactReceiver))]
    public class FDMiContactReceiver : FDMiBehaviour
    {
        public FDMiHandInputGroup inputGroup;
        public FDMiHandInput leftHandInput;
        public FDMiHandInput rightHandInput;

        public string leftContactTag = "FingerIndexL";
        public string rightContactTag = "FingerIndexR";

        private void OnDisable()
        {
            if (leftHandInput)
                leftHandInput.ExitContact(inputGroup);

            if (rightHandInput && rightHandInput != leftHandInput)
                rightHandInput.ExitContact(inputGroup);
        }

        public override void OnContactEnter(ContactEnterInfo contactInfo)
        {
            if (!inputGroup || !inputGroup.IsActive())
                return;
            if (!contactInfo.contactSender.isValid)
                return;
            if (contactInfo.contactSender.player != Networking.LocalPlayer)
                return;
            if (contactInfo.matchingTags == null || contactInfo.matchingTags.Length == 0)
                return;

            if (contactInfo.matchingTags[0] == leftContactTag)
            {
                if (leftHandInput)
                    leftHandInput.EnterContact(inputGroup);
                return;
            }

            if (contactInfo.matchingTags[0] == rightContactTag && rightHandInput)
                rightHandInput.EnterContact(inputGroup);
        }

        public override void OnContactExit(ContactExitInfo contactInfo)
        {
            if (!inputGroup)
                return;
            if (!contactInfo.contactSender.isValid)
                return;
            if (contactInfo.contactSender.player != Networking.LocalPlayer)
                return;
            if (contactInfo.matchingTags == null || contactInfo.matchingTags.Length == 0)
                return;

            if (contactInfo.matchingTags[0] == leftContactTag)
            {
                if (leftHandInput)
                    leftHandInput.ExitContact(inputGroup);
                return;
            }

            if (contactInfo.matchingTags[0] == rightContactTag && rightHandInput)
                rightHandInput.ExitContact(inputGroup);
        }
    }
}
