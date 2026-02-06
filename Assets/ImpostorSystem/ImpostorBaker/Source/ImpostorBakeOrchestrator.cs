using UnityEngine;
using ImpostorPlugin.Utils;

namespace ImpostorPlugin
{
    /// <summary>
    /// Orchestrator for the impostor baking process.
    /// Coordinates ImpostorSnapshotBuilder and ImpostorAtlasRenderer.
    /// </summary>
    public class ImpostorBakeOrchestrator
    {
        private const string BAKER_SHADER_NAME = "IMP/ImpostorBaker";

        private Shader _bakerShader;
        private Material _bakerMaterial;

        private Renderer[] _renderers;
        private Mesh[] _meshes;
        private Material[][] _materials;

        private ImpostorSnapshotBuilder _snapshotBuilder;

        /// <summary>
        /// Sets renderers, meshes, and materials for baking.
        /// </summary>
        public void SetRenderersAndMeshes(Renderer[] renderers, Mesh[] meshes, Material[][] materials)
        {
            _renderers = renderers;
            _meshes = meshes;
            _materials = materials;
        }

        /// <summary>
        /// Initializes shaders and materials for baking.
        /// </summary>
        public void SetMaterialsAndShaders()
        {
            if (_bakerShader == null)
            {
                _bakerShader = Shader.Find(BAKER_SHADER_NAME);
                if (_bakerShader == null)
                {
                    Debug.LogError($"[ImpostorBakeOrchestrator] Shader '{BAKER_SHADER_NAME}' not found!");
                    return;
                }
                _bakerMaterial = new Material(_bakerShader);
            }

            _snapshotBuilder = new ImpostorSnapshotBuilder();
        }

        /// <summary>
        /// Main impostor baking method.
        /// Coordinates snapshot building and atlas rendering.
        /// </summary>
        /// <returns>Tuple of albedo and normal atlases</returns>
        public (RenderTexture albedo, RenderTexture normal) Bake(Transform transform, ImpostorData data)
        {
            _snapshotBuilder.InitializeSnapshotData(transform, data, _renderers, _meshes);

            Color32[] pixelMask = _snapshotBuilder.RenderMinMaxMask(data, _bakerMaterial, _renderers, _meshes);

            _snapshotBuilder.OptimizeAndRebuild(data, pixelMask);

            var atlasRenderer = new ImpostorAtlasRenderer(
                _bakerMaterial,
                _renderers,
                _meshes,
                _materials
            );

            return atlasRenderer.BakeAtlases(data);
        }
    }
}
