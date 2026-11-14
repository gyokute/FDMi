using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.sync
{
    // §8.2。機体モデルを root の有無で付け替え、root フレームに合わせる。
    // root の間は Manager が原点を Unity 原点・identity に写すため、モデルは世界位置 0・世界回転 identity。
    // Why-not: 位置だけ 0 にしない。parent 代入は世界姿勢を保存するので、root 切替時の世界回転が残り、
    // 機体モデル (と配下の station) が同期姿勢の root フレームから回転してずれる。着席位置が
    // その回転を含んだまま player.GetPosition() で送られ、他のクライアントで乗員の位置がずれる。
    public class FDMiModelTransformFollower : FDMiBehaviour
    {
        [Tooltip("root 判定の解決パス")]
        public string IsRootPath = "~/IsRoot";

        [FDMiDataPath(nameof(IsRootPath)), FDMiRegisterCallback(nameof(OnChange))]
        [Tooltip("この機体が root か")]
        public FDMiBool IsRoot;

        [Tooltip("root のときの親")]
        public Transform parentWhenTrue;

        [Tooltip("root でないときの親")]
        public Transform parentWhenFalse;

        public void OnChange()
        {
            if (IsRoot.Data)
            {
                transform.parent = parentWhenTrue;
                transform.position = Vector3.zero;
                transform.rotation = Quaternion.identity;
            }
            else
            {
                transform.parent = parentWhenFalse;
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }
        }
    }
}
