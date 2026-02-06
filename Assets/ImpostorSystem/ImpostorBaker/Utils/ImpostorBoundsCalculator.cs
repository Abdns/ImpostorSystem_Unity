using UnityEngine;

namespace ImpostorPlugin.Utils
{
    /// <summary>
    /// Utilities for calculating bounds for the impostor.
    /// Calculates bounds from mesh vertices, radius and offset,
    /// and optimizes bounds using pixel mask.
    /// </summary>
    public static class ImpostorBoundsCalculator
    {
        /// <summary>
        /// Calculates bounds enclosing all mesh vertices relative to root transform.
        /// </summary>
        /// <param name="rootTransform">Root transform of the object.</param>
        /// <param name="renderers">Array of renderers to calculate bounds from.</param>
        /// <param name="meshes">Array of meshes corresponding to renderers.</param>
        /// <returns>Bounds enclosing all mesh vertices.</returns>
        public static Bounds CalculateMeshBounds(Transform rootTransform, Renderer[] renderers, Mesh[] meshes)
        {
            Bounds bounds = new Bounds(renderers[0].transform.position, Vector3.zero);

            for (int i = 0; i < renderers.Length; i++)
            {
                Vector3[] verts = meshes[i].vertices;
                for (int v = 0; v < verts.Length; v++)
                {
                    Vector3 meshWorldVert = renderers[i].localToWorldMatrix.MultiplyPoint3x4(verts[v]);
                    Vector3 meshLocalToRoot = rootTransform.worldToLocalMatrix.MultiplyPoint3x4(meshWorldVert);
                    Vector3 worldVert = rootTransform.localToWorldMatrix.MultiplyPoint3x4(meshLocalToRoot);

                    bounds.Encapsulate(worldVert);
                }
            }
            return bounds;
        }

        /// <summary>
        /// Calculates radius and offset (center) from bounds.
        /// </summary>
        /// <param name="bounds">Bounds to calculate from.</param>
        /// <returns>Tuple containing radius and offset.</returns>
        public static (float radius, Vector3 offset) CalculateRadiusOffset(Bounds bounds)
        {
            float radius = Vector3.Distance(bounds.min, bounds.max) * 0.5f;
            Vector3 offset = bounds.center;

            return (radius, offset);
        }

        /// <summary>
        /// Optimizes bounds radius based on pixel mask.
        /// Finds min/max occupied pixels and recalculates radius.
        /// </summary>
        /// <param name="pixels">Pixel mask array.</param>
        /// <param name="atlasResolution">Atlas resolution in pixels.</param>
        /// <param name="boundsRadius">Current bounds radius.</param>
        /// <param name="framePadding">Frame padding in pixels.</param>
        /// <returns>Optimized bounds radius.</returns>
        public static float TrimBoundsByPixelMask(Color32[] pixels, int atlasResolution, float boundsRadius, float framePadding)
        {
            // Start with max/min values
            Vector2 min = Vector2.one * atlasResolution;
            Vector2 max = Vector2.zero;

            // Iterate pixels and find min/max
            for (int c = 0; c < pixels.Length; c++)
            {
                if (pixels[c].r != 0x00)
                {
                    Vector2 texPos = Get2DIndex(c, atlasResolution);
                    min.x = Mathf.Min(min.x, texPos.x);
                    min.y = Mathf.Min(min.y, texPos.y);
                    max.x = Mathf.Max(max.x, texPos.x);
                    max.y = Mathf.Max(max.y, texPos.y);
                }
            }

            // Add padding
            min -= Vector2.one * framePadding;
            max += Vector2.one * framePadding;

            // Recalculate radius
            Vector2 len = new Vector2(max.x - min.x, max.y - min.y);
            float maxR = Mathf.Max(len.x, len.y);
            float ratio = maxR / atlasResolution; // assuming square

            return boundsRadius * ratio;
        }

        /// <summary>
        /// Converts linear index to 2D coordinates.
        /// </summary>
        /// <param name="index">Linear index.</param>
        /// <param name="rowCapacity">Number of elements per row.</param>
        /// <returns>2D coordinates.</returns>
        private static Vector2 Get2DIndex(int index, int rowCapacity)
        {
            float x = index % rowCapacity;
            float y = (index - x) / rowCapacity;

            return new Vector2(x, y);
        }
    }
}
