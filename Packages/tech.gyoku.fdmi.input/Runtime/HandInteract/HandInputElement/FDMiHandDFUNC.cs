using FDMi.core;
using UnityEngine;

namespace FDMi.input
{
    /// <summary>
    /// 既定 Group 選択中 (手がどの操作領域にも触れていない) に有効になる DFUNC。
    /// PadV / PadH で放射状に並んだ選択肢から機能を選び、Trigger 押下の間、
    /// 選択中の選択肢 (target) へ掴み通知を転送する。機能の中身と同期は target 側が持つ。
    /// Grip は検出しない (OnGrabStart / OnGrabEnd はオーバーライドしない)。
    /// </summary>
    public class FDMiHandDFUNC : FDMiHandInputElement
    {
        public string matchingTag = "FingerIndexL";

        [Tooltip("選択肢の表示名。targets と並列。表示文字は Builder が確定させるため、実行時には読まない。")]
        public string[] labels = new string[0];

        [Tooltip("Trigger 押下で掴み通知を転送する要素。labels と並列。null は選択不可の空きスロット。Axis のみを読む要素を設定すること。")]
        public FDMiHandInputElement[] targets = new FDMiHandInputElement[0];

        [Tooltip("選択を確定するパッド傾斜。SAFV DialSensitivity 相当。")]
        public float selectThreshold = 0.7f;

        [Tooltip("掴みセッションを開始する Trigger 閾値。FDMiHandAxisInput.threshold に合わせる。")]
        public float triggerThreshold = 0.5f;

        public FDMiHandDFUNCDisplay display;

        private int selected;
        private FDMiHandInputElement sessionTarget;
        private bool prevTrigger;

        public override void OnSelectStart(FDMiHandInputGroup group)
        {
            // 選択開始時点で Trigger が押下済みなら吞み込む (SAFV DFUNC_Selected の TriggerLastFrame 相当)。
            prevTrigger = group.axes[(int)FDMiHandAxisType.Trigger] > triggerThreshold;
            if (display)
                display.OnSelectStart(selected);
        }

        // セッション駆動源は WhileSelect と WhileGrab の両方。
        // SAFV の DFUNC は選択中に自前 Update で Trigger をポーリングする。FDMi では「選択中」に相当する
        // 毎フレーム通知が Select (触れていない) と Grab (Grip で掴んでいる) に分かれるため、両方から更新する。
        // 掴みの開始・終了イベント自体は無視するので Grip を検出することにはならない。
        public override void WhileSelect(FDMiHandInputGroup group)
        {
            UpdateDFUNC(group);
        }

        public override void WhileGrab(FDMiHandInputGroup group)
        {
            UpdateDFUNC(group);
        }

        public override void OnSelectEnd()
        {
            EndSession();
            if (display)
                display.OnSelectEnd();
        }

        private void UpdateDFUNC(FDMiHandInputGroup group)
        {
            if (display)
                display.OnTick();

            bool triggerPressed = group.axes[(int)FDMiHandAxisType.Trigger] > triggerThreshold;
            if (triggerPressed && !prevTrigger)
                StartSession(group);
            else if (!triggerPressed && prevTrigger)
                EndSession();
            prevTrigger = triggerPressed;
            if (sessionTarget)
                sessionTarget.WhileGrab(group);

            UpdateSelection(group);
        }

        private void StartSession(FDMiHandInputGroup group)
        {
            FDMiHandInputElement target = targets[selected];
            // target なしの選択肢 (空きスロット) はセッションを開始しない。
            if (!target)
                return;
            sessionTarget = target;
            target.OnGrabStart(group);
            if (display)
                display.SetSessionTarget(selected);
        }

        private void EndSession()
        {
            if (!sessionTarget)
                return;
            FDMiHandInputElement target = sessionTarget;
            sessionTarget = null;
            target.OnGrabEnd();
            if (display)
                display.SetSessionTarget(-1);
        }

        private void UpdateSelection(FDMiHandInputGroup group)
        {
            float padH = group.axes[(int)FDMiHandAxisType.PadH];
            float padV = group.axes[(int)FDMiHandAxisType.PadV];
            // 閾値未満 (中心) では現在の選択を保持する。
            if (padH * padH + padV * padV < selectThreshold * selectThreshold)
                return;

            // 0 = 正面上, 正 = 時計回り。上を option 0 の中心にする (SAFV DivideStraightUp 相当)。
            float angle = Mathf.Atan2(padH, padV) * Mathf.Rad2Deg;
            int sector = Mathf.RoundToInt(angle * targets.Length / 360f);
            sector = ((sector % targets.Length) + targets.Length) % targets.Length;
            // 空きスロットは選択不可: 選択を保持する。
            if (sector == selected || !targets[sector])
                return;
            selected = sector;
            if (display)
                display.SetSelection(selected);
        }
    }
}
