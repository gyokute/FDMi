using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace FDMi.sync
{
    [RequireComponent(typeof(VRCStation))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class FDMiSeatAnchor : FDMiBehaviour
    {
        [Tooltip("Sync body owned by the seated pilot. Same GameObject as the FDMi or Sacc vehicle root.")]
        public FDMiEcefSync vehicleSync;
        [Tooltip("Where the seated avatar is drawn. Defaults to stationEnterPlayerLocation.")]
        public Transform seatView;
        [Tooltip("While seated, copy this heading each frame. Empty = keep station yaw.")]
        public Transform headingSource;
        [Tooltip("Offset from the seat in vehicle-local meters (x=right, y=up, z=forward).")]
        public Vector3 seatOffsetM = new Vector3(0f, 0.55f, 0f);

        VRCStation station;
        bool seated;
        Transform vehicleRoot;

        void Start()
        {
            station = GetComponent<VRCStation>();
            if (!seatView)
                seatView = station.stationEnterPlayerLocation;
            if (vehicleSync)
                vehicleRoot = vehicleSync.target ? vehicleSync.target : vehicleSync.transform;
        }

        public override void Interact()
        {
            if (!vehicleSync || !vehicleRoot)
                return;
            Networking.SetOwner(Networking.LocalPlayer, vehicleSync.gameObject);
            vehicleSync.CaptureAndSend(true);
            Networking.LocalPlayer.UseAttachedStation();
        }

        public override void OnStationEntered(VRCPlayerApi player)
        {
            if (!player.isLocal)
                return;
            seated = true;
            if (vehicleSync && Networking.IsOwner(vehicleSync.gameObject))
                vehicleSync.CaptureAndSend(true);
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
            if (!player.isLocal)
                return;
            seated = false;
        }

        void Update()
        {
            if (!seated || !vehicleRoot || !seatView)
                return;
            Vector3 anchor = vehicleRoot.TransformPoint(seatOffsetM);
            Quaternion heading = headingSource ? headingSource.rotation : vehicleRoot.rotation;
            seatView.position = anchor;
            seatView.rotation = heading;
        }
    }
}
