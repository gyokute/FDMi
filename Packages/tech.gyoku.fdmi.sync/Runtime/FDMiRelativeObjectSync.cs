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

        public override void initReferencePoint()
        {
            base.initReferencePoint();
            if (obejctManaer) obejctManaer.SubscribeOwnerManagement(this);
            body.useGravity = false;

            isRoot = false;
            setPosition(body.position);
            setRotation(body.rotation);
            syncedPos = _position;
            syncedRot = _rotation;
            syncedKmPos = _kmPosition;
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
            prevRootPos = rootRefPoint._position;
            prevRootRot = rootRefPoint._rotation;
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
        Vector3 prevRootPos = Vector3.zero;
        Quaternion prevRootRot = Quaternion.identity;
        void FixedUpdate()
        {
            if (!isInit) return;
            if (Networking.IsOwner(gameObject) && !body.isKinematic)
            {
                Quaternion br = body.rotation;
                Vector3 bp = body.position;
                if (isRoot)
                {
                    setRotation((_rotation * br).normalized);
                    setPosition(_rotation * bp + _position);
                    body.position = Vector3.zero;
                    body.rotation = Quaternion.identity;
                    body.velocity = Quaternion.Inverse(br) * body.velocity;

                    // gravity
                    Vector3 g = Quaternion.Inverse(_rotation) * gravity;
                    body.AddForce(g, ForceMode.Acceleration);
                }
                else
                {
                    Quaternion smr = syncManager.localRotation;
                    Vector3 smp = syncManager.localPosition;
                    Vector3 rbdp = Vector3.zero;
                    if (rootRefPoint.body)
                    {
                        smr = rootRefPoint.body.rotation * smr;
                        rbdp = rootRefPoint.body.position;
                    }

                    setRotation((Quaternion.Inverse(smr) * br).normalized);
                    setPosition(Quaternion.Inverse(smr) * (bp - smp - rbdp));
                    body.rotation = getViewRotation();
                    body.position = getViewPosition();
                    // body.velocity = Quaternion.Inverse(rbr) * body.velocity;

                    // gravity
                    Vector3 g = Quaternion.Inverse(rootRefPoint._rotation) * gravity;
                    body.AddForce(g, ForceMode.Acceleration);
                }
            }
            else
            {
                if (isRoot) return;
                body.rotation = getViewRotation();
                body.position = getViewPosition();
                body.transform.position = getViewPosition();
                body.transform.rotation = getViewRotation();
                Vector3 g = Quaternion.Inverse(rootRefPoint._rotation) * gravity;
                body.AddForce(g, ForceMode.Acceleration);
            }
        }

        public void Update()
        {
            if (!isInit || stopUpdate) return;
            if (Networking.IsOwner(gameObject) && !body.isKinematic)
            {
                Quaternion btr = body.transform.rotation;
                Vector3 btp = body.transform.position + (syncManager.localPlayerPosition ? 1000f * syncManager.localPlayerPosition._kmPosition : Vector3.zero);
                if (isRoot)
                {
                    _velocity = _rotation * body.velocity;
                }
                // if (parentRefPoint.index == rootRefPoint.index)
                // {
                //     setRotation((btr * parentRefPoint._rotation).normalized);
                //     setPosition(parentRefPoint._rotation * btp + parentRefPoint._position + 1000f * (parentRefPoint.syncedKmPos - syncedKmPos));
                //     _velocity = parentRefPoint._rotation * body.velocity;
                // }
                // Serialize
                ownerSyncedTime = ServerTimeDiff + Time.time;
                syncedVel = _velocity;
                TrySerialize();
            }
            if (!Networking.IsOwner(gameObject))
            {
                ExtrapolationAndSmoothing();
                if (Networking.IsOwner(body.gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
            transform.position = getViewPosition();
            transform.rotation = getViewRotation();
        }

        #region Interpolated
        [Tooltip("How quickly to lerp rotation to new extrapolated target rotation, it might help to reduce this in high-lag situations with planes that can roll quickly")]
        public float RotationSyncAgressiveness = 10f;
        [Tooltip("Multiply velocity vectors recieved while in idle mode, useful for stopping sea vehicles from extrapolating above and below the water")]
        public float IdleModeVelMultiplier = .4f;
        [Tooltip("Maximum amount of extrapolation for high ping players, will brake formation flying for high ping players if set lower than their ping = 0.1 = 100ms, useful for dogfight worlds")]
        public float MaxPingExtrapolationInSeconds = 0.1f;
        private bool deserialized = false;
        private double localSyncedTime;
        private float SmoothingTimeDivider;
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
            syncedRot = O_LastRotation = RotExtrapolation_Raw = _rotation;
            syncedKmPos = lastSyncedKmPos = _kmPosition;
            O_LastUpdateTime = L_UpdateTime = lastframetime_extrap = Networking.GetServerTimeInMilliseconds();
            O_LastUpdateTime -= updateInterval;
            SmoothingTimeDivider = 1f / updateInterval;

        }

        double lastframetime_extrap;
        float ErrorLastFrame;
        private Vector3 ExtrapDirection_Smooth;
        private Quaternion RotExtrapDirection_Smooth, RotationL;
        private void ExtrapolationAndSmoothing()
        {
            if (float.IsNaN(_rotation.x)) _rotation = syncedRot;
            if (deserialized)
            {
                deserialized = false;
                whenSynced();
            }
            float deltatime = Time.deltaTime;
            double time;
            if (deltatime > .099f)
            {
                time = Networking.GetServerTimeInSeconds();
                deltatime = (float)(time - lastframetime_extrap);
                ResetSyncTime();
            }
            else { time = ServerTimeDiff + Time.time; }
            lastframetime_extrap = Networking.GetServerTimeInSeconds();
            float TimeSinceUpdate = (float)(time - L_UpdateTime);

            Vector3 PredictedPosition = L_PingAdjustedPosition + (ExtrapolationDirection * TimeSinceUpdate);
            Quaternion PredictedRotation = (Quaternion.SlerpUnclamped(Quaternion.identity, CurAngMom, Ping + TimeSinceUpdate) * syncedRot);

            if (TimeSinceUpdate < updateInterval)
            {
                float TimeSincePrevUpdate = (float)(time - lastUpdateTime);
                Vector3 OldPredictedPosition = L_LastPingAdjustedPosition + (LastExtrapolationDirection * TimeSincePrevUpdate);
                Quaternion OldPredictedRotation = (Quaternion.SlerpUnclamped(Quaternion.identity, LastCurAngMom, LastPing + TimeSincePrevUpdate) * O_LastRotation2);

                _rotation = Quaternion.Slerp(_rotation,
                  Quaternion.Slerp(OldPredictedRotation, PredictedRotation, TimeSinceUpdate * SmoothingTimeDivider), Time.smoothDeltaTime * RotationSyncAgressiveness);

                _position = Vector3.Lerp(OldPredictedPosition, PredictedPosition, (float)TimeSinceUpdate * SmoothingTimeDivider);
            }
            else
            {
                _rotation = Quaternion.Slerp(_rotation, PredictedRotation, Time.smoothDeltaTime * SmoothingTimeDivider);
                _position = PredictedPosition;
            }
        }

        private double O_LastUpdateTime, lastUpdateTime, L_UpdateTime;
        private Vector3 Acceleration, LastAcceleration;
        private float Ping, LastPing;
        private Quaternion CurAngMom, LastCurAngMom, CurAngMomAcceleration;

        private Vector3 ExtrapolationDirection, LastExtrapolationDirection;
        private Quaternion RotationExtrapolationDirection;
        private Vector3 L_PingAdjustedPosition = Vector3.zero, L_LastPingAdjustedPosition = Vector3.zero;
        private Quaternion L_PingAdjustedRotation = Quaternion.identity;
        private Vector3 lerpedCurVel;
        private Vector3 lastSyncedPos, LastSyncedVelocity, lastVelocity, lastSyncedKmPos;
        private Quaternion O_LastRotation, O_LastRotation2;
        private Vector3 Extrapolation_Raw;
        private Quaternion RotExtrapolation_Raw = Quaternion.identity;
        private void whenSynced()
        {
            LastAcceleration = Acceleration;
            LastPing = Ping;
            lastUpdateTime = L_UpdateTime;
            LastCurAngMom = CurAngMom;

            L_UpdateTime = ServerTimeDiff + Time.time;
            Ping = Mathf.Min((float)(L_UpdateTime - O_UpdateTime), MaxPingExtrapolationInSeconds);

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
            Quaternion PlaneRotDif = syncedRot * Quaternion.Inverse(O_LastRotation);
            Acceleration = (PlaneRotDif * Acceleration) * .5f;//not sure why it's 0.5, but it seems correct from testing
            Acceleration += Acceleration * (Ping / updateInterval);

            //current angular momentum as a quaternion
            CurAngMom = Quaternion.SlerpUnclamped(Quaternion.identity, PlaneRotDif, speednormalizer);
            CurAngMomAcceleration = CurAngMom * Quaternion.Inverse(LastCurAngMom);

            //if direction of acceleration changed by more than 90 degrees, just set zero to prevent bounce effect, the vehicle likely just crashed into a wall.
            //+ if idlemode, disable acceleration because it brakes
            if (Vector3.Dot(Acceleration, LastAcceleration) < 0 || SetVelZero || syncedVel.magnitude < IdleMovementRange)
            { Acceleration = Vector3.zero; CurAngMomAcceleration = Quaternion.identity; }

            RotationExtrapolationDirection = CurAngMomAcceleration * CurAngMom;
            Quaternion PingRotExtrap = Quaternion.SlerpUnclamped(Quaternion.identity, RotationExtrapolationDirection, Ping);
            L_PingAdjustedRotation = PingRotExtrap * syncedRot;
            Quaternion FrameRotExtrap = Quaternion.SlerpUnclamped(Quaternion.identity, RotationExtrapolationDirection, -Time.deltaTime);
            RotExtrapolation_Raw = FrameRotExtrap * L_PingAdjustedRotation;//undo 1 frame worth of movement because its done again in update()

            LastExtrapolationDirection = ExtrapolationDirection;
            ExtrapolationDirection = _velocity + Acceleration;
            L_LastPingAdjustedPosition = L_PingAdjustedPosition;
            L_PingAdjustedPosition = syncedPos + (ExtrapolationDirection * Ping);

            Extrapolation_Raw = L_PingAdjustedPosition - (ExtrapolationDirection * Time.deltaTime);//undo 1 frame worth of movement because its done again in update()
            L_LastPingAdjustedPosition -= syncedKmDiff;
            _position -= syncedKmDiff;

            O_LastUpdateTime = O_UpdateTime;
            O_LastRotation2 = O_LastRotation;
            O_LastRotation = syncedRot;
            lastSyncedPos = syncedPos;
            lastVelocity = _velocity;
            lastSyncedKmPos = syncedKmPos;
        }

        #endregion
    }
}