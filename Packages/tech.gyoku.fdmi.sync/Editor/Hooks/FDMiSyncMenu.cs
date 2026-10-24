using FDMi.sync.Editor.Application.UseCases;
using FDMi.sync.Editor.Domain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FDMi.sync.Editor.Hooks
{
    // §11.2 Setup 手順の入口。焼き (1〜7) と検証 (8) を 1 メニューにまとめる。
    public static class FDMiSyncMenu
    {
        [MenuItem("FDMi/Sync/Bake Scene")]
        public static void BakeScene()
        {
            FDMiSyncManager manager = Object.FindObjectOfType<FDMiSyncManager>();
            if (manager == null)
            {
                Debug.LogError("[FDMiSync] V-01: FDMiSyncManager がシーンにありません。");
                return;
            }

            BakeSyncSceneUseCase.Execute(manager);
            FDMiSyncValidator.Result result = FDMiSyncValidator.Validate(manager);
            if (result.Errors.Count != 0)
            {
                Debug.LogError("[FDMiSync] 焼きを中止しました (エラー " + result.Errors.Count + " 件)。");
                return;
            }

            // 手順 8: エラーが 0 件なら保存する。
            UnityEngine.SceneManagement.Scene scene = manager.gameObject.scene;
            if (scene.IsValid() && !string.IsNullOrEmpty(scene.path)) EditorSceneManager.SaveScene(scene);
            Debug.Log("[FDMiSync] 焼きと検証が完了しました。");
        }

        [MenuItem("FDMi/Sync/Validate")]
        public static void Validate()
        {
            FDMiSyncValidator.Result result = FDMiSyncValidator.Validate(Object.FindObjectOfType<FDMiSyncManager>());
            Debug.Log("[FDMiSync] 検証: エラー " + result.Errors.Count + " 件 / 警告 " + result.Warnings.Count + " 件。");
        }
    }
}
