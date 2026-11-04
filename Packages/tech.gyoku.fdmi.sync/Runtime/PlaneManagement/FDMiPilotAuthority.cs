using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using FDMi.core;

namespace FDMi.sync
{
    // §9.2。機体のパイロット選出と、姿勢の権威 (anchor の所有権) を一致させる。
    // 選出は FDMiSeatManager がローカルに行う。ここは選出結果と所有権を接続し、
    // 「パイロットが有効か」の 1 bit (~/PilotActive) を anchor と同期する。
    // Why-not: PilotID を同期しない。int の LWW は後着で食い違い、所有者とパイロットがずれる。
    // anchor の [UdonSynced] _pilotActive は所有者だけが書けるため、同期値が競合しない。
    // このコンポーネントは anchor と同じ GameObject に置く (所有権イベントは同一 GameObject に届く)。
    public class FDMiPilotAuthority : FDMiBehaviour
    {
        [Tooltip("同一 GameObject の機体アンカー")]
        public FDMiDynamicObjectSync anchor;

        [Tooltip("席の選出を行うマネージャ")]
        public FDMiSeatManager seatManager;

        [FDMiDataPathConst("~/PilotID"), FDMiRegisterCallback(nameof(OnPilotChanged))]
        [Tooltip("選出されたパイロットの ID。ローカル派生値")]
        public FDMiInt PilotID;

        [FDMiDataPathConst("~/IsPilot"), FDMiRegisterCallback(nameof(OnPilotOrZoneChanged))]
        [Tooltip("ローカルプレイヤーがパイロットか。ローカル派生値")]
        public FDMiBool IsPilot;

        [FDMiDataPathConst("~/InZone"), FDMiRegisterCallback(nameof(OnPilotOrZoneChanged))]
        [Tooltip("ローカルプレイヤーが機体のゾーン内にいるか")]
        public FDMiBool InZone;

        [FDMiDataPathConst("~/PilotActive")]
        [Tooltip("anchor._pilotActive のローカルミラー。選出の起動に使う")]
        public FDMiBool PilotActive;

        bool _retryScheduled;
        bool _inRetry;

        void Start()
        {
            _OnAnchorSynced();
        }

        // anchor の OnDeserialization / OnOwnershipTransferred から呼ばれる。
        // 同期値のミラー、後参加者の incumbent 復元、選出の再実行を行う。
        public void _OnAnchorSynced()
        {
            if (anchor == null || PilotActive == null)
                return;

            if (PilotActive.Data != anchor._pilotActive)
                PilotActive.Data = anchor._pilotActive;

            if (anchor._pilotActive && PilotID != null && PilotID.Data == -1)
            {
                // 後参加者: 同期された active と所有者から incumbent を復元する。
                // Why-not: 常に所有者を incumbent にしない。所有者の離脱で自動移譲された
                // 非パイロットの所有者を、立ちパイロットと誤認する。
                VRCPlayerApi owner = Networking.GetOwner(anchor.gameObject);
                if (owner != null)
                    PilotID.Data = owner.playerId;
            }

            if (seatManager != null)
                seatManager.DeterminePilot();

            _Reconcile();
        }

        public void OnPilotChanged()
        {
            _Reconcile();
        }

        // パイロットがゾーンを出たら権限を失う。立ちパイロットはゾーン内にいる限り継続する。
        // 着席中はゾーン内とみなす (席は機体内にある)。
        public void OnPilotOrZoneChanged()
        {
            _CheckEligibility();
        }

        // 席の退出はデータ変化を伴わないことがある (立ちパイロットの継続) ため、
        // パイロットのクライアントだけ毎フレーム資格を確認する。非パイロットは 2 回の bool 読みで抜ける。
        void Update()
        {
            _CheckEligibility();
        }

        void _CheckEligibility()
        {
            if (anchor == null || IsPilot == null || InZone == null)
                return;
            if (!IsPilot.Data || InZone.Data)
                return;
            if (seatManager != null && seatManager._IsPilotSeated())
                return;
            _LosePilot();
        }

        void _LosePilot()
        {
            // 退出 = 権限喪失。false を先に同期してから所有権を移す (§9.2)。
            if (anchor._pilotActive)
            {
                anchor._pilotActive = false;
                anchor.RequestSerialization();
            }
            if (PilotActive != null && PilotActive.Data)
                PilotActive.Data = false;
            if (seatManager != null)
                seatManager.DeterminePilot();
        }

        // 選出結果と所有権を一致させる。所有者だけが移送を実行できるため、
        // 取得側の要求と合わせて両方向から収束させる。
        public void _Reconcile()
        {
            if (anchor == null || PilotID == null)
                return;

            bool isOwner = Networking.IsOwner(anchor.gameObject);
            int pilotId = PilotID.Data;

            if (!isOwner)
            {
                if (pilotId == Networking.LocalPlayer.playerId)
                    Networking.SetOwner(Networking.LocalPlayer, anchor.gameObject);
                return;
            }

            if (pilotId == Networking.LocalPlayer.playerId)
            {
                if (!anchor._pilotActive)
                {
                    anchor._pilotActive = true;
                    anchor.RequestSerialization();
                }
                // ローカルミラーも同期させる。次の選出が立ちパイロットを継続できる。
                if (PilotActive != null && !PilotActive.Data)
                    PilotActive.Data = true;
                return;
            }

            // 所有者がパイロットでない。active を落としてから次パイロットへ渡す。
            if (anchor._pilotActive)
            {
                anchor._pilotActive = false;
                anchor.RequestSerialization();
            }
            if (PilotActive != null && PilotActive.Data)
                PilotActive.Data = false;
            if (pilotId != -1)
            {
                VRCPlayerApi next = VRCPlayerApi.GetPlayerById(pilotId);
                if (next != null && !next.isLocal)
                    Networking.SetOwner(next, anchor.gameObject);
                _ScheduleRetry();
            }
        }

        // 移譲の取りこぼしに備えて 1 回だけ再試行する。
        void _ScheduleRetry()
        {
            if (_retryScheduled || _inRetry)
                return;
            _retryScheduled = true;
            SendCustomEventDelayedSeconds(nameof(_RetryReconcile), 1f);
        }

        public void _RetryReconcile()
        {
            _retryScheduled = false;
            _inRetry = true;
            _Reconcile();
            _inRetry = false;
        }

        // パイロットの離脱。所有権は VRChat が自動で誰かに移すため、
        // 新所有者は active を落として次候補への移譲に備える。
        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (anchor == null)
                return;
            if (Networking.IsOwner(anchor.gameObject)
                && anchor._pilotActive
                && PilotID != null
                && PilotID.Data == player.playerId)
            {
                anchor._pilotActive = false;
                anchor.RequestSerialization();
            }
            _Reconcile();
        }

        // anchor の OnOwnershipRequest から呼ばれる。選出されたパイロット以外への移譲を拒否する。
        public bool _AllowOwnership(VRCPlayerApi newOwner)
        {
            return PilotID != null && newOwner != null && newOwner.playerId == PilotID.Data;
        }
    }
}
