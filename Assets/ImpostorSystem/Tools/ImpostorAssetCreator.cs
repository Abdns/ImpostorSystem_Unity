#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ImpostorPlugin
{
    /// <summary>
    /// Creates impostor assets: textures, material, mesh.
    /// Works only in Editor.
    /// </summary>
    public static class ImpostorAssetCreator
    {
        private const string ImpostorShaderName = "Impostor/ImpostorURP";

        /// <summary>
        /// Creates all necessary assets for the impostor.
        /// </summary>
        public static GameObject CreateAssets(ImpostorData data, GameObject sourceObject, RenderTexture albedoAtlas, RenderTexture normalAtlas)
        {
            string savePath = PromptSavePath(sourceObject.name);
            if (string.IsNullOrEmpty(savePath)) return null;

            string directory = Path.GetDirectoryName(savePath);

            GameObject impostorObject = new GameObject($"{sourceObject.name}_Impostor");
            impostorObject.transform.position = sourceObject.transform.position;
            impostorObject.transform.rotation = sourceObject.transform.rotation;
            impostorObject.transform.localScale = sourceObject.transform.localScale;

            SetupMesh(impostorObject, data);

            string albedoPath = SaveTexture(albedoAtlas, directory, $"{sourceObject.name}_ImpostorAlbedoMap");
            string normalPath = SaveTexture(normalAtlas, directory, $"{sourceObject.name}_ImpostorNormalMap");

            ConfigureTextureImport(albedoPath, data.atlasResolution);
            ConfigureTextureImport(normalPath, data.atlasResolution);

            var material = CreateMaterial(directory, sourceObject.name, data, albedoPath, normalPath);

            var meshRenderer = impostorObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.sharedMaterial = material;
            }

            AssetDatabase.SaveAssets();

            return impostorObject;
        }

        private static string PromptSavePath(string defaultName)
        {
            return EditorUtility.SaveFilePanelInProject(
                "Save Impostor Textures",
                defaultName,
                "",
                "Select textures save location"
            );
        }

        private static void SetupMesh(GameObject gameObject, ImpostorData data)
        {
            if (!gameObject.TryGetComponent<MeshRenderer>(out _))
            {
                gameObject.AddComponent<MeshRenderer>();
            }

            if (!gameObject.TryGetComponent<MeshFilter>(out var meshFilter))
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }

            meshFilter.sharedMesh = ImpostorMeshFactory.Create(data.boundsOffset, data.boundsRadius);
        }

        private static string SaveTexture(RenderTexture source, string directory, string textureName)
        {
            var texture = new Texture2D(source.width, source.height, TextureFormat.ARGB32, true, true);
            texture.name = textureName;

            Graphics.SetRenderTarget(source);
            texture.ReadPixels(new Rect(0f, 0f, source.width, source.height), 0, 0);

            string fullPath = $"{directory}/{textureName}.png";
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(fullPath, bytes);

            Object.DestroyImmediate(texture);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return fullPath;
        }

        private static void ConfigureTextureImport(string texturePath, int maxSize)
        {
            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Default;
            importer.maxTextureSize = maxSize;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.sRGBTexture = false;
            importer.SaveAndReimport();
        }

        private static Material CreateMaterial(string directory, string objectName, ImpostorData data, string albedoPath, string normalPath)
        {
            string materialName = $"{objectName}_ImpostorMaterial";
            string materialPath = $"{directory}/{materialName}.asset";

            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            if (material == null)
            {
                var shader = Shader.Find(ImpostorShaderName);
                if (shader == null)
                {
                    Debug.LogError($"[ImpostorAssetCreator] Shader '{ImpostorShaderName}' not found!");
                    return null;
                }

                material = new Material(shader) { name = materialName };
                AssetDatabase.CreateAsset(material, materialPath);
            }

            var albedoTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
            var normalTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);

            material.SetTexture("_ImpostorAlbedoMap", albedoTexture);
            material.SetTexture("_ImpostorNormalMap", normalTexture);
            material.SetFloat("_ImpostorIsHalfSphere", data.isHalfSphere ? 1 : 0);
            material.SetFloat("_ImpostorFrames", data.framesCount);
            material.SetFloat("_ImpostorSize", data.boundsRadius);
            material.SetFloat("_ShadowSize", data.boundsRadius);
            material.SetFloat("_DepthSize", data.boundsRadius * 2.0f);
            material.SetVector("_ImpostorOffset", data.boundsOffset);

            EditorUtility.SetDirty(material);

            return material;
        }
    }
}
#endif
