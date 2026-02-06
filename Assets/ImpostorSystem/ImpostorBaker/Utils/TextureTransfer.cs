using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace ImpostorPlugin.Utils
{
    /// <summary>
    /// Utilities for transferring texture data between GPU and CPU.
    /// </summary>
    public static class TextureTransfer
    {
        /// <summary>
        /// Synchronously copies data from GPU RenderTexture to Color32 array on CPU.
        /// </summary>
        public static Color32[] ReadPixels(RenderTexture source, int size)
        {
            var previousActive = RenderTexture.active;
            RenderTexture.active = source;

            var tempTexture = new Texture2D(size, size, TextureFormat.R8, false, true);
            tempTexture.ReadPixels(new Rect(0, 0, size, size), 0, 0, false);
            tempTexture.Apply(false, false);

            Color32[] pixels = tempTexture.GetPixels32();
            
            RenderTexture.active = previousActive;
            UnityEngine.Object.DestroyImmediate(tempTexture);

            return pixels;
        }

        /// <summary>
        /// Asynchronously copies data from GPU RenderTexture to Color32 array on CPU.
        /// </summary>
        public static void ReadPixelsAsync(RenderTexture source, int size, Action<Color32[]> onComplete)
        {
            AsyncGPUReadback.Request(source, 0, TextureFormat.R8, (AsyncGPUReadbackRequest request) =>
            {
                if (request.hasError)
                {
                    Debug.LogError("AsyncGPUReadback error");
                    onComplete?.Invoke(null);
                    return;
                }

                var data = request.GetData<Color32>();
                Color32[] pixels = data.ToArray();

                onComplete?.Invoke(pixels);
            });
        }
    }
}
