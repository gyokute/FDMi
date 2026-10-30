using FDMi.core.Editor.Preview;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FDMi.core.Editor.Tests
{
    public class FDMiPreviewSessionTests
    {
        const string AssetPath = "Assets/__FDMiPreviewSessionTest.prefab";

        [Test]
        public void Stop_RestoresSceneTransform()
        {
            var target = new GameObject("session-target");
            try
            {
                target.transform.localPosition = new Vector3(1f, 2f, 3f);

                FDMiPreviewSession.Begin(target);
                var participant = FDMiPreviewSession.Track(
                    target.transform,
                    value => target.transform.localPosition = new Vector3(value, 0f, 0f),
                    null
                );
                participant.SetValue(5f);

                Assert.That(target.transform.localPosition.x, Is.EqualTo(5f));

                FDMiPreviewSession.Stop();

                Assert.That(
                    Vector3.Distance(target.transform.localPosition, new Vector3(1f, 2f, 3f)),
                    Is.LessThan(1e-5f)
                );
            }
            finally
            {
                FDMiPreviewSession.Stop();
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void Stop_RemovesTransientPrefabOverride()
        {
            var root = new GameObject("session-prefab");
            var child = new GameObject("session-prefab-child");
            child.transform.SetParent(root.transform);
            GameObject instance = null;

            try
            {
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, AssetPath);
                Object.DestroyImmediate(root);

                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Transform target = instance.transform.Find("session-prefab-child");
                Vector3 before = target.localPosition;

                FDMiPreviewSession.Begin(instance);
                var participant = FDMiPreviewSession.Track(
                    target,
                    value => target.localPosition = new Vector3(value, 0f, 0f),
                    null
                );
                participant.SetValue(2f);

                FDMiPreviewSession.Stop();

                Assert.That(Vector3.Distance(target.localPosition, before), Is.LessThan(1e-5f));
                Assert.That(HasPositionModification(instance, target), Is.False);
            }
            finally
            {
                FDMiPreviewSession.Stop();
                if (instance != null)
                    Object.DestroyImmediate(instance);
                AssetDatabase.DeleteAsset(AssetPath);
            }
        }

        static bool HasPositionModification(GameObject instance, Transform target)
        {
            PropertyModification[] modifications = PrefabUtility.GetPropertyModifications(instance);
            if (modifications == null)
                return false;

            for (int i = 0; i < modifications.Length; i++)
            {
                if (modifications[i].target != target)
                    continue;
                string path = modifications[i].propertyPath;
                if (path != null && path.StartsWith("m_LocalPosition"))
                    return true;
            }
            return false;
        }
    }
}
