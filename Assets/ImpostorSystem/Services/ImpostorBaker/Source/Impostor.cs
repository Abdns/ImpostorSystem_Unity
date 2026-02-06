using System.Collections.Generic;
using UnityEngine;
using ImpostorPlugin.Utils;

namespace ImpostorPlugin
{
    /// <summary>
    /// Component for baking impostors.
    /// Attach to a GameObject containing meshes to bake.
    /// </summary>
    public class Impostor : MonoBehaviour
    {
        [Header("Atlas Settings")]
        [SerializeField] private int _atlasResolution = 2048;
        [SerializeField] private int _framesCount = 12;

        [Header("Baking Settings")]
        [SerializeField][Range(0, 800)] private float _framePadding = 0;
        [SerializeField] private bool _isHemiSphere = true;

        private ImpostorBakeOrchestrator _baker;

        /// <summary>
        /// Starts the impostor baking process.
        /// Creates atlas textures, saves them as assets, and generates impostor GameObject.
        /// </summary>
        [ContextMenu("Bake Impostor")]
        public void Bake()
        {
            ImpostorData data = CreateBakeData();

            TransformState originalState = SaveTransformState();
            ResetTransform();

            SetupBaker();

            var (albedoAtlas, normalAtlas) = _baker.Bake(transform, data);

            RestoreTransformState(originalState);

#if UNITY_EDITOR
            GameObject impostorObject = ImpostorAssetCreator.CreateAssets(data, gameObject, albedoAtlas, normalAtlas);

            if (impostorObject != null)
            {
                Debug.Log($"[Impostor] Created impostor object: {impostorObject.name}");
            }
#else
            Debug.LogWarning("[Impostor] Asset creation is only available in the Unity Editor.");
#endif

            RenderTexture.ReleaseTemporary(albedoAtlas);
            RenderTexture.ReleaseTemporary(normalAtlas);
        }

        /// <summary>
        /// Creates ImpostorData from serialized fields.
        /// </summary>
        /// <returns>Configured ImpostorData instance.</returns>
        private ImpostorData CreateBakeData()
        {
            return new ImpostorData
            {
                atlasResolution = _atlasResolution,
                framesCount = SnapshotTools.GetValidFrameCount(_framesCount),
                isHalfSphere = _isHemiSphere,
                framePadding = _framePadding
            };
        }

        /// <summary>
        /// Initializes the baker with renderers, meshes, and materials from child objects.
        /// </summary>
        private void SetupBaker()
        {
            _baker = new ImpostorBakeOrchestrator();

            var renderers = GetChildRenderers();
            var (meshes, materials) = ExtractMeshesAndMaterials(renderers);

            _baker.SetMaterialsAndShaders();
            _baker.SetRenderersAndMeshes(renderers, meshes, materials);
        }

        /// <summary>
        /// Collects all MeshRenderers from child objects, excluding the renderer on this object.
        /// </summary>
        private Renderer[] GetChildRenderers()
        {
            var renderers = new List<MeshRenderer>(GetComponentsInChildren<MeshRenderer>(true));
            
            var selfRenderer = GetComponent<MeshRenderer>();
            if (selfRenderer != null)
            {
                renderers.Remove(selfRenderer);
            }

            if (renderers.Count == 0)
            {
                Debug.LogWarning($"[Impostor] No MeshRenderers found in children of {gameObject.name}");
            }

            return renderers.ToArray();
        }

        /// <summary>
        /// Extracts meshes and materials from the given renderers.
        /// </summary>
        private (Mesh[] meshes, Material[][] materials) ExtractMeshesAndMaterials(Renderer[] renderers)
        {
            var meshes = new Mesh[renderers.Length];
            var materials = new Material[renderers.Length][];

            for (int i = 0; i < renderers.Length; i++)
            {
                var meshFilter = renderers[i].GetComponent<MeshFilter>();
                meshes[i] = meshFilter != null ? meshFilter.sharedMesh : null;
                materials[i] = renderers[i].sharedMaterials;
            }

            return (meshes, materials);
        }

        #region Transform State Management

        /// <summary>
        /// Stores transform position, rotation, and scale.
        /// </summary>
        private struct TransformState
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
        }

        /// <summary>
        /// Saves the current transform state.
        /// </summary>
        /// <returns>TransformState containing current position, rotation, and scale.</returns>
        private TransformState SaveTransformState()
        {
            return new TransformState
            {
                Position = transform.position,
                Rotation = transform.rotation,
                Scale = transform.localScale
            };
        }

        /// <summary>
        /// Resets transform to origin with identity rotation and unit scale.
        /// Required for correct baking calculations.
        /// </summary>
        private void ResetTransform()
        {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Restores transform to a previously saved state.
        /// </summary>
        /// <param name="state">TransformState to restore.</param>
        private void RestoreTransformState(TransformState state)
        {
            transform.position = state.Position;
            transform.rotation = state.Rotation;
            transform.localScale = state.Scale;
        }

        #endregion
    }
}
