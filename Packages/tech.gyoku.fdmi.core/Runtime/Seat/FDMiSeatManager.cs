using UnityEngine;
using VRC.SDKBase;

namespace FDMi.core
{
    // FDMiSeat と組み合わせ、Seat の占有からパイロットを一意に決定する。
    // 選出は全クライアントが同じ入力 (席の占有と pilotPriority) からローカルに行う。
    // ネットワーク上の真実は車両アンカーの所有権と ~/PilotActive だけで、これらは同期しない。
    // FDMiSeat の OnStationEntered/Exited はリモートプレイヤーでも全クライアントに届くため、
    // 席の占有は全クライアントで同一になる (VRChat VRC Station の仕様)。
    public class FDMiSeatManager : FDMiBehaviour
    {
        [Header("FDMiSeatと組み合わせ、Seatに座ったプレイヤーから、パイロットを一意に決定します。")]
        [Tooltip("対象の座席。Editor が名前空間内から自動収集する")]
        public FDMiSeat[] allSeats;

        [FDMiDataPathConst("~/IsPilot")]
        [Tooltip("ローカルプレイヤーがパイロットか。同期しないローカルフラグ")]
        public FDMiBool IsPilot;

        [FDMiDataPathConst("~/PilotID")]
        [Tooltip("パイロットのプレイヤー ID。不在は -1。ローカル選出の結果")]
        public FDMiInt PilotID;

        // 車両アンカーの同期 bit のローカルミラー。パイロットが有効な間 true。
        // ゾーン退出や所有者交代は FDMiPilotAuthority がこの値を落とし、再選出が走る。
        [FDMiDataPathConst("~/PilotActive"), FDMiRegisterCallback(nameof(DeterminePilot))]
        [Tooltip("パイロットが有効か。アンカーの同期値のローカルミラー")]
        public FDMiBool PilotActive;

        void Start()
        {
            DeterminePilot();
        }

        // パイロットを一意に決定する。全クライアントが同じ結果を得る。
        // - PilotActive が立っている間は incumbent (PilotID) を維持する。
        // - incumbent が着席中なら、より高い pilotPriority の候補だけが交代できる。
        // - incumbent が席を立っているなら、ゾーン内にいる限り継続し、座位候補が現れた時点で交代する。
        // - PilotActive が落ちていれば、座位候補の中で最高優先度の者を選ぶ。
        // Why-not: 獲得者だけが共有状態を書く方式にしない。書き込みの後着で状態が食い違う (LWW)。
        public void DeterminePilot()
        {
            int localId = Networking.LocalPlayer.playerId;
            int incumbent = PilotID.Data;
            // 離脱者は OnPlayerLeft が PilotID を -1 にするため、ここでは生存確認をしない。
            bool active = PilotActive.Data && incumbent != -1;

            FDMiSeat best = null;
            FDMiSeat incumbentSeat = null;
            foreach (FDMiSeat seat in allSeats)
            {
                if (seat.seatedPlayer == null || seat.pilotPriority < 0)
                    continue;
                if (seat.seatedPlayer.playerId == incumbent)
                    incumbentSeat = seat;
                if (best == null || seat.pilotPriority > best.pilotPriority)
                    best = seat;
            }

            int pilotId = -1;
            if (active)
            {
                bool replaced = incumbentSeat != null
                    ? (best != null && best.pilotPriority > incumbentSeat.pilotPriority)
                    : best != null;

                pilotId = replaced ? best.seatedPlayer.playerId : incumbent;
            }
            else if (best != null)
            {
                pilotId = best.seatedPlayer.playerId;
            }

            // FDMiData.Set は常にコールバックを発火する。同値で書かない (選出の再入を止める)。
            if (PilotID.Data != pilotId)
                PilotID.Data = pilotId;
            if (IsPilot.Data != (pilotId == localId))
                IsPilot.Data = pilotId == localId;
        }

        // OnPlayerLeft は全クライアントで呼ばれる。離脱者の席を解放して再選出する。
        // 離脱者がパイロットなら incumbent から外し、次候補へ移る。
        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            int departId = player.playerId;

            foreach (FDMiSeat seat in allSeats)
                if (seat.seatedPlayer != null && seat.seatedPlayer.playerId == departId)
                    seat.seatedPlayer = null;

            if (PilotID.Data == departId)
                PilotID.Data = -1;

            DeterminePilot();
        }

        // ローカルプレイヤーがパイロット席に着席しているか。
        // 着席中はゾーン内とみなす (席は機体内にある)。立ちパイロットだけがゾーン判定を受ける。
        public bool _IsPilotSeated()
        {
            int localId = Networking.LocalPlayer.playerId;
            foreach (FDMiSeat seat in allSeats)
                if (seat.pilotPriority >= 0
                    && seat.seatedPlayer != null
                    && seat.seatedPlayer.playerId == localId)
                    return true;
            return false;
        }
    }
}
