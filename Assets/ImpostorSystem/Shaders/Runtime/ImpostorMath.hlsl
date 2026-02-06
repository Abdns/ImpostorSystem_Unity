#ifndef IMPOSTOR_MATH_INCLUDED
#define IMPOSTOR_MATH_INCLUDED

struct Ray
{
    float3 Origin;
    float3 Direction;
};

float3 OctaHemiDec(float2 coord)
{
    coord = float2(coord.x + coord.y, coord.x - coord.y) * 0.5;
    float3 v = float3(coord.x, 1.0 - dot(1.0, abs(coord.xy)), coord.y);
    return normalize(v);
}

float3 OctaSphereDec(float2 coord)
{
    float3 v = float3(coord.x, 1.0 - dot(1.0, abs(coord.xy)), coord.y);
    if (v.y < 0.0)
    {
        float2 flip = (v.xz >= 0.0) ? float2(1, 1) : float2(-1, -1);
        v.xz = (1.0 - abs(v.zx)) * flip;
    }
    return normalize(v);
}

float3 GridToVector(float2 coord)
{
    #if defined(IMPOSTOR_USE_HEMI)
        return OctaHemiDec(coord);
    #else
        return OctaSphereDec(coord);
    #endif
}

float2 VecToHemiOct(float3 v)
{
    v = normalize(v);
    v.xz /= dot(1.0, abs(v));
    return float2(v.x + v.z, v.x - v.z);
}

float2 VecToSphereOct(float3 v)
{
    v = normalize(v);
    v.xz /= dot(1.0, abs(v));
    if (v.y <= 0.0)
    {
        float2 flip = (v.xz >= 0.0) ? float2(1, 1) : float2(-1, -1);
        v.xz = (1.0 - abs(v.zx)) * flip;
    }
    return v.xz;
}

float2 VectorToGrid(float3 v)
{
    #if defined(IMPOSTOR_USE_HEMI)
        v.y = max(0.001, v.y);
        return VecToHemiOct(v);
    #else
        return VecToSphereOct(v);
    #endif
}

float4 TriangleInterpolate(float2 uv)
{
    uv = frac(uv);
    float2 omuv = 1.0 - uv;

    float4 res;
    res.x = min(omuv.x, omuv.y);
    res.y = abs(dot(uv, float2(1.0, -1.0)));
    res.z = min(uv.x, uv.y);
    res.w = saturate(ceil(uv.x - uv.y));
    return res;
}

float3 FrameXYToRay(float2 frame, float2 frameCountMinusOne)
{
    float2 f = frame / frameCountMinusOne; 
    f = (f - 0.5) * 2.0;
    return GridToVector(f);
}

float3 ITBasis(float3 v, float3 bx, float3 by, float3 bz)
{
    return float3(dot(bx, v), dot(by, v), dot(bz, v));
}

float3 FrameTransform(float3 projRay, float3 frameRay, out float3 worldX, out float3 worldZ)
{
    // stable basis
    worldX = normalize(float3(-frameRay.z, 0, frameRay.x));
    worldZ = normalize(cross(worldX, frameRay));

    projRay *= -1.0;
    float3 local = normalize(ITBasis(projRay, worldX, frameRay, worldZ));
    return local; 
}

float2 VirtualPlaneUV(float3 planeNormal, float3 planeX, float3 planeZ, float3 center, float2 uvScale, Ray rayLocal)
{
    float normalDotOrigin = dot(planeNormal, rayLocal.Origin);
    float normalDotCenter = dot(planeNormal, center);
    float normalDotRay = dot(planeNormal, rayLocal.Direction);

    float planeDistance = (normalDotOrigin - normalDotCenter) * -1.0;
    float t = planeDistance / normalDotRay;

    float2 uv = 0;
    if (t > 0.0)
    {
        float3 hit = ((rayLocal.Direction * t) + rayLocal.Origin) - center;
        float dx = dot(planeX, hit);
        float dz = dot(planeZ, hit);
        uv = float2(dx, dz);
        uv /= uvScale;
        uv += 0.5;
    }
    return uv;
}

float3 BilboardProjection(float3 pivotToCameraDirection, float2 uvExpansion, float framesCount, float2 size, float2 texcoord, out float4 tangent)
{
    float3 upVector = float3(0, 1, 0);
    
    pivotToCameraDirection = normalize(pivotToCameraDirection);
    float3 objectHorizontalVector = normalize(cross(pivotToCameraDirection, upVector));
    float3 objectVerticalVector = cross(objectHorizontalVector, pivotToCameraDirection);
   
    tangent = float4(objectHorizontalVector, 1);

    float2 uv = (texcoord - 0.5) * 2.0;

    float2 halfSize = size * 0.5;
    float3 bilbordVertPos = (objectHorizontalVector * (uv.x * halfSize.x)) + (objectVerticalVector * (uv.y * halfSize.y));
      
    return bilbordVertPos;
}

#endif
