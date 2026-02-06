namespace ImpostorPlugin
{
    /// <summary>
    /// G-Buffer texture indices.
    /// </summary>
    public enum GBuffer : byte
    {
        Albedo = 0,
        Specular,
        Normals,
        Emission,
        Depth
    }

    /// <summary>
    /// Output buffer indices.
    /// </summary>
    public enum OutputBuffer : byte
    {
        Diffuse = 0,
        NormalsDepth,
        Depth,
        Dilate
    }

    /// <summary>
    /// Unity standard shader pass indices.
    /// </summary>
    public enum UnityShaderPass : byte
    {
        ForwardLit = 0,
        ShadowCaster,
        GBuffer,
        DepthOnly,
        DepthNormals,
        Meta,
    }

    /// <summary>
    /// ImpostorBaker shader pass indices.
    /// </summary>
    public enum ImpostorBakerPass : byte
    {
        MinMax = 0,
        AlphaCopy,
        MergeNormalsDepth,
        Correction,
        Dilate,
    }
}
