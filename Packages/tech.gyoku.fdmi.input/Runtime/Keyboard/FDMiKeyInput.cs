using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.input
{
    public enum FDMiKeyMode
    {
        Momentum,
        Alternate,
        Force,
    }

    // キーボードの押下を FDMiData へ書く。手入力と違いコンタクトやグラブの契機を持たず、常時受ける。
    // 1フレームのコストは「テーブル内のキー数ぶんの GetKey」で、押下中のキーが無ければ
    // Input.anyKeyDown 1回でループごと省く。
    // Why not キー1つにつきコンポーネント1つ: Update のイベントエントリがキー数ぶん増える。
    // Why not 毎フレーム Set: Set はリスナーごとの SendCustomEvent を伴う。押下・離上のエッジだけで足りる。
    // Why not FixedUpdate: 押下検出はフレーム単位で行う必要があり、FixedUpdate では短いタップを取りこぼす。
    [DefaultExecutionOrder(-300)]
    public class FDMiKeyInput : FDMiBehaviour
    {
        [Tooltip("監視するキー。KeyCode の値を並べる")]
        // Why not KeyCode[]: Udon は extern enum の配列を扱えない。値は KeyCode の int。
        public int[] keys = new int[0];

        [Tooltip("押下を出力値へ変換する規則")]
        public FDMiKeyMode[] modes = new FDMiKeyMode[0];

        [Tooltip("書き込み先。keys と同じ並び")]
        public FDMiData[] outputs = new FDMiData[0];

        [Tooltip("押している間の値")]
        public float[] onValues = new float[0];

        [Tooltip("離したときの値。Alternate では反転先、Force では未使用")]
        public float[] offValues = new float[0];

        private int _holdCount = 0;

        void OnDisable()
        {
            _holdCount = 0;
            for (int i = 0; i < keys.Length; i++)
                OnRelease(i);
        }

        void Update()
        {
            if (_holdCount == 0 && !Input.anyKeyDown)
                return;

            for (int i = 0; i < keys.Length; i++)
            {
                if (Input.GetKeyDown((KeyCode)keys[i]))
                    OnPress(i);
                if (Input.GetKeyUp((KeyCode)keys[i]))
                    OnRelease(i);
            }
        }

        void OnPress(int i)
        {
            _holdCount++;
            FDMiData output = outputs[i];
            switch (modes[i])
            {
                case FDMiKeyMode.Momentum:
                    output.Set(onValues[i]);
                    break;
                case FDMiKeyMode.Force:
                    output.Set(onValues[i]);
                    return;
                case FDMiKeyMode.Alternate:
                    // 反転の基準は出力の現在値。初期値が on 側でも1回目の押下で off へ落ちる。
                    output.Set(Mathf.Approximately(output.GetFloat(), onValues[i]) ? offValues[i] : onValues[i]);
                    break;
            }
        }

        void OnRelease(int i)
        {
            _holdCount = Mathf.Max(0, _holdCount - 1);
            switch (modes[i])
            {
                case FDMiKeyMode.Momentum:
                    outputs[i].Set(offValues[i]);
                    break;
            }
        }
    }
}
