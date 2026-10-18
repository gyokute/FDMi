using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace FDMi.sync
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class FDMiPlayerAnchor : FDMiBehaviour
    {
        [Tooltip("Shared world origin. Auto-found when empty.")]
        public FDMiEcefOrigin origin;
        [Tooltip("This player's sync body. Positioned under the vehicle while seated, follows the avatar otherwise.")]
        public FDMiEcefSync playerSync;
        [Tooltip("Serialize at most this often in seconds.")]
        public float updateInterval = 0.25f;
        [Tooltip("Skip serialize while the avatar moved less than this in meters.")]
        public float idleMeters = 0.02f;
        [Tooltip("Skip serialize while the avatar turned less than this in degrees.")]
        public float idleDegrees = 1f;

        VRCPlayerApi localPlayer;
        float nextSendTime;
        bool seated;

        void Start()
        {
            localPlayer = Networking.LocalPlayer;
            nextSendTime = Time.time + updateInterval;
        }
        void Update()
        {
            if (!playerSync || localPlayer == null)
                return;
            if (!Networking.IsOwner(playerSync.gameObject))
                return;
            if (seated)
                return;
            if (Time.time < nextSendTime)
                return;
            nextSendTime = Time.time + updateInterval;
            Vector3 position = localPlayer.GetPosition();
            Quaternion rotation = localPlayer.GetRotation();
            Vector3 offsetM = origin ? origin.UnityToEcefOffsetM(position) : position;
            int x = Mathf.RoundToInt(offsetM.x / playerSync.cellMeters);
            int y = Mathf.RoundToInt(offsetM.y / playerSync.cellMeters);
            int z = Mathf.RoundToInt(offsetM.z / playerSync.cellMeters);
            if (playerSync.cellX == x && playerSync.cellY == y && playerSync.cellZ == z)
                return;
            playerSync.target.position = position;
            playerSync.target.rotation = rotation;
            playerSync.CaptureAndSend(false);
        }

        public override void OnStationEntered(VRCPlayerApi player)
        {
            if (player.isLocal)
                seated = true;
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
            if (!player.isLocal)
                return;
            seated = false;
            if (playerSync)
                playerSync.CaptureAndSend(true);
        }
    }
}
