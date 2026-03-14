using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WataOfuton.Tool.WNDP.Editor
{
    /// <summary>
    /// session 単位で Scene 上の GameObject / prefab instance を扱うための API。
    /// 生成・複製・親子付け・破棄は Source Scene ではなく session Scene に対して行う。
    /// </summary>
    public sealed class WorldTransientObjects
    {
        private readonly Scene _scene;

        internal WorldTransientObjects(Scene scene)
        {
            _scene = scene;
        }

        /// <summary>
        /// 現在の session Scene 上に新しい GameObject を生成する。
        /// </summary>
        public GameObject Create(string name, Transform parent = null)
        {
            var gameObject = new GameObject(string.IsNullOrWhiteSpace(name) ? "Transient Object" : name);
            AttachToSession(gameObject, parent);
            return gameObject;
        }

        /// <summary>
        /// 現在の session Scene 上で <paramref name="source" /> を複製する。
        /// </summary>
        public GameObject Clone(GameObject source, Transform parent = null, string nameOverride = null)
        {
            if (source == null)
            {
                return null;
            }

            var clone = UnityEngine.Object.Instantiate(source);
            if (clone == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(nameOverride))
            {
                clone.name = nameOverride;
            }

            AttachToSession(clone, parent);
            return clone;
        }

        /// <summary>
        /// <paramref name="sourceComponent" /> を保持する GameObject を複製し、
        /// 複製先の同型 component を返す。
        /// </summary>
        public T CloneComponentHost<T>(T sourceComponent, Transform parent = null, string nameOverride = null)
            where T : Component
        {
            if (sourceComponent == null)
            {
                return null;
            }

            var clone = Clone(sourceComponent.gameObject, parent, nameOverride);
            return clone != null ? clone.GetComponent<T>() : null;
        }

        /// <summary>
        /// prefab asset を現在の session Scene 上へ instance 化する。
        /// </summary>
        public GameObject InstantiatePrefab(GameObject prefabAsset, Transform parent = null, string nameOverride = null)
        {
            if (prefabAsset == null)
            {
                return null;
            }

            if (!PrefabUtility.IsPartOfPrefabAsset(prefabAsset))
            {
                throw new ArgumentException("InstantiatePrefab requires a prefab asset.", nameof(prefabAsset));
            }

            var instance = PrefabUtility.InstantiatePrefab(prefabAsset, _scene) as GameObject;
            if (instance == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(nameOverride))
            {
                instance.name = nameOverride;
            }

            if (parent != null)
            {
                instance.transform.SetParent(parent, false);
            }

            return instance;
        }

        /// <summary>
        /// 生成物をまとめる generated root を取得し、存在しなければ生成する。
        /// parent 指定時はその直下、未指定時は session Scene の root を対象とする。
        /// 返した object の transform は generated root 用の既定値へ揃える。
        /// </summary>
        public GameObject GetOrCreateGeneratedRoot(string name, Transform parent = null)
        {
            var normalizedName = string.IsNullOrWhiteSpace(name) ? "Generated Root" : name;
            GameObject root;

            if (parent != null)
            {
                var existingChild = parent.Find(normalizedName);
                root = existingChild != null ? existingChild.gameObject : Create(normalizedName, parent);
            }
            else
            {
                root = FindRootObject(normalizedName) ?? Create(normalizedName);
            }

            if (parent != null && root.transform.parent != parent)
            {
                root.transform.SetParent(parent, false);
            }

            ResetTransformForGeneratedRoot(root.transform, parent == null);
            return root;
        }

        /// <summary>
        /// 指定した GameObject に component が無ければ追加し、存在すれば既存の component を返す。
        /// </summary>
        public T EnsureComponent<T>(GameObject target) where T : Component
        {
            if (target == null)
            {
                return null;
            }

            var component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        /// <summary>
        /// 指定した component を現在の session Scene 上で破棄する。
        /// </summary>
        public void DestroyComponent(Component target)
        {
            if (target == null)
            {
                return;
            }

            UnityEngine.Object.DestroyImmediate(target);
        }

        /// <summary>
        /// 指定した Transform の local 位置・回転・スケールを既定値へ戻す。
        /// </summary>
        public void ResetLocalTransform(Transform target)
        {
            if (target == null)
            {
                return;
            }

            target.localPosition = Vector3.zero;
            target.localRotation = Quaternion.identity;
            target.localScale = Vector3.one;
        }

        /// <summary>
        /// source の Transform 値を destination へコピーする。
        /// 親が同じ場合は local 値、異なる場合は world 位置・回転と localScale をコピーする。
        /// </summary>
        public void CopyTransform(Transform source, Transform destination)
        {
            if (source == null || destination == null)
            {
                return;
            }

            if (source.parent == destination.parent)
            {
                destination.localPosition = source.localPosition;
                destination.localRotation = source.localRotation;
                destination.localScale = source.localScale;
                return;
            }

            destination.position = source.position;
            destination.rotation = source.rotation;
            destination.localScale = source.localScale;
        }

        /// <summary>
        /// 現在の session Scene 上の GameObject を破棄する。
        /// </summary>
        public void Destroy(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            UnityEngine.Object.DestroyImmediate(target);
        }

        /// <summary>
        /// 現在の session Scene 上の GameObject の active 状態を切り替える。
        /// </summary>
        public void SetActive(GameObject target, bool active)
        {
            if (target == null)
            {
                return;
            }

            target.SetActive(active);
        }

        private GameObject FindRootObject(string name)
        {
            if (!_scene.IsValid())
            {
                return null;
            }

            foreach (var rootObject in _scene.GetRootGameObjects())
            {
                if (rootObject != null && rootObject.name == name)
                {
                    return rootObject;
                }
            }

            return null;
        }

        private static void ResetTransformForGeneratedRoot(Transform target, bool isSceneRoot)
        {
            if (target == null)
            {
                return;
            }

            if (isSceneRoot)
            {
                target.position = Vector3.zero;
                target.rotation = Quaternion.identity;
                target.localScale = Vector3.one;
                return;
            }

            target.localPosition = Vector3.zero;
            target.localRotation = Quaternion.identity;
            target.localScale = Vector3.one;
        }

        private void AttachToSession(GameObject gameObject, Transform parent)
        {
            if (gameObject == null)
            {
                return;
            }

            if (parent != null)
            {
                gameObject.transform.SetParent(parent, false);
                return;
            }

            if (_scene.IsValid() && gameObject.scene != _scene)
            {
                SceneManager.MoveGameObjectToScene(gameObject, _scene);
            }
        }
    }
}