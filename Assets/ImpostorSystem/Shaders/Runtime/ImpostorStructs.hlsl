#ifndef IMPOSTOR_STRUCTS_INCLUDED
#define IMPOSTOR_STRUCTS_INCLUDED

struct ImpostorConfig
{
    float frames;
    float size;
    float3 offset;
    float borderClamp;
    float parallax;
    float textureBias;
    float depthSize;

    float alphaClip;
    float metallic;
    float smoothness;

    float shadowView;
    float shadowBias;
    float forwardBias;

    float4 albedoTexelSize;
};


struct ImpostorData
{
    float2 uv; 
    float2 grid; 

    float4 frame0; 
    float4 frame1;
    float4 frame2;
};


struct ImpostorOutput
{
    half3 Albedo;
    half3 Normal;
    half Smoothness;
    half Metallic;
    half3 Specular;
    half Occlusion;
    half3 Emission;
    half3 WorldNormal;
    half Alpha;
};

#endif
