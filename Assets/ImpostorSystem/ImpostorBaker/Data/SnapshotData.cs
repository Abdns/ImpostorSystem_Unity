using UnityEngine;

namespace ImpostorPlugin
{
    /// <summary>
    /// Single impostor snapshot data.
    /// Contains virtual camera position and direction.
    /// </summary>
    public struct SnapshotData
    {
        /// <summary>
        /// Virtual camera position in world coordinates.
        /// </summary>
        public Vector3 position;

        /// <summary>
        /// Camera look direction (from camera to object).
        /// </summary>
        public Vector3 direction;
    }
}
