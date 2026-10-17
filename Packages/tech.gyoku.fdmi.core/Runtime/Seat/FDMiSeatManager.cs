using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    // FDMiSeat と組み合わせ、Seat の占有からパイロットを一意に決定する。
    // パイロット状態は ~/IsPilot / ~/HasPilot / ~/PilotID の共有データ (同期は FDMiDataSync が担う) に置かれ、
    // 書き込みは次の 2 経路に限定する。
    // - 獲得: FDMiSeat.TryTakePilot (獲得者自身のクライアントのみが書く)
    // - 喪失: FDMiZoneDetector (ゾーン外出) / 本クラス (ワールド離脱)。~/HasPilot の変更で再選出が走る
    // パイロット不在は許容される。Seat の占有状態 (seatedPlayer) は全クライアントで同一になることを前提とする。
    public class FDMiSeatManager : FDMiBehaviour
    {
        [Header("FDMiSeatと組み合わせ、Seatに座ったプレイヤーから、パイロットを一意に決定します。")]
        public FDMiSeat[] allSeats;

        [FDMiDataPathConst("~/IsPilot")]
        public FDMiBool IsPilot;

        [FDMiDataPathConst("~/HasPilot"), FDMiRegisterCallback(nameof(DetermineNewPilot))]
        public FDMiBool HasPilot;

        [FDMiDataPathConst("~/PilotID"), FDMiRegisterCallback(nameof(RecoverPilotState))]
        public FDMiInt PilotID;

        // パイロット不在時に、残る Seat 占有者の中から最高 PilotPriority の座席を新パイロットとする。
        // 同一 priority の座席は allSeats の並び順で先の座席を選ぶ (全クライアントが同一結果を得る)。
        // 獲得書き込みは獲得者自身のクライアントのみが行うため、共有状態は一意に保たれる。
        public void DetermineNewPilot()
        {
            if (HasPilot.Data)
                return;

            FDMiSeat candidate = null;
            foreach (FDMiSeat seat in allSeats)
            {
                if (seat.seatedPlayer == null)
                    continue;
                if (candidate == null || seat.pilotPriority > candidate.pilotPriority)
                    candidate = seat;
            }

            if (candidate != null && candidate.seatedPlayer.isLocal)
                candidate.TryTakePilot();
        }

        // 遅延した解放書き込み (LWW の後着) に対する自己修復:
        // HasPilot=true なのに PilotID==-1 の矛盾 (新パイロットの獲得値が後着の解放値に潰された状態) を解消する。
        // OnDeserialization 経由でも発火するため、他クライアントの遅延書き込みからも回復する。
        public void RecoverPilotState()
        {
            if (HasPilot.Data && PilotID.Data == -1)
                HasPilot.Data = false; // ~/HasPilot の変更で再選出が起動
        }

        // OnPlayerLeft は全クライアントで呼ばれるため、離脱者の Seat 占有を解放した後、
        // 離脱者がパイロットなら各クライアントから同一値の解放書き込みが行われ収束する。
        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            int departId = player.playerId;

            foreach (FDMiSeat seat in allSeats)
                if (seat.seatedPlayer != null && seat.seatedPlayer.playerId == departId)
                    seat.seatedPlayer = null;

            if (PilotID.Data != departId)
                return;

            IsPilot.Data = false;
            PilotID.Data = -1;
            HasPilot.Data = false; // ~/HasPilot の変更が再選出 (DetermineNewPilot) を起動する
        }
    }
}
