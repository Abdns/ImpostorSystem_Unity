using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using ImpostorPlugin.Utils;

namespace ImpostorPlugin
{
    /// <summary>
    /// Responsible for rendering impostor atlases.
    /// Renders G-Buffer for each snapshot and assembles results into atlases.
    /// </summary>
    public class ImpostorAtlasRenderer
    {
        private readonly Renderer[] _renderers;
        private readonly Mesh[] _meshes;
        private readonly Material[][] _materials;
        private readonly Material _bakerMaterial;

        // Render buffers (initialized per bake session)
        private CommandBuffer _cmd;
        private ImpostorTextureProcessor _textureProcessor;
        private RenderTexture[] _gBuffer;
        private RenderTexture[] _outBuffer;

        /// <summary>
        /// Creates a new atlas renderer with the specified rendering resources.
        /// </summary>
        public ImpostorAtlasRenderer(Material bakerMaterial, Renderer[] renderers, Mesh[] meshes, Material[][] materials)
        {
            _bakerMaterial = bakerMaterial;
            _renderers = renderers;
            _meshes = meshes;
            _materials = materials;
        }

        /// <summary>
        /// Bakes atlases (albedo and normal) from snapshot data.
        /// </summary>
        /// <param name="data">Impostor data containing snapshot information and resolution settings.</param>
        /// <returns>Tuple containing albedo and normal atlas RenderTextures.</returns>
        public (RenderTexture albedo, RenderTexture normal) BakeAtlases(ImpostorData data)
        {
            RenderTexture albedoAtlasRT = RenderTexture.GetTemporary(data.atlasResolution, data.atlasResolution, 0, GraphicsFormat.R8G8B8A8_SRGB);
            RenderTexture normalAtlasRT = RenderTexture.GetTemporary(data.atlasResolution, data.atlasResolution, 0, GraphicsFormat.R32G32B32A32_SFloat);

            RenderToAtlases(albedoAtlasRT, normalAtlasRT, data);

            return (albedoAtlasRT, normalAtlasRT);
        }

        /// <summary>
        /// Main rendering loop that processes all snapshots and fills atlas textures.
        /// </summary>
        private void RenderToAtlases(RenderTexture albedoAtlasRT, RenderTexture normalAtlasRT, ImpostorData data)
        {
            InitializeBuffers(data.frameResolution);

            Shader.EnableKeyword("_RENDER_PASS_ENABLED");

            RenderTargetIdentifier[] mrt = new RenderTargetIdentifier[4];
            for (int i = 0; i < mrt.Length; i++)
            {
                mrt[i] = _gBuffer[i];
            }

            RenderTexture diffuseRT = RenderTexture.GetTemporary(data.frameResolution, data.frameResolution, 0, GraphicsFormat.R8G8B8A8_SRGB);
            RenderTexture normalsDepthRT = RenderTexture.GetTemporary(data.frameResolution, data.frameResolution, 0, GraphicsFormat.R32G32B32A32_SFloat);

            for (int frameIndex = 0; frameIndex < data.snapshotsData.Length; frameIndex++)
            {
                RenderFrame(data, frameIndex, mrt, diffuseRT, normalsDepthRT);
                CopyFrameToAtlas(data, frameIndex, albedoAtlasRT, normalAtlasRT);

                Graphics.ExecuteCommandBuffer(_cmd);
                _cmd.Clear();
            }

            Shader.DisableKeyword("_RENDER_PASS_ENABLED");

            RenderBufferFactory.Release(diffuseRT, normalsDepthRT);
            ReleaseBuffers();
        }

        /// <summary>
        /// Initializes G-Buffer and output buffers for the bake session.
        /// </summary>
        private void InitializeBuffers(int frameResolution)
        {
            _cmd = new CommandBuffer { name = "BakeSnapShots" };
            _textureProcessor = new ImpostorTextureProcessor(_cmd, _bakerMaterial);

            _gBuffer = RenderBufferFactory.CreateGBuffer(frameResolution);
            _outBuffer = RenderBufferFactory.CreateOutputBuffers(frameResolution);
        }

        /// <summary>
        /// Releases all allocated buffers after bake session completes.
        /// </summary>
        private void ReleaseBuffers()
        {
            RenderBufferFactory.ReleaseBuffers(_outBuffer);
            RenderBufferFactory.ReleaseBuffers(_gBuffer);
            _cmd.Release();

            _cmd = null;
            _textureProcessor = null;
            _gBuffer = null;
            _outBuffer = null;
        }

        /// <summary>
        /// Renders a single frame: sets up camera, renders to G-Buffer, and processes output.
        /// </summary>
        private void RenderFrame(ImpostorData data, int frameIndex, RenderTargetIdentifier[] mrt, RenderTexture diffuseRT, RenderTexture normalsDepthRT)
        {
            SnapshotData currentSnapshot = data.snapshotsData[frameIndex];

            SetupCameraMatrices(data.boundsRadius, currentSnapshot);

            _cmd.ClearBuffers(_outBuffer);
            _cmd.SetAndClear(mrt, _gBuffer[(int)GBuffer.Depth]);
            _cmd.DrawMeshes(UnityShaderPass.GBuffer, _renderers, _meshes, _materials);

            Graphics.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();

            ProcessGBufferToOutput(diffuseRT, normalsDepthRT);
        }

        /// <summary>
        /// Sets up orthographic camera matrices for the given snapshot.
        /// </summary>
        private void SetupCameraMatrices(float boundsRadius, SnapshotData snapshot)
        {
            Matrix4x4 orthoP = Matrix4x4.Ortho(-boundsRadius, boundsRadius, -boundsRadius, boundsRadius, 0f, boundsRadius * 2);
            Matrix4x4 cameraV = Matrix4x4.Inverse(Matrix4x4.TRS(
                snapshot.position,
                Quaternion.LookRotation(snapshot.direction, Vector3.up),
                new Vector3(1, 1, -1)
            ));
            _cmd.SetViewProjectionMatrices(cameraV, orthoP);
        }

        /// <summary>
        /// Post-processes G-Buffer data: applies metallic correction, merges normals with depth, and dilates edges.
        /// </summary>
        private void ProcessGBufferToOutput(RenderTexture diffuseRT, RenderTexture normalsDepthRT)
        {
            // Copy emission channels
            _textureProcessor.CopyChannel(_gBuffer[(int)GBuffer.Emission], _outBuffer[(int)OutputBuffer.Dilate], new Vector4(1, 0, 0, 0));
            _textureProcessor.CopyChannel(_gBuffer[(int)GBuffer.Emission], _outBuffer[(int)OutputBuffer.Diffuse], new Vector4(0, 0, 0, 1));

            // Metallic correction and normal-depth merge
            _textureProcessor.MetallicCorrection(_gBuffer[(int)GBuffer.Albedo], _gBuffer[(int)GBuffer.Specular], diffuseRT);
            _textureProcessor.MergeNormalDepth(_gBuffer[(int)GBuffer.Normals], _gBuffer[(int)GBuffer.Depth], normalsDepthRT);

            // Dilation
            _textureProcessor.Dilate(_outBuffer[(int)OutputBuffer.Dilate], normalsDepthRT, _outBuffer[(int)OutputBuffer.NormalsDepth], new Vector4(1, 1, 1, 1), false);
            _textureProcessor.Dilate(_outBuffer[(int)OutputBuffer.Dilate], diffuseRT, _outBuffer[(int)OutputBuffer.Diffuse], new Vector4(1, 1, 1, 0), true);
        }

        /// <summary>
        /// Copies rendered frame from output buffer to the appropriate position in atlas textures.
        /// </summary>
        private void CopyFrameToAtlas(ImpostorData data, int frameIndex, RenderTexture albedoAtlasRT, RenderTexture normalAtlasRT)
        {
            int x = frameIndex % data.framesCount;
            int y = frameIndex / data.framesCount;
            x *= data.frameResolution;
            y *= data.frameResolution;

            _cmd.CopyTexture(
                _outBuffer[(int)OutputBuffer.Diffuse], 0, 0, 0, 0,
                data.frameResolution, data.frameResolution,
                albedoAtlasRT, 0, 0, x, y
            );
            _cmd.CopyTexture(
                _outBuffer[(int)OutputBuffer.NormalsDepth], 0, 0, 0, 0,
                data.frameResolution, data.frameResolution,
                normalAtlasRT, 0, 0, x, y
            );
        }
    }
}
