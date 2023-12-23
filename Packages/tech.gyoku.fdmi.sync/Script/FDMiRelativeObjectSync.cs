using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.sync
{
    public class FDMiRelativeObjectSync : FDMiReferencePoint
    {
        public Vector3 gravity = new Vector3(0, -9.8f, 0);
        public FDMiObjectManager obejctManaer;

        void Start()
        {
            if (!body)
            {
                gameObject.SetActive(false);
                return;
            }
            if (obejctManaer) obejctManaer.SubscribeOwnerManagement(this);
            body.useGravity = false;

            setPosition(body.position);
            setRotation(body.rotation);
            ResetSyncStuff();
        }
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (player.isLocal)
            {
                ResetSyncTime();
                whenSynced();
            }
        }
        public override void windupPositionAndRotation()
        {
            if (!Networking.IsOwner(gameObject)) ExtrapolationAndSmoothing();
            transform.position = getViewPosition();
            transform.rotation = getViewRotation();
            if (body)
            {
                body.position = transform.position;
                body.rotation = transform.rotation;
                body.transform.position = transform.position;
                body.transform.rotation = transform.rotation;
            }
        }

        private Vector3 tentativeVel;
        public void SetLocalPlayerAsOwner()
        {
            tentativeVel = _velocity;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            SendCustomEventDelayedFrames(nameof(RevertTentativeVelocity), 3);
        }
        public void RevertTentativeVelocity()
        {
            _velocity = tentativeVel;
            body.velocity = Quaternion.Inverse(_rotation) * _velocity;
        }
        Vector3 tPos = Vector3.zero, tVel = Vector3.zero, tAngVel = Vector3.zero;
        Quaternion tRot = Quaternion.identity;
        void FixedUpdate()
        {
            if (!isInit) return;
            if (!Networking.IsOwner(gameObject) || body.isKinematic) return;
            if (isRoot)
            {
                Quaternion br = body.rotation;
                Vector3 bp = body.position;
                setRotation((_rotation * br).normalized);
                setPosition(_rotation * bp + _position);
                body.position = Vector3.zero;
                body.rotation = Quaternion.identity;
                body.velocity = Quaternion.Inverse(br) * body.velocity;

                // gravity
                Vector3 g = Quaternion.Inverse(_rotation) * gravity;
                body.AddForce(g, ForceMode.Acceleration);
            }
            if (parentRefPoint.index == rootRefPoint.index)
            {
                // gravity
                Vector3 g = Quaternion.Inverse(parentRefPoint._rotation) * gravity;
                body.AddForce(g, ForceMode.Acceleration);
            }
        }

        public void Update()
        {
            if (!isInit || stopUpdate) return;
            if (Networking.IsOwner(gameObject) && !body.isKinematic)
            {
                Quaternion btr = body.transform.rotation;
                Vector3 btp = body.transform.position;
                if (isRoot)
                {
                    // setRotation((btr * _rotation).normalized);
                    // setPosition(_rotation * btp + Position);
                    _velocity = _rotation * body.velocity;
                }
                if (parentRefPoint.index == rootRefPoint.index)
                {
                    setRotation((btr * parentRefPoint._rotation).normalized);
                    setPosition(parentRefPoint._rotation * btp + parentRefPoint._position + 1000f * (parentRefPoint.syncedKmPos - syncedKmPos));
                    _velocity = parentRefPoint._rotation * body.velocity;
                }
                // Serialize
                ownerSyncedTime = ServerTimeDiff + Time.time;
                syncedVel = _velocity;
                TrySerialize();
            }
        }
        public void LateUpdate()
        {
            if (!Networking.IsOwner(gameObject)) ExtrapolationAndSmoothing();
            if (stopUpdate || isRoot) return;
            transform.rotation = getViewRotation();
            transform.position = getViewPosition();
            body.transform.position = transform.position;
            body.transform.rotation = transform.rotation;
        }

        #region Interpolated
        [Tooltip("If vehicle moves less than this distance since it's last update, it'll be considered to be idle, may need to be increased for vehicles that want to be idle on water. If the vehicle floats away sometimes, this value is probably too big")]
        public float IdleMovementRange = .35f;
        [Tooltip("If vehicle rotates less than this many degrees since it's last update, it'll be considered to be idle")]
        public float IdleRotationRange = 5f;
        [Tooltip("Angle Difference between movement direction and rigidbody velocity that will cause the vehicle to teleport instead of interpolate")]
        public float TeleportAngleDifference = 20;
        [Tooltip("How much vehicle accelerates extra towards its 'raw' position when not owner in order to correct positional errors")]
        public float CorrectionTime = 8f;
        [Tooltip("How quickly non-owned vehicle's velocity vector lerps towards its new value")]
        public float SpeedLerpTime = 4f;
        [Tooltip("Strength of force to stop correction overshooting target")]
        public float CorrectionDStrength = 1.666666f;
        [Tooltip("How much vehicle accelerates extra towards its 'raw' rotation when not owner in order to correct rotational errors")]
        public float CorrectionTime_Rotation = 1f;
        [Tooltip("How quickly non-owned vehicle's rotation slerps towards its new value")]
        public float RotationSpeedLerpTime = 10f;


        private bool deserialized = false;
        private double localSyncedTime;
        [UdonSynced, FieldChangeCallback(nameof(O_UpdateTime))] public double ownerSyncedTime;
        public double O_UpdateTime
        {
            get => ownerSyncedTime;
            set
            {
                if (ownerSyncedTime != value)
                {
                    ownerSyncedTime = value;
                    localSyncedTime = ServerTimeDiff + Time.time;
                    deserialized = true;
                }

            }
        }

        [UdonSynced] public Vector3 syncedVel;
        private void ResetSyncStuff()
        {
            syncedPos = lastSyncedPos = Extrapolation_Raw = L_PingAdjustedPosition = _position;
            syncedRot = lastSyncedRot = RotExtrapolation_Raw = _rotation;
            syncedKmPos = lastSyncedKmPos = _kmPosition;
            O_LastUpdateTime = L_UpdateTime = lastframetime_extrap = Networking.GetServerTimeInMilliseconds();
            O_LastUpdateTime -= updateInterval;

        }

        double lastframetime_extrap;
        float ErrorLastFrame;
        private Vector3 ExtrapDirection_Smooth;
        private Quaternion RotExtrapDirection_Smooth;
        private void ExtrapolationAndSmoothing()
        {
            if (deserialized)
            {
                deserialized = false;
                whenSynced();
            }
            float deltatime = Time.deltaTime;
            double time;
            Vector3 Deriv = Vector3.zero;
            Vector3 Correction = (Extrapolation_Raw - _position) * CorrectionTime;
            float Error = Vector3.Distance(_position, Extrapolation_Raw);
            if (deltatime > .099f)
            {
                time = Networking.GetServerTimeInSeconds();
                deltatime = (float)(time - lastframetime_extrap);
                ResetSyncTime();
            }
            else { time = ServerTimeDiff + Time.time; }
            //like a PID derivative. Makes movement a bit jerky because the 'raw' target is jerky.
            if (Error < ErrorLastFrame)
            {
                Deriv = -Correction.normalized * (ErrorLastFrame - Error) * CorrectionDStrength / deltatime;
            }
            ErrorLastFrame = Error;
            lastframetime_extrap = Networking.GetServerTimeInSeconds();
            float TimeSinceUpdate = (float)(time - L_UpdateTime) / updateInterval;
            //extrapolated position based on time passed since update
            Vector3 VelEstimate = _velocity + (Acceleration * TimeSinceUpdate);
            ExtrapDirection_Smooth = Vector3.Lerp(ExtrapDirection_Smooth, VelEstimate + Correction + Deriv, SpeedLerpTime * deltatime);

            //rotate using similar method to movement (no deriv, correction is done with a simple slerp after)
            Quaternion FrameRotAccel = Quaternion.Slerp(Quaternion.identity, CurAngMomAcceleration, TimeSinceUpdate);
            Quaternion AngMomEstimate = FrameRotAccel * CurAngMom;
            RotExtrapDirection_Smooth = Quaternion.Slerp(RotExtrapDirection_Smooth, AngMomEstimate, RotationSpeedLerpTime * deltatime);

            //apply positional update
            Extrapolation_Raw += ExtrapolationDirection * deltatime;
            _position += ExtrapDirection_Smooth * deltatime;
            //apply rotational update
            Quaternion FrameRotExtrap = Quaternion.Slerp(Quaternion.identity, RotationExtrapolationDirection, deltatime);
            RotExtrapolation_Raw = FrameRotExtrap * RotExtrapolation_Raw;
            Quaternion FrameRotExtrap_Smooth = Quaternion.Slerp(Quaternion.identity, RotExtrapDirection_Smooth, deltatime);
            _rotation = FrameRotExtrap_Smooth * _rotation;
            //correct rotational desync
            _rotation = Quaternion.Slerp(_rotation, RotExtrapolation_Raw, CorrectionTime_Rotation * deltatime);

        }

        private double O_LastUpdateTime, lastUpdateTime, L_UpdateTime;
        private Vector3 Acceleration, LastAcceleration;
        private float Ping, LastPing;
        private Quaternion CurAngMom, LastCurAngMom, CurAngMomAcceleration;

        private Vector3 ExtrapolationDirection;
        private Quaternion RotationExtrapolationDirection;
        private Vector3 L_PingAdjustedPosition = Vector3.zero;
        private Quaternion L_PingAdjustedRotation = Quaternion.identity;
        private Vector3 lerpedCurVel;
        private Vector3 lastSyncedPos, LastSyncedVelocity, lastVelocity, lastSyncedKmPos;
        private Quaternion lastSyncedRot;
        private Vector3 Extrapolation_Raw;
        private Quaternion RotExtrapolation_Raw = Quaternion.identity;
        private void whenSynced()
        {
            LastAcceleration = Acceleration;
            LastPing = Ping;
            lastUpdateTime = L_UpdateTime;
            LastCurAngMom = CurAngMom;

            L_UpdateTime = ServerTimeDiff + Time.time;
            Ping = (float)(L_UpdateTime - O_UpdateTime);

            _kmPosition = syncedKmPos;
            Vector3 syncedKmDiff = 1000f * (syncedKmPos - lastSyncedKmPos);

            //time between this update and last
            float updatedelta = (float)(O_UpdateTime - O_LastUpdateTime);
            float speednormalizer = 1 / updatedelta;


            bool SetVelZero = false;
            if (syncedVel.sqrMagnitude == 0)
            {
                if (LastSyncedVelocity.sqrMagnitude != 0)
                { _velocity = Vector3.zero; SetVelZero = true; }
                else
                { _velocity = (syncedKmDiff + syncedPos - lastSyncedPos) * speednormalizer; }
            }
            else
            { _velocity = syncedVel; }
            LastSyncedVelocity = syncedVel;
            Acceleration = (_velocity - lastVelocity);//acceleration is difference in velocity

            //rotate Acceleration by the difference in rotation of vehicle between last and this update to make it match the angle for the next update better
            Quaternion PlaneRotDif = syncedRot * Quaternion.Inverse(lastSyncedRot);
            Acceleration = (PlaneRotDif * Acceleration) * .5f;//not sure why it's 0.5, but it seems correct from testing
            Acceleration += Acceleration * (Ping / updateInterval);

            //current angular momentum as a quaternion
            CurAngMom = Quaternion.Slerp(Quaternion.identity, PlaneRotDif, speednormalizer);
            CurAngMomAcceleration = CurAngMom * Quaternion.Inverse(LastCurAngMom);

            //if direction of acceleration changed by more than 90 degrees, just set zero to prevent bounce effect, the vehicle likely just crashed into a wall.
            //+ if idlemode, disable acceleration because it brakes
            if (Vector3.Dot(Acceleration, LastAcceleration) < 0 || SetVelZero || syncedVel.magnitude < IdleMovementRange)
            { Acceleration = Vector3.zero; CurAngMomAcceleration = Quaternion.identity; }

            RotationExtrapolationDirection = CurAngMomAcceleration * CurAngMom;
            Quaternion PingRotExtrap = Quaternion.Slerp(Quaternion.identity, RotationExtrapolationDirection, Ping);
            L_PingAdjustedRotation = PingRotExtrap * syncedRot;
            Quaternion FrameRotExtrap = Quaternion.Slerp(Quaternion.identity, RotationExtrapolationDirection, -Time.deltaTime);
            RotExtrapolation_Raw = FrameRotExtrap * L_PingAdjustedRotation;//undo 1 frame worth of movement because its done again in update()

            ExtrapolationDirection = _velocity + Acceleration;
            L_PingAdjustedPosition = syncedPos + (ExtrapolationDirection * Ping);

            Extrapolation_Raw = L_PingAdjustedPosition - (ExtrapolationDirection * Time.deltaTime);//undo 1 frame worth of movement because its done again in update()
            _position -= syncedKmDiff;
            //if we're going one way but moved the other, we must have teleported.
            //set values to the same thing for Current and Last to make teleportation instead of interpolation
            // if (Vector3.Angle(syncedPos - lastSyncedPos, syncedVel) > TeleportAngleDifference && _velocity.magnitude > 30f)
            // {
            //     LastCurAngMom = CurAngMom;
            //     _position = Extrapolation_Raw;
            // }

            O_LastUpdateTime = O_UpdateTime;
            lastSyncedRot = syncedRot;
            lastSyncedPos = syncedPos;
            lastVelocity = _velocity;
            lastSyncedKmPos = syncedKmPos;
        }
        #endregion
    }
}