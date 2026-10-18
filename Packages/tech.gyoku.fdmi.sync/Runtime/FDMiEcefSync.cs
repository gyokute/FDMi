using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace FDMi.sync
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiEcefSync : FDMiBehaviour
    {
        [Tooltip("Observes this transform (Rigidbody root for vehicles, stationEnterPlayerLocation for seated players).")]
        public Transform target;
        [Tooltip("Shared world origin. Auto-found when empty.")]
        public FDMiEcefOrigin origin;
        [Tooltip("Meters per synced cell step. cm precision: 0.01.")]
        public float cellMeters = 0.01f;
        [Tooltip("Serialize at most this often in seconds.")]
        public float updateInterval = 0.1f;
        [Tooltip("Skip serialize while every int lane moved less than this.")]
        public int idleCellEpsilon = 1;
        [Tooltip("Freeze Rigidbody interpolation for this many frames after a re-anchor.")]
        public int freezeFrames = 2;

        [UdonSynced, HideInInspector] public int cellX;
        [UdonSynced, HideInInspector] public int cellY;
        [UdonSynced, HideInInspector] public int cellZ;
        [UdonSynced, HideInInspector] public short quatX;
        [UdonSynced, HideInInspector] public short quatY;
        [UdonSynced, HideInInspector] public short quatZ;
        [UdonSynced, HideInInspector] public short quatW;
        [UdonSynced, HideInInspector] public short velX;
        [UdonSynced, HideInInspector] public short velY;
        [UdonSynced, HideInInspector] public short velZ;
        [UdonSynced, HideInInspector] public int revision;

        int lastSentX = int.MaxValue;
        int lastSentY = int.MaxValue;
        int lastSentZ = int.MaxValue;
        float nextSendTime;
        double serverTimeDiff;
        double lastRemoteRevisionTime;
        int lastAppliedRevision = int.MinValue;
        int freezeLeft;
        Rigidbody frozenBody;
        RigidbodyInterpolation frozenInterpolation;
        Vector3 smoothPosition;
        Quaternion smoothRotation = Quaternion.identity;
        bool hasSmoothPose;

        void Start()
        {
            if (!target)
                target = transform;
            serverTimeDiff = Networking.GetServerTimeInSeconds() - (double)Time.time;
            nextSendTime = Time.time + updateInterval;
            if (Networking.IsOwner(gameObject))
                CaptureAndSend(true);
            else
                ApplyReceived();
        }
        void Update()
        {
            if (Networking.IsOwner(gameObject))
            {
                if (Time.time >= nextSendTime)
                {
                    nextSendTime = Time.time + updateInterval;
                    CaptureAndSend(false);
                }
                return;
            }
            if (freezeLeft > 0)
            {
                freezeLeft--;
                if (freezeLeft == 0)
                    RestoreInterpolation();
            }
            if (!hasSmoothPose)
                return;
            float blend = freezeLeft > 0 ? 1f : Mathf.Clamp01(Time.deltaTime * 10f);
            target.position = Vector3.Lerp(target.position, smoothPosition, blend);
            target.rotation = Quaternion.Slerp(target.rotation, smoothRotation, blend);
        }

        public void CaptureAndSend(bool force)
        {
            if (!target || !origin)
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            Vector3 offsetM = origin.UnityToEcefOffsetM(target.position);
            int x = Mathf.RoundToInt(offsetM.x / cellMeters);
            int y = Mathf.RoundToInt(offsetM.y / cellMeters);
            int z = Mathf.RoundToInt(offsetM.z / cellMeters);
            if (!force && lastSentX != int.MaxValue
                && Mathf.Abs(x - lastSentX) <= idleCellEpsilon
                && Mathf.Abs(y - lastSentY) <= idleCellEpsilon
                && Mathf.Abs(z - lastSentZ) <= idleCellEpsilon)
                return;
            cellX = x;
            cellY = y;
            cellZ = z;
            PackQuaternion(target.rotation);
            PackVelocity();
            revision++;
            lastSentX = x;
            lastSentY = y;
            lastSentZ = z;
            if (!Networking.IsClogged)
                RequestSerialization();
        }

        public override void OnDeserialization()
        {
            lastRemoteRevisionTime = serverTimeDiff + Time.time;
            ApplyReceived();
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if (player.isLocal)
                CaptureAndSend(true);
        }

        public void TeleportEcef(Vector3 offsetM, Quaternion worldRotation)
        {
            if (!origin)
                return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            Vector3 unity = origin.EcefOffsetToUnityM(offsetM);
            BeginFreeze();
            target.position = unity;
            target.rotation = worldRotation;
            CaptureAndSend(true);
        }

        void ApplyReceived()
        {
            if (!target || !origin)
                return;
            if (revision == lastAppliedRevision && hasSmoothPose)
                return;
            lastAppliedRevision = revision;
            Vector3 offsetM = new Vector3(cellX * cellMeters, cellY * cellMeters, cellZ * cellMeters);
            smoothPosition = origin.EcefOffsetToUnityM(offsetM);
            smoothRotation = UnpackQuaternion();
            Vector3 velocity = UnpackVelocity();
            if (!hasSmoothPose)
            {
                target.position = smoothPosition;
                target.rotation = smoothRotation;
                hasSmoothPose = true;
                return;
            }
            float dt = (float)(serverTimeDiff + Time.time - lastRemoteRevisionTime);
            if (dt > 0f && dt < 1f)
                smoothPosition += velocity * dt;
            BeginFreeze();
        }

        void PackQuaternion(Quaternion q)
        {
            quatX = (short)Mathf.RoundToInt(q.x * 32767f);
            quatY = (short)Mathf.RoundToInt(q.y * 32767f);
            quatZ = (short)Mathf.RoundToInt(q.z * 32767f);
            quatW = (short)Mathf.RoundToInt(q.w * 32767f);
        }

        Quaternion UnpackQuaternion()
        {
            Quaternion q = new Quaternion(quatX / 32767f, quatY / 32767f, quatZ / 32767f, quatW / 32767f);
            float mag = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            if (mag < 1e-6f)
                return Quaternion.identity;
            q.x /= mag;
            q.y /= mag;
            q.z /= mag;
            q.w /= mag;
            return q;
        }

        void PackVelocity()
        {
            Rigidbody body = target.GetComponent<Rigidbody>();
            Vector3 v = body ? body.velocity : Vector3.zero;
            velX = (short)Mathf.Clamp(Mathf.RoundToInt(v.x * 100f), -32768, 32767);
            velY = (short)Mathf.Clamp(Mathf.RoundToInt(v.y * 100f), -32768, 32767);
            velZ = (short)Mathf.Clamp(Mathf.RoundToInt(v.z * 100f), -32768, 32767);
        }

        Vector3 UnpackVelocity()
        {
            return new Vector3(velX / 100f, velY / 100f, velZ / 100f);
        }

        void BeginFreeze()
        {
            if (freezeFrames <= 0)
                return;
            Rigidbody body = target.GetComponent<Rigidbody>();
            if (!body)
                return;
            if (freezeLeft == 0)
            {
                frozenBody = body;
                frozenInterpolation = body.interpolation;
            }
            body.interpolation = RigidbodyInterpolation.None;
            freezeLeft = freezeFrames;
        }

        void RestoreInterpolation()
        {
            if (frozenBody)
                frozenBody.interpolation = frozenInterpolation;
            frozenBody = null;
        }
    }
}
