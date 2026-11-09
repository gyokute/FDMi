using UdonSharp;
using UnityEngine;

namespace FDMi.core
{
    /// <summary>
    /// FDMiData の値変化コールバックで Renderer の Material を切り替えるドライバ。
    /// </summary>
    public class FDMiMaterialChangeByValue : FDMiBehaviour
    {
        [Tooltip("入力データの解決パス。名前空間の区切りは /")]
        public string dataPath;

        [FDMiDataPath(nameof(dataPath)), FDMiRegisterCallback(nameof(OnChange))]
        [Tooltip("マテリアルを決める入力。dataPath から解決する")]
        public FDMiData Value;

        [Tooltip("入力値 → changingMaterial の添字の変換カーブ")]
        public AnimationCurve outputCurve;

        [Tooltip("対象の解決パス。名前空間の区切りは /")]
        public string targetPath;

        [FDMiDataPath(nameof(targetPath))]
        [Tooltip("マテリアルを切り替える Transform。targetPath から解決する")]
        public FDMiTransformRef target;

        [Tooltip("入力値の添字で選ぶマテリアル")]
        public Material[] changingMaterial;

        // Renderer.material は取得時にレンダラー専用の複製を生成する。複製は明示的に破棄しないと残るため、
        // 切り替え時に前の複製を、終了時に元の Material へ戻してから最後の複製を Destroy する。
        // target は設定時に解決される静的参照なので、GetComponent のコストを避けて Renderer をキャッシュする。
        Renderer targetRenderer;
        Material originalMaterial;
        Material instance;

        // 同じ添字への再適用で複製を作り直さないためのキャッシュ。初期値 -1 で最初の OnChange は必ず適用される。
        int currentIndex = -1;

        public void Initialize()
        {
            targetRenderer = target.data[0].GetComponent<Renderer>();
            OnChange();
        }

        public void OnChange()
        {
            // Initialize を経ずに値変化コールバックが来た場合の遅延解決。
            if (targetRenderer == null)
                targetRenderer = target.data[0].GetComponent<Renderer>();

            float v = outputCurve.Evaluate(Value.GetFloat());
            int index = Mathf.Clamp(Mathf.RoundToInt(v), 0, changingMaterial.Length - 1);
            if (index == currentIndex)
                return;

            // 最初の適用時に、終了時に戻すための元の Material を控える。
            if (currentIndex == -1)
                originalMaterial = targetRenderer.sharedMaterial;

            currentIndex = index;
            targetRenderer.material = changingMaterial[index];

            if (instance != null)
                Destroy(instance);

            instance = targetRenderer.material;
        }

        void OnDestroy()
        {
            if (instance == null)
                return;

            // Renderer が残っている場合に破棄済み Material を参照させないため、元へ戻してから複製を破棄する。
            if (targetRenderer != null)
                targetRenderer.sharedMaterial = originalMaterial;

            Destroy(instance);
        }
    }
}
