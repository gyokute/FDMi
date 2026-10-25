using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace FDMi.sync
{
    // §9.2。動的アンカー。絶対姿勢の積分・送信ゲート・受信サンプル・view。
    // 同期フィールドは 4 フィールド 11 要素 48 B (§4.4)。_absMm は送信スナップショットであり、
    // 積分は _accMm が持つ (§4.3)。この分離が「送った位置」と「送った stamp」を同一時点に揃える。
    // Why-not: 基底の [UdonBehaviourSyncMode] は継承されないため、派生クラスにも明示する (§5.1)。
    // ExecOrder 1000: Manager (999) が root を積分・原点更新した後に、自分の view を書く (§7.1)。
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(1000)]
    public class FDMiDynamicObjectSync : FDMiSyncAnchor
    {
        // §3.3 / §6.5 / §6.6。定数は仕様値。
        public const int MAX_EXTRAP_MS = 250;
        public const long TELEPORT_DIST_MM = 500000L;
        public const int TELEPORT_GAP_MS = 2000;
        public const int RENDER_DELAY_OFFSET_MS = 50;
        public const float KEEP_ALIVE_SEC = 2.0f;

        // §4.4 の 4 フィールド。Editor 焼きが E-01 / E-02 の初期値を持つ。
        [UdonSynced, HideInInspector] public long[] _absMm = new long[3];
        [UdonSynced, HideInInspector] public short[] _rot = new short[4];
        [UdonSynced] public Vector3 _vel;
        [UdonSynced] public int _stampMs;

        // owner のキャッシュ。更新するのは _InitLocal と OnOwnershipTransferred の 2 箇所だけ (§9.2)。
        // Why-not: Networking.IsOwner を _Tick / _ApplyView から呼ばない。毎フレームの native 呼び出しと、
        // ノードごとに owner が変わる瞬間のばらつきを避ける (§14.2)。
        public bool _isLocalOwner;
        public float sendIntervalSec = 0.25f;
        // sendIntervalSec の ms 換算と、そこから導く表示遅延 (§6.5)。どちらも _InitLocal が確定する。
        // Why-not: 送信間隔と ms 系を別々に設定しない。2 つの真実ができると片方だけ直されて
        // ゲート (§6.3 条件 4) と表示 (§6.5) が食い違う。プレイヤーは sendIntervalSec だけを 0.5f にする。
        public int _sendIntervalMs = 250;
        public int _renderDelayMs = 200;

        // _accMm: 整数部が永続状態、_fracMm が mm 未満の端数 (§4.3)。
        public long[] _accMm = new long[3];
        public float[] _fracMm = new float[3];
        public Vector3 _lastBodyPos;
        public Vector3 _localVelFef;

        public long[] _lastSentAbs = new long[3];
        public short[] _lastSentRot = new short[4];
        public bool _hasSent;
        // 送信時に立て、OnPostSerialization で必ず下ろす。真の間に再要求しない (§6.3)。
        public bool _sendPending;
        public float _nextSendTime;
        public float _lastSendTime;
        public short[] _tmpRot = new short[4];
        // 受信スロットの絶対位置の受け皿。Why-not: 平坦化した _recvAbs のスロット 1 は添字 3 から始まり、
        // MaxAbsDeltaMm (添字 0..2) に直接渡せない。スロット 0 のときは _recvAbs をそのまま使う。
        public long[] _tmpAbs = new long[3];

        public bool _hasPrev;
        public bool _hasLatest;
        public bool _snap;
        public bool _received;
        public long[] _prevAbs = new long[3];
        public long[] _latestAbs = new long[3];
        public Quaternion _prevRot = Quaternion.identity;
        public Quaternion _latestRot = Quaternion.identity;
        public int _prevStamp;
        public int _latestStamp;
        public Vector3 _latestVel;
        // 受信 2 スロットの平坦化バッファ。_recvAbs[k][i] は _recvAbs[k * 3 + i] (§6.5)。
        public long[] _recvAbs = new long[6];
        public short[] _recvRot = new short[8];
        public Vector3[] _recvVel = new Vector3[2];
        public int[] _recvStamp = new int[2];
        public int _recvCount;
        public long[] _blendFromAbs = new long[3];
        public Quaternion _blendFromRot = Quaternion.identity;
        public int _blendT;
        public bool _blending;
        public long[] _targetAbs = new long[3];
        public Quaternion _targetRot = Quaternion.identity;

        // §9.2。Editor 焼きの絶対姿勢を積分の起点にする。_Init が Start より先でも成立する (§5.4)。
        public override void _InitLocal()
        {
            _localAbs[0] = _absMm[0]; _localAbs[1] = _absMm[1]; _localAbs[2] = _absMm[2];
            _localRot = FDMiSyncMath.UnpackQuat(_rot);
            _accMm[0] = _absMm[0]; _accMm[1] = _absMm[1]; _accMm[2] = _absMm[2];
            _fracMm[0] = 0f; _fracMm[1] = 0f; _fracMm[2] = 0f;
            _isLocalOwner = Networking.IsOwner(gameObject);
            _sendIntervalMs = (int)FDMiSyncMath.RoundHalfAwayFromZero(sendIntervalSec * 1000f);
            _renderDelayMs = _sendIntervalMs - RENDER_DELAY_OFFSET_MS;
            _lastBodyPos = (body != null) ? body.position : view.position;
            _writesView = true;
        }

        // §9.2。Update 相当。積分・送受信・送信。
        // Why-not: Manager のループから呼ぶ方式にしない。アンカーごとに自分が更新する (§9.2)。
        // Why-not: suspend の判定を各型に書かない。suspend は Manager だけが知る (§8.3)。
        public void Update()
        {
            if (manager == null || manager._IsSuspended(this)) return;
            _Tick(manager._frameStampMs, manager._nowMs);
        }

        // §9.2。LateUpdate 相当。view を書く。
        public void LateUpdate()
        {
            _ApplyViewDriver();
        }

        // §9.2 の 5 手順。owner は _ComputeViewPose を呼ばない (§9.2 の注記)。
        public override void _Tick(int frameStamp, int nowMs)
        {
            if (!_isLocalOwner)
            {
                _IntegrateReceive(nowMs);
                _ComputeViewPose(nowMs);
                return;
            }

            // root は Manager 999 が FixedUpdate で積分済み。二重積分しない。
            if (manager.GetRoot() != this) _UpdateAbsolutePose();
            _TrySend(frameStamp);
        }

        // §7.2。root-local physics。Manager が物理ステップの前に呼ぶ。
        // Why-not: Update で body 差分を読まない。body を 0 に戻した後の差分を二重に拾う。
        public override void _IntegrateRootBody()
        {
            if (!_isLocalOwner || body == null) return;

            Quaternion br = body.rotation;
            Vector3 bp = body.position;

            _localRot = FDMiSyncMath.QNormalize(_localRot * br);

            // V2 と同じく、回転更新後の絶対姿勢で位置差分を写す。
            Vector3 dAbs = _localRot * (bp * 1000f);
            _fracMm[0] = _fracMm[0] + dAbs.x;
            _fracMm[1] = _fracMm[1] + dAbs.y;
            _fracMm[2] = _fracMm[2] + dAbs.z;

            long n0 = (long)_fracMm[0];
            long n1 = (long)_fracMm[1];
            long n2 = (long)_fracMm[2];
            _accMm[0] = _accMm[0] + n0; _fracMm[0] = _fracMm[0] - (float)n0;
            _accMm[1] = _accMm[1] + n1; _fracMm[1] = _fracMm[1] - (float)n1;
            _accMm[2] = _accMm[2] + n2; _fracMm[2] = _fracMm[2] - (float)n2;
            _localAbs[0] = _accMm[0]; _localAbs[1] = _accMm[1]; _localAbs[2] = _accMm[2];

            body.position = Vector3.zero;
            body.rotation = Quaternion.identity;
            body.velocity = FDMiSyncMath.QInv(br) * body.velocity;
            body.angularVelocity = FDMiSyncMath.QInv(br) * body.angularVelocity;

            _lastBodyPos = Vector3.zero;
            _localVelFef = _localRot * body.velocity;
        }

        // §8.2。root 切替時に新しい root body を origin に置く。
        public override void _SnapRootBodyToOrigin()
        {
            if (body == null) return;
            body.position = Vector3.zero;
            body.rotation = Quaternion.identity;
            _lastBodyPos = Vector3.zero;
        }

        // §7.2。owner の積分。端数は _fracMm に繰り越す。整数丸めの毎フレーム積分は禁止 (§4.3)。
        public void _UpdateAbsolutePose()
        {
            Vector3 cur = (body != null) ? body.position : view.position;
            Vector3 dView = (cur - _lastBodyPos) * 1000f;

            Quaternion originRot = manager.GetOriginRot();
            // Why-not: 回転を省かない。view 軸の差分は FEF に写してから足す。省くと root 切替で経路から外れる (M-13)。
            Vector3 dAbs = originRot * dView;

            _fracMm[0] = _fracMm[0] + dAbs.x;
            _fracMm[1] = _fracMm[1] + dAbs.y;
            _fracMm[2] = _fracMm[2] + dAbs.z;

            long n0 = (long)_fracMm[0];
            long n1 = (long)_fracMm[1];
            long n2 = (long)_fracMm[2];
            _accMm[0] = _accMm[0] + n0; _fracMm[0] = _fracMm[0] - (float)n0;
            _accMm[1] = _accMm[1] + n1; _fracMm[1] = _fracMm[1] - (float)n1;
            _accMm[2] = _accMm[2] + n2; _fracMm[2] = _fracMm[2] - (float)n2;
            _localAbs[0] = _accMm[0]; _localAbs[1] = _accMm[1]; _localAbs[2] = _accMm[2];

            _lastBodyPos = cur;
            _localRot = originRot * ((body != null) ? body.rotation : view.rotation);
            Vector3 velView = (body != null) ? body.velocity : dView * 0.001f / Time.deltaTime;
            _localVelFef = originRot * velView;
        }

        // §6.3。owner のときだけ呼ばれる (_Tick 手順 5)。owner 判定は _Tick が持つ。
        public void _TrySend(int frameStamp)
        {
            if (_sendPending) return;
            if (Networking.IsClogged) return;
            if (Time.time < _nextSendTime) return;

            // パックは送信間隔が経過したときだけ。変化判定と送信で同じ値を使う。
            FDMiSyncMath.PackQuat(_localRot, _tmpRot);

            bool moved = !_hasSent || FDMiSyncMath.MaxAbsDeltaMm(_localAbs, _lastSentAbs) >= 1L;
            bool turned = !_hasSent
                || _tmpRot[0] != _lastSentRot[0] || _tmpRot[1] != _lastSentRot[1]
                || _tmpRot[2] != _lastSentRot[2] || _tmpRot[3] != _lastSentRot[3];
            if (!moved && !turned && (Time.time - _lastSendTime) < KEEP_ALIVE_SEC) return;

            // §6.3。要素コピーであり参照代入ではない (T-PROTO-07)。
            _sendPending = true;
            _absMm[0] = _accMm[0]; _absMm[1] = _accMm[1]; _absMm[2] = _accMm[2];
            _rot[0] = _tmpRot[0]; _rot[1] = _tmpRot[1]; _rot[2] = _tmpRot[2]; _rot[3] = _tmpRot[3];
            _vel = _localVelFef;
            _stampMs = frameStamp;
            RequestSerialization();
        }

        // §6.3。失敗しても待ちは解く。送信記録は成功時だけ更新する。
        public override void OnPostSerialization(SerializationResult result)
        {
            _sendPending = false;
            if (!result.success) return;

            _lastSentAbs[0] = _absMm[0]; _lastSentAbs[1] = _absMm[1]; _lastSentAbs[2] = _absMm[2];
            _lastSentRot[0] = _rot[0]; _lastSentRot[1] = _rot[1]; _lastSentRot[2] = _rot[2]; _lastSentRot[3] = _rot[3];
            _hasSent = true;
            _lastSendTime = Time.time;
            _nextSendTime = Time.time + sendIntervalSec;
        }

        // §6.4。配列を含む全フィールドの唯一の受信経路。ここでは積むだけで、適用は次の Update。
        public override void OnDeserialization()
        {
            if (Networking.IsOwner(gameObject)) return;
            // 3 件目は捨てる。配列外へは書かない (T-PROTO-13)。
            if (_recvCount >= 2) return;

            int k = _recvCount;
            int b = k * 3;
            _recvAbs[b] = _absMm[0]; _recvAbs[b + 1] = _absMm[1]; _recvAbs[b + 2] = _absMm[2];
            int r = k * 4;
            _recvRot[r] = _rot[0]; _recvRot[r + 1] = _rot[1]; _recvRot[r + 2] = _rot[2]; _recvRot[r + 3] = _rot[3];
            _recvVel[k] = _vel;
            _recvStamp[k] = _stampMs;
            _recvCount = k + 1;
            _received = true;
        }

        // §6.5。2 スロットを到着順に消化する。同一フレームの 2 パケットでも古い方を捨てない。
        public void _IntegrateReceive(int nowMs)
        {
            if (!_received) return;
            _received = false;

            int n = _recvCount;
            _recvCount = 0;
            for (int k = 0; k < n; k++)
            {
                // _stampMs == 0 は未送信。
                if (_recvStamp[k] == 0) continue;
                if (_hasLatest && _recvStamp[k] - _latestStamp <= 0) continue;

                if (!_hasLatest)
                {
                    // §6.5。初回受信はスナップ。0 に戻さず、受信した現在値をそのまま表示する (§6.9)。
                    _CopySlotAbs(k, _latestAbs);
                    _latestRot = _UnpackSlotRot(k);
                    _latestVel = _recvVel[k];
                    _latestStamp = _recvStamp[k];
                    _hasLatest = true;
                    _snap = true;
                    continue;
                }

                _CopySlotAbs(k, _tmpAbs);
                int dt = _recvStamp[k] - _latestStamp;
                long d = FDMiSyncMath.MaxAbsDeltaMm(_tmpAbs, _latestAbs);
                bool teleport = (d > TELEPORT_DIST_MM) || (dt > TELEPORT_GAP_MS);

                _prevAbs[0] = _latestAbs[0]; _prevAbs[1] = _latestAbs[1]; _prevAbs[2] = _latestAbs[2];
                _prevRot = _latestRot;
                _prevStamp = _latestStamp;

                _latestAbs[0] = _tmpAbs[0]; _latestAbs[1] = _tmpAbs[1]; _latestAbs[2] = _tmpAbs[2];
                _latestRot = _UnpackSlotRot(k);
                _latestVel = _recvVel[k];
                _latestStamp = _recvStamp[k];
                _hasPrev = true;

                if (teleport)
                {
                    _prevAbs[0] = _latestAbs[0]; _prevAbs[1] = _latestAbs[1]; _prevAbs[2] = _latestAbs[2];
                    _prevRot = _latestRot;
                    _prevStamp = _latestStamp;
                    _snap = true;
                    continue;
                }

                _blendFromAbs[0] = _localAbs[0]; _blendFromAbs[1] = _localAbs[1]; _blendFromAbs[2] = _localAbs[2];
                _blendFromRot = _localRot;
                _blendT = nowMs;
                _blending = true;
            }
        }

        // §6.5。既定は外挿。250 ms を超えたら補間のフォールバックに移る。
        public void _ComputeViewPose(int nowMs)
        {
            if (!_hasLatest) return;

            if (_snap)
            {
                _localAbs[0] = _latestAbs[0]; _localAbs[1] = _latestAbs[1]; _localAbs[2] = _latestAbs[2];
                _localRot = _latestRot;
                _snap = false;
                _blending = false;
                return;
            }

            int ahead = nowMs - _latestStamp;
            int dt = _hasPrev ? _latestStamp - _prevStamp : 0;
            bool canExtrap = _hasPrev && dt > 0
                && (_latestVel.x != 0f || _latestVel.y != 0f || _latestVel.z != 0f);

            if (canExtrap && ahead <= MAX_EXTRAP_MS)
            {
                if (ahead < 0) ahead = 0;
                // v[m/s] × Δt[ms] = Δ[mm] なので _latestVel をそのまま mm の増分に使える (§6.5)。
                _targetAbs[0] = _latestAbs[0] + FDMiSyncMath.RoundHalfAwayFromZero(_latestVel.x * (float)ahead);
                _targetAbs[1] = _latestAbs[1] + FDMiSyncMath.RoundHalfAwayFromZero(_latestVel.y * (float)ahead);
                _targetAbs[2] = _latestAbs[2] + FDMiSyncMath.RoundHalfAwayFromZero(_latestVel.z * (float)ahead);
                _targetRot = _ExtrapolateRot(_prevRot, _latestRot, dt, ahead);
            }
            else
            {
                // §6.5。renderStamp は now から引く。latestStamp − renderDelay にすると表示がパケット周期で止まる。
                int renderStamp = nowMs - _renderDelayMs;
                if (_hasPrev
                    && renderStamp - _prevStamp >= 0
                    && _latestStamp - renderStamp >= 0)
                {
                    float u = (dt > 0) ? (float)(renderStamp - _prevStamp) / (float)dt : 1f;
                    _targetAbs[0] = _prevAbs[0] + FDMiSyncMath.RoundHalfAwayFromZero((float)(_latestAbs[0] - _prevAbs[0]) * u);
                    _targetAbs[1] = _prevAbs[1] + FDMiSyncMath.RoundHalfAwayFromZero((float)(_latestAbs[1] - _prevAbs[1]) * u);
                    _targetAbs[2] = _prevAbs[2] + FDMiSyncMath.RoundHalfAwayFromZero((float)(_latestAbs[2] - _prevAbs[2]) * u);
                    _targetRot = FDMiSyncMath.SlerpHemisphere(_prevRot, _latestRot, u);
                }
                else
                {
                    _targetAbs[0] = _latestAbs[0]; _targetAbs[1] = _latestAbs[1]; _targetAbs[2] = _latestAbs[2];
                    _targetRot = _latestRot;
                }
            }

            // 到着時は 1 送信間隔で新しい予測へ寄せる。新旧の予測は同じ速度で進むので正味の遅れは出ない。
            if (_blending)
            {
                float u = (float)(nowMs - _blendT) / (float)_sendIntervalMs;
                if (u < 0f) u = 0f; else if (u > 1f) u = 1f;
                _targetAbs[0] = _blendFromAbs[0] + FDMiSyncMath.RoundHalfAwayFromZero((float)(_targetAbs[0] - _blendFromAbs[0]) * u);
                _targetAbs[1] = _blendFromAbs[1] + FDMiSyncMath.RoundHalfAwayFromZero((float)(_targetAbs[1] - _blendFromAbs[1]) * u);
                _targetAbs[2] = _blendFromAbs[2] + FDMiSyncMath.RoundHalfAwayFromZero((float)(_targetAbs[2] - _blendFromAbs[2]) * u);
                _targetRot = FDMiSyncMath.SlerpHemisphere(_blendFromRot, _targetRot, u);
                if (u >= 1f) _blending = false;
            }

            _localAbs[0] = _targetAbs[0]; _localAbs[1] = _targetAbs[1]; _localAbs[2] = _targetAbs[2];
            _localRot = _targetRot;
        }

        // §6.5。q0 → q1 の軸角を ahead / dt 倍して q1 に掛ける。
        public Quaternion _ExtrapolateRot(Quaternion q0, Quaternion q1, int dtMs, int aheadMs)
        {
            if (dtMs <= 0 || aheadMs <= 0) return q1;

            Quaternion dq = q1 * FDMiSyncMath.QInv(q0);
            if (dq.w < 0f) dq = new Quaternion(-dq.x, -dq.y, -dq.z, -dq.w);
            float angle = 2f * Mathf.Acos(Mathf.Clamp(dq.w, -1f, 1f));
            if (angle < 1e-5f) return q1;

            Vector3 axis = new Vector3(dq.x, dq.y, dq.z);
            float len = Mathf.Sqrt(axis.x * axis.x + axis.y * axis.y + axis.z * axis.z);
            if (len < 1e-6f) return q1;
            float inv = 1f / len;
            axis = new Vector3(axis.x * inv, axis.y * inv, axis.z * inv);

            float spinDeg = angle * ((float)aheadMs / (float)dtMs) * Mathf.Rad2Deg;
            Quaternion spin = Quaternion.AngleAxis(spinDeg, axis);
            return FDMiSyncMath.QNormalize(spin * q1);
        }

        // §6.7。書き込み順は固定: position/rotation → velocity → 待ち解除 → snap。
        public override void OnOwnershipTransferred(VRCPlayerApi newOwner)
        {
            // §6.7 手順の先頭。送受信の分岐はこの 1 個だけを見る。
            _isLocalOwner = newOwner.isLocal;
            _CacheRefs();

            if (!newOwner.isLocal)
            {
                // 旧 owner の待ちを解く。body の物理状態は外部の物理制御が所有する。
                _sendPending = false;
                return;
            }

            int nowMs = Networking.GetServerTimeInMilliseconds();
            if (_hasLatest)
            {
                int age = nowMs - _latestStamp;
                if (age < 0) age = 0;
                if (age > MAX_EXTRAP_MS) age = MAX_EXTRAP_MS;
                _localAbs[0] = _latestAbs[0] + FDMiSyncMath.RoundHalfAwayFromZero(_latestVel.x * (float)age);
                _localAbs[1] = _latestAbs[1] + FDMiSyncMath.RoundHalfAwayFromZero(_latestVel.y * (float)age);
                _localAbs[2] = _latestAbs[2] + FDMiSyncMath.RoundHalfAwayFromZero(_latestVel.z * (float)age);
                _localRot = _latestRot;
            }
            else
            {
                // Why-not: 受信前の移転で _latestAbs (初期値 0) を使わない。地球中心へ飛ぶ (T-PROTO-14)。
                _localAbs[0] = _absMm[0]; _localAbs[1] = _absMm[1]; _localAbs[2] = _absMm[2];
                _localRot = FDMiSyncMath.UnpackQuat(_rot);
            }

            _accMm[0] = _localAbs[0]; _accMm[1] = _localAbs[1]; _accMm[2] = _localAbs[2];
            _fracMm[0] = 0f; _fracMm[1] = 0f; _fracMm[2] = 0f;

            if (body != null)
            {
                body.position = manager.ViewFromAbs(_localAbs);
                body.rotation = manager.ViewRotFromAbs(_localRot);
                body.velocity = FDMiSyncMath.QInv(manager.GetOriginRot()) * _latestVel;
                _lastBodyPos = body.position;
            }
            else
            {
                view.position = manager.ViewFromAbs(_localAbs);
                view.rotation = manager.ViewRotFromAbs(_localRot);
                _lastBodyPos = view.position;
            }

            _sendPending = false;
            _nextSendTime = 0f;
            _snap = true;
        }

        // §9.2。root のローカル所有 body だけは Manager が積分するので view を書かない。
        // それ以外は view を書く。物理状態は外部が所有する。
        public override void _ApplyView(bool shift)
        {
            if (!shift && _isLocalOwner && manager.GetRoot() == this) return;
            _WriteViewPose();
            if (shift) _lastBodyPos = (body != null) ? body.position : view.position;
        }

        // §9.2。動的アンカーは常に world 姿勢を書く。
        // Why-not: 親との差分 (local) で書かない。積分は body の world 差分を読むので、
        // 親の Transform を経由した world 姿勢は 1 フレーム古くなりうる (§7.2)。
        public override void _WriteViewPose()
        {
            if (body != null)
            {
                body.position = manager.ViewFromAbs(_localAbs);
                body.rotation = manager.ViewRotFromAbs(_localRot);
                return;
            }
            view.position = manager.ViewFromAbs(_localAbs);
            view.rotation = manager.ViewRotFromAbs(_localRot);
        }

        void _CopySlotAbs(int k, long[] dst)
        {
            int b = k * 3;
            dst[0] = _recvAbs[b]; dst[1] = _recvAbs[b + 1]; dst[2] = _recvAbs[b + 2];
        }

        // Why-not: 平坦化バッファの 4 要素を UnpackQuat に渡すため _tmpRot を使い回す。
        // owner は受信経路を呼ばないので _tmpRot (パック先) と重ならない。スロット専用配列を増やさない。
        Quaternion _UnpackSlotRot(int k)
        {
            int b = k * 4;
            _tmpRot[0] = _recvRot[b]; _tmpRot[1] = _recvRot[b + 1]; _tmpRot[2] = _recvRot[b + 2]; _tmpRot[3] = _recvRot[b + 3];
            return FDMiSyncMath.UnpackQuat(_tmpRot);
        }
    }
}
