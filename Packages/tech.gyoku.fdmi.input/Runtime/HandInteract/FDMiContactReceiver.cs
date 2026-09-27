using FDMi.core;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDKBase;

namespace FDMi.input
{
    [UnityEngine.RequireComponent(typeof(VRCContactReceiver))]
    public class FDMiContactReceiver : FDMiHandInputGroup
    {
        public override void OnContactEnter(ContactEnterInfo contactInfo)
        {
            foreach (var t in Trackers)
            {
                t.SetContactInput(contactInfo.matchingTags[0], this);
            }
        }

        public override void OnContactExit(ContactExitInfo contactInfo)
        {
            foreach (var t in Trackers)
            {
                t.UnsetContactInput(this);
            }
        }
    }
}
