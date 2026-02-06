using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace ImpostorPlugin
{
    /// <summary>
    /// Factory for creating RenderTexture buffers.
    /// Responsible for creating G-Buffer, Output buffers and Depth buffers.
    /// </summary>
    public static class RenderBufferFactory
    {
        /// <summary>
        /// Creates G-Buffer texture array for deferred rendering.
        /// </summary>
        public static RenderTexture[] CreateGBuffer(int textureSize)
        {
            RenderTexture[] gBuffers = new RenderTexture[5];

            // Albedo (RGB) MaterialFlags (A) [A => miscFlags(7)]
            gBuffers[(int)GBuffer.Albedo] = RenderTexture.GetTemporary(textureSize, textureSize, 0, GraphicsFormat.R8G8B8A8_SRGB);

            // Specular (RGB) Occlusion (A) [R => Metallic : RGB => Specular]
            gBuffers[(int)GBuffer.Specular] = RenderTexture.GetTemporary(textureSize, textureSize, 0, GraphicsFormat.R8G8B8A8_UNorm);

            // Normals (RGB) Smoothness (A)
            gBuffers[(int)GBuffer.Normals] = RenderTexture.GetTemporary(textureSize, textureSize, 0, GraphicsFormat.R8G8B8A8_SNorm);

            // Emission (RGB) Alpha (A)
            gBuffers[(int)GBuffer.Emission] = RenderTexture.GetTemporary(textureSize, textureSize, 0, GraphicsFormat.R16G16B16A16_SFloat);

            // Depth
            gBuffers[(int)GBuffer.Depth] = RenderTexture.GetTemporary(textureSize, textureSize, 24, RenderTextureFormat.Depth);

            return gBuffers;
        }

        /// <summary>
        /// Creates output buffer array for post-processing.
        /// </summary>
        public static RenderTexture[] CreateOutputBuffers(int textureSize)
        {
            RenderTexture[] outBuffers = new RenderTexture[4];

            outBuffers[(int)OutputBuffer.Diffuse] = RenderTexture.GetTemporary(textureSize, textureSize, 0, GraphicsFormat.R8G8B8A8_SRGB);
            outBuffers[(int)OutputBuffer.NormalsDepth] = RenderTexture.GetTemporary(textureSize, textureSize, 0, GraphicsFormat.R32G32B32A32_SFloat);
            outBuffers[(int)OutputBuffer.Depth] = RenderTexture.GetTemporary(textureSize, textureSize, 0, GraphicsFormat.R16_UNorm);
            outBuffers[(int)OutputBuffer.Dilate] = RenderTexture.GetTemporary(textureSize, textureSize, 0, GraphicsFormat.R8G8B8A8_SRGB);

            return outBuffers;
        }

        /// <summary>
        /// Releases temporary RenderTexture array.
        /// </summary>
        public static void ReleaseBuffers(RenderTexture[] buffers)
        {
            if (buffers == null) return;
            
            for (int i = 0; i < buffers.Length; i++)
            {
                if (buffers[i] != null)
                {
                    RenderTexture.ReleaseTemporary(buffers[i]);
                }
            }
        }

        /// <summary>
        /// Releases one or more temporary RenderTextures.
        /// </summary>
        public static void Release(params RenderTexture[] textures)
        {
            if (textures == null) return;
            
            for (int i = 0; i < textures.Length; i++)
            {
                if (textures[i] != null)
                {
                    RenderTexture.ReleaseTemporary(textures[i]);
                }
            }
        }
    }
}
