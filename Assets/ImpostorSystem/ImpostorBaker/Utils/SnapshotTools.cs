using UnityEngine;

namespace ImpostorPlugin.Utils
{
    /// <summary>
    /// Utilities for creating and validating impostor snapshot data.
    /// Uses octahedral mapping for camera direction distribution.
    /// </summary>
    public static class SnapshotTools
    {
        /// <summary>
        /// Creates an array of snapshot data for the given configuration.
        /// </summary>
        /// <param name="framesCount">Number of frames per axis (total = framesCount²)</param>
        /// <param name="boundsRadius">View sphere radius</param>
        /// <param name="origin">Object center</param>
        /// <param name="isHemiSphere">True for hemisphere, false for full sphere</param>
        public static SnapshotData[] BuildSnapshots(int framesCount, float boundsRadius, Vector3 origin, bool isHemiSphere = true)
        {
            SnapshotData[] snapshots = new SnapshotData[framesCount * framesCount];

            float framesMinusOne = framesCount - 1;

            int index = 0;
            for (int y = 0; y < framesCount; y++)
            {
                for (int x = 0; x < framesCount; x++)
                {
                    Vector2 normalizedCoord = new Vector2(
                        x / framesMinusOne * 2f - 1f,
                        y / framesMinusOne * 2f - 1f
                    );

                    Vector3 direction = isHemiSphere
                        ? DecodeOctahedralHemiSphere(normalizedCoord)
                        : DecodeOctahedralSphere(normalizedCoord);

                    direction = direction.normalized;

                    snapshots[index].position = origin + direction * boundsRadius;
                    snapshots[index].direction = -direction;
                    index++;
                }
            }

            return snapshots;
        }

        /// <summary>
        /// Validates and corrects frame count (must be even and >= 2).
        /// </summary>
        public static int GetValidFrameCount(int framesCount)
        {
            if (framesCount % 2 != 0)
            {
                framesCount--;
            }
            return Mathf.Max(2, framesCount);
        }

        /// <summary>
        /// Decodes hemispherical octahedral mapping coordinates to direction.
        /// </summary>
        private static Vector3 DecodeOctahedralHemiSphere(Vector2 coord)
        {
            coord = new Vector2(coord.x + coord.y, coord.x - coord.y) * 0.5f;
            
            Vector3 direction = new Vector3(
                coord.x,
                1.0f - Vector2.Dot(Vector2.one, new Vector2(Mathf.Abs(coord.x), Mathf.Abs(coord.y))),
                coord.y
            );
            
            return Vector3.Normalize(direction);
        }

        /// <summary>
        /// Decodes spherical octahedral mapping coordinates to direction.
        /// </summary>
        private static Vector3 DecodeOctahedralSphere(Vector2 uv)
        {
            Vector3 direction = new Vector3(
                uv.x,
                1f - Mathf.Abs(uv.x) - Mathf.Abs(uv.y),
                uv.y
            );

            float t = Mathf.Clamp01(-direction.y);

            direction.x += direction.x >= 0f ? -t : t;
            direction.z += direction.z >= 0f ? -t : t;

            return direction;
        }
    }
}
