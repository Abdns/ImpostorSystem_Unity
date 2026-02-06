using UnityEngine;

namespace ImpostorPlugin
{
    /// <summary>
    /// Factory for creating impostor mesh.
    /// </summary>
    public static class ImpostorMeshFactory
    {
        /// <summary>
        /// Creates an impostor mesh with the specified bounds parameters.
        /// </summary>
        public static Mesh Create(Vector3 boundsOffset, float boundsRadius)
        {
            var vertices = new[]
            {
                new Vector3(0f, 0.0f, 0f),   
                new Vector3(-0.5f, 0.0f, -0.5f), 
                new Vector3(0.5f, 0.0f, -0.5f),  
                new Vector3(0.5f, 0.0f, 0.5f),   
                new Vector3(-0.5f, 0.0f, 0.5f)   
            };

            var triangles = new[]
            {
                2, 1, 0,  
                3, 2, 0,  
                4, 3, 0,  
                1, 4, 0  
            };

            var uvs = new[]
            {
                new Vector2(0.5f, 0.5f),
                new Vector2(0.0f, 0.0f), 
                new Vector2(1.0f, 0.0f), 
                new Vector2(1.0f, 1.0f), 
                new Vector2(0.0f, 1.0f)  
            };

            var normals = new[]
            {
                Vector3.up,
                Vector3.up,
                Vector3.up,
                Vector3.up,
                Vector3.up
            };

            var mesh = new Mesh
            {
                name = "ImpostorMesh",
                vertices = vertices,
                uv = uvs,
                normals = normals,
                tangents = new Vector4[5]
            };

            mesh.SetTriangles(triangles, 0);
            mesh.bounds = new Bounds(Vector3.zero + boundsOffset, Vector3.one * boundsRadius * 2f);
            mesh.RecalculateTangents();

            return mesh;
        }
    }
}
