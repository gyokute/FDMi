using FDMi.core.Editor.Domain.Entities;
using FDMi.core.Editor.Domain.Repositories;
using FDMi.core.Editor.Infrastructure.Repositories;
using UnityEditor;
using UnityEngine;

namespace FDMi.core.Editor.Application.UseCases
{
    /// <summary>
    /// FDMiDataSyncGroup の channelPaths を FDMiData へ解決し、チャネル幅から同期語の本数を焼く。
    /// 解決できないチャネルは前回値を維持する（ResolveDataPathsUseCase と同じ規約）。
    /// </summary>
    public class ResolveSyncGroupUseCase : IFDMiAutoSetupUseCase
    {
        readonly IFDMiDataRepository _repository;

        public ResolveSyncGroupUseCase()
            : this(new SceneFDMiDataRepository()) { }

        public ResolveSyncGroupUseCase(IFDMiDataRepository repository)
        {
            _repository = repository;
        }

        public void Execute(Object target)
        {
            var group = target as FDMiDataSyncGroup;
            if (group == null)
                return;

            var so = new SerializedObject(target);
            so.Update();

            var pathsProp = so.FindProperty(nameof(FDMiDataSyncGroup.channelPaths));
            var channelsProp = so.FindProperty(nameof(FDMiDataSyncGroup.channels));
            var widthsProp = so.FindProperty(nameof(FDMiDataSyncGroup.channelWidths));
            var scalesProp = so.FindProperty(nameof(FDMiDataSyncGroup.channelScales));

            int count = pathsProp.arraySize;
            var resolved = new FDMiData[count];
            for (int i = 0; i < count; i++)
            {
                string pathText = pathsProp.GetArrayElementAtIndex(i).stringValue;
                var matches = _repository.FindAll(
                    group.gameObject,
                    FDMiDataPath.Parse(pathText),
                    typeof(FDMiData)
                );
                if (matches.Length > 0)
                {
                    resolved[i] = matches[0];
                }
                else if (i < channelsProp.arraySize)
                {
                    resolved[i] = channelsProp.GetArrayElementAtIndex(i).objectReferenceValue as FDMiData;
                }
            }

            channelsProp.arraySize = count;
            for (int i = 0; i < count; i++)
                channelsProp.GetArrayElementAtIndex(i).objectReferenceValue = resolved[i];

            // 量子化規則が幅を決めるチャネルは、焼く前に幅を確定させる。
            // Bool は 1bit、Unit/Signed は 255/127 固定なので 8bit。ここを任せると 8bit の Bool が残り、
            // 語数だけが増える（ランタイムは幅を見ずに 0/1 を書くため正しさは変わらない）。
            var widths = new byte[count];
            for (int i = 0; i < count; i++)
            {
                byte scale = (byte)scalesProp.GetArrayElementAtIndex(i).intValue;
                int width = widthsProp.GetArrayElementAtIndex(i).intValue;
                if (scale == FDMiDataSyncGroup.ScaleBool)
                    width = 1;
                else if (scale == FDMiDataSyncGroup.ScaleUnit || scale == FDMiDataSyncGroup.ScaleSigned)
                    width = 8;

                widths[i] = (byte)width;
                if (widthsProp.GetArrayElementAtIndex(i).intValue != width)
                    widthsProp.GetArrayElementAtIndex(i).intValue = width;
            }

            var wordsProp = so.FindProperty(nameof(FDMiDataSyncGroup.words));
            int wordCount = FDMiDataSyncGroup.ComputeWordCount(widths);
            if (wordsProp.arraySize != wordCount)
                wordsProp.arraySize = wordCount;

            so.ApplyModifiedPropertiesWithoutUndo();

            // ビルド時の ResolveAll はユースケースの実行順を保証しない。解決直後に登録しておけば、
            // RegisterCallbacksUseCase が古い channels で先に走っても最終状態は新しいチャネルに一致する。
            new RegisterCallbacksUseCase().Execute(target);
        }

        public void ExecuteAll()
        {
            foreach (var group in Object.FindObjectsByType<FDMiDataSyncGroup>(FindObjectsSortMode.None))
                Execute(group);
        }
    }
}
