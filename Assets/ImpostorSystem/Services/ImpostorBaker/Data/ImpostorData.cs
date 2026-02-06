using UnityEngine;

namespace ImpostorPlugin
{
    /// <summary>
    /// Contains all data required for impostor baking.
    /// </summary>
    public class ImpostorData
    {
        /// <summary>
        /// Number of frames per atlas axis.
        /// </summary>
        public int framesCount;

        /// <summary>
        /// Atlas resolution in pixels.
        /// </summary>
        public int atlasResolution;

        /// <summary>
        /// Single frame resolution (atlasResolution / framesCount).
        /// </summary>
        public int frameResolution;

        /// <summary>
        /// Padding around frame edges in pixels.
        /// </summary>
        public float framePadding;

        /// <summary>
        /// True for hemisphere, false for full sphere.
        /// </summary>
        public bool isHalfSphere;

        /// <summary>
        /// Object bounds (calculated automatically).
        /// </summary>
        public Bounds bounds;

        /// <summary>
        /// Bounds center offset.
        /// </summary>
        public Vector3 boundsOffset;

        /// <summary>
        /// Radius of the bounding sphere.
        /// </summary>
        public float boundsRadius;

        /// <summary>
        /// Array of snapshot data for each atlas frame.
        /// </summary>
        public SnapshotData[] snapshotsData;
    }
}
