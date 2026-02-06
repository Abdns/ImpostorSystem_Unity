using UnityEngine;
using UnityEngine.Rendering;

namespace ImpostorPlugin.Utils
{
    /// <summary>
    /// Processes textures during impostor baking.
    /// Performs channel copy, dilation, metallic correction,
    /// and normal-depth merge operations via Blit commands.
    /// </summary>
    public class ImpostorTextureProcessor
    {
        private readonly CommandBuffer _cmd;
        private readonly Material _bakerMaterial;

        public ImpostorTextureProcessor(CommandBuffer cmd, Material bakerMaterial)
        {
            _cmd = cmd;
            _bakerMaterial = bakerMaterial;
        }

        /// <summary>
        /// Copies the specified channel from source to destination.
        /// </summary>
        public void CopyChannel(RenderTexture source, RenderTexture dest, Vector4 channel)
        {
            _bakerMaterial.SetTexture("_AlphaMap", source);
            _bakerMaterial.SetVector("_Channels", channel);
            _cmd.Blit(source, dest, _bakerMaterial, (int)ImpostorBakerPass.AlphaCopy);

            Graphics.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();
        }

        /// <summary>
        /// Performs texture dilation (edge expansion).
        /// </summary>
        public void Dilate(RenderTexture dilateMask, RenderTexture source, RenderTexture dest, Vector4 channel, bool useMaskAsAlpha)
        {
            _bakerMaterial.SetTexture("_DilateMask", dilateMask);
            _bakerMaterial.SetTexture("_MainTex", source);
            _bakerMaterial.SetVector("_Channels", channel);
            _bakerMaterial.SetFloat("_UseMaskAsAlpha", useMaskAsAlpha ? 1 : 0);
            _cmd.Blit(source, dest, _bakerMaterial, (int)ImpostorBakerPass.Dilate);

            Graphics.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();
        }

        /// <summary>
        /// Corrects metallic values based on G-Buffer data.
        /// </summary>
        public void MetallicCorrection(RenderTexture albedoBuffer, RenderTexture specularBuffer, RenderTexture dest)
        {
            _bakerMaterial.SetTexture("_MainTex", albedoBuffer);
            _bakerMaterial.SetTexture("_GBuffer1", specularBuffer);
            _cmd.Blit(albedoBuffer, dest, _bakerMaterial, (int)ImpostorBakerPass.Correction);

            Graphics.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();
        }

        /// <summary>
        /// Merges normal map and depth map into a single texture.
        /// </summary>
        public void MergeNormalDepth(RenderTexture normalMap, RenderTexture depthMap, RenderTexture target)
        {
            _bakerMaterial.SetTexture("_NormalMap", normalMap);
            _bakerMaterial.SetTexture("_DepthMap", depthMap);
            _cmd.Blit(null, target, _bakerMaterial, (int)ImpostorBakerPass.MergeNormalsDepth);

            Graphics.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();
        }
    }
}
