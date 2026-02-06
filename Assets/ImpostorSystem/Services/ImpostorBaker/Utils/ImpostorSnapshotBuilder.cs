using UnityEngine;
using UnityEngine.Rendering;

namespace ImpostorPlugin.Utils
{
    /// <summary>
    /// Responsible for building and updating impostor snapshot data.
    /// Coordinates bounds calculation and creates SnapshotData array.
    /// </summary>
    public class ImpostorSnapshotBuilder
    {
        /// <summary>
        /// Initializes snapshot data: calculates bounds, creates initial snapshots array.
        /// </summary>
        public void InitializeSnapshotData(Transform transform, ImpostorData data, Renderer[] renderers, Mesh[] meshes)
        {
            data.frameResolution = data.atlasResolution / data.framesCount;

            // Build initial snapshots
            data.snapshotsData = SnapshotTools.BuildSnapshots(
                data.framesCount,
                data.boundsRadius,
                data.boundsOffset,
                data.isHalfSphere
            );

            // Calculate bounds from meshes
            data.bounds = ImpostorBoundsCalculator.CalculateMeshBounds(transform, renderers, meshes);

            // Get radius and offset
            (data.boundsRadius, data.boundsOffset) = ImpostorBoundsCalculator.CalculateRadiusOffset(data.bounds);
        }

        /// <summary>
        /// Optimizes bounds based on pixel mask and rebuilds snapshots.
        /// </summary>
        public void OptimizeAndRebuild(ImpostorData data, Color32[] pixelMask)
        {
            // Trim bounds by mask
            data.boundsRadius = ImpostorBoundsCalculator.TrimBoundsByPixelMask(
                pixelMask,
                data.atlasResolution,
                data.boundsRadius,
                data.framePadding
            );

            // Rebuild snapshots with new radius
            data.snapshotsData = SnapshotTools.BuildSnapshots(
                data.framesCount,
                data.boundsRadius,
                data.boundsOffset,
                data.isHalfSphere
            );
        }

        /// <summary>
        /// Renders MinMax mask for determining occupied pixels.
        /// Returns pixel array for subsequent bounds optimization.
        /// </summary>
        public Color32[] RenderMinMaxMask(ImpostorData data, Material bakerMaterial, Renderer[] renderers, Mesh[] meshes)
        {
            RenderTexture minMaxTileRT = RenderTexture.GetTemporary(
                data.atlasResolution,
                data.atlasResolution,
                0,
                RenderTextureFormat.R8,
                RenderTextureReadWrite.Linear
            );

            RenderMinMaxToTexture(minMaxTileRT, data, bakerMaterial, renderers, meshes);

            Color32[] pixels = TextureTransfer.ReadPixels(minMaxTileRT, data.atlasResolution);

            RenderTexture.ReleaseTemporary(minMaxTileRT);

            return pixels;
        }

        private void RenderMinMaxToTexture(RenderTexture target, ImpostorData data, Material bakerMaterial, Renderer[] renderers, Mesh[] meshes)
        {
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "RenderMinMaxMask";

            cmd.SetAndClear(target);

            for (var i = 0; i < data.snapshotsData.Length; i++)
            {
                Matrix4x4 orthoP = Matrix4x4.Ortho(
                    -data.boundsRadius, data.boundsRadius,
                    -data.boundsRadius, data.boundsRadius,
                    0f, data.boundsRadius * 2
                );

                Matrix4x4 cameraV = Matrix4x4.Inverse(Matrix4x4.TRS(
                    data.snapshotsData[i].position,
                    Quaternion.LookRotation(data.snapshotsData[i].direction, Vector3.up),
                    new Vector3(1, 1, -1)
                ));

                cmd.SetViewProjectionMatrices(cameraV, orthoP);

                cmd.DrawMeshes(bakerMaterial, ImpostorBakerPass.MinMax, renderers, meshes);
            }

            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Release();
        }
    }
}
