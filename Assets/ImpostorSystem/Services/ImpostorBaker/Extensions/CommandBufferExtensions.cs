using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace ImpostorPlugin
{
    /// <summary>
    /// Extensions for CommandBuffer.
    /// Provides methods for working with render targets and drawing meshes.
    /// </summary>
    public static class CommandBufferExtensions
    {
        /// <summary>
        /// Clears RenderTexture buffer array.
        /// </summary>
        public static void ClearBuffers(this CommandBuffer cmd, RenderTexture[] buffers)
        {
            for (int i = 0; i < buffers.Length; i++)
            {
                cmd.SetRenderTarget(buffers[i]);
                cmd.ClearRenderTarget(true, true, Color.clear, 1);
            }
        }

        /// <summary>
        /// Sets and clears a single render target.
        /// </summary>
        public static void SetAndClear(this CommandBuffer cmd, RenderTargetIdentifier target)
        {
            cmd.SetRenderTarget(target);
            cmd.ClearRenderTarget(true, true, Color.clear, 1);
        }

        /// <summary>
        /// Sets and clears MRT (Multiple Render Targets).
        /// </summary>
        public static void SetAndClear(this CommandBuffer cmd, RenderTargetIdentifier[] colors, RenderTargetIdentifier depth)
        {
            cmd.SetRenderTarget(colors, depth);
            cmd.ClearRenderTarget(true, true, Color.clear, 1);
        }

        /// <summary>
        /// Draws meshes with custom material (for baking passes).
        /// </summary>
        public static void DrawMeshes(
            this CommandBuffer cmd, 
            Material material, 
            ImpostorBakerPass pass, 
            Renderer[] renderers, 
            Mesh[] meshes)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                for (int j = 0; j < meshes[i].subMeshCount; j++)
                {
                    cmd.DrawMesh(meshes[i], renderers[i].localToWorldMatrix, material, j, (int)pass);
                }
            }
        }

        /// <summary>
        /// Draws meshes with original materials (for G-Buffer pass).
        /// </summary>
        public static void DrawMeshes(
            this CommandBuffer cmd, 
            UnityShaderPass pass, 
            Renderer[] renderers, 
            Mesh[] meshes, 
            Material[][] materials)
        {
            string passName = Enum.GetName(typeof(UnityShaderPass), pass);

            for (int i = 0; i < renderers.Length; i++)
            {
                for (int j = 0; j < meshes[i].subMeshCount; j++)
                {
                    if (materials[i] == null || j >= materials[i].Length) continue;

                    int passIndex = materials[i][j].FindPass(passName);

                    if (passIndex == -1)
                        continue;

                    cmd.DrawRenderer(renderers[i], materials[i][j], j, passIndex);
                }
            }
        }
    }
}
