#ifndef IMPOSTOR_ALGO_INCLUDED
    #define IMPOSTOR_ALGO_INCLUDED

    #include "ImpostorMath.hlsl"
    #include "ImpostorStructs.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

    #define Unity_ObjectToWorld GetObjectToWorldMatrix()
    #define Unity_WorldToObject GetWorldToObjectMatrix()

    inline void ImpostorVertex(inout ImpostorData imp, in ImpostorConfig cfg, inout float3 positionOS, out float3 normalOS, out float4 tangentOS, out float4 viewPosVS)
    {
        float2 texcoord = imp.uv;
    
        float framesMinusOne = cfg.frames - 1;
        float parallaxScale = -cfg.parallax;
   
        float3 worldCameraPos;

        #if defined(UNITY_PASS_SHADOWCASTER)
            float3 worldOrigin = 0;
            float4 perspective = float4(0, 0, 0, 1);

             if (UNITY_MATRIX_P[3][3] == 1) // Orthographic
             {
                // Используем большое расстояние для ортографической проекции
                float orthoDistance = 5000;
                perspective = float4(0, 0, orthoDistance, 0);
                worldOrigin = Unity_ObjectToWorld._m03_m13_m23;
             }
    
             worldCameraPos = worldOrigin + mul(UNITY_MATRIX_I_V, perspective).xyz ;
        #else
            if (UNITY_MATRIX_P[3][3] == 1)
            {
                worldCameraPos = Unity_ObjectToWorld._m03_m13_m23 + UNITY_MATRIX_I_V._m02_m12_m22 * 5000;
            }
            else
            {
                worldCameraPos = GetCameraRelativePositionWS(_WorldSpaceCameraPos);
            }
        #endif

        // Camera in object spaces
        float3 objectSpaceCameraPos = mul(Unity_WorldToObject, float4(worldCameraPos.xyz, 1)).xyz;
        float3 objectCameraDirection = normalize(objectSpaceCameraPos);
        
        float cameraDistance = length(objectSpaceCameraPos);

        float normalizedDistance = cameraDistance / max(cfg.size, 0.001);
        float distanceFade = saturate((normalizedDistance - 1.0) / 2.0);
        float closeRangeFade = saturate(normalizedDistance - 0.5);
        float combinedFade = distanceFade * closeRangeFade;

        float2 size = cfg.size.xx * 2.0;
        
        float4 tangent;
        float3 billboardProj = BilboardProjection(objectCameraDirection, positionOS.xy, cfg.frames, size, texcoord.xy, tangent);

        float3 billboardPointOS = billboardProj + cfg.offset;
        float3 toCameraDirOS = normalize(objectSpaceCameraPos - billboardPointOS);

        float3 cameraDepthOffsetOS = toCameraDirOS * 0.01;
        float3 finalVertexPosOS = billboardProj + cameraDepthOffsetOS + cfg.offset;

        float3 localDirection = (billboardProj + cfg.offset) - objectSpaceCameraPos;

        // Projected position to camera
        float3 projInterpolated = normalize(objectSpaceCameraPos - (billboardProj + cfg.offset));

        Ray rayLocal;
        rayLocal.Origin = objectSpaceCameraPos - cfg.offset;
        rayLocal.Direction = localDirection;

        float2 grid = VectorToGrid(objectCameraDirection);
        float2 gridRaw = grid;
        grid = saturate((grid + 1.0) * 0.5); 
        grid *= framesMinusOne;

        float2 gridFrac = frac(grid);
        float2 gridFloor = floor(grid);
        float4 weights = TriangleInterpolate(gridFrac);

        // Nearest frames
        float2 frame0 = gridFloor;
        float2 frame1 = gridFloor + lerp(float2(0, 1), float2(1, 0), weights.w);
        float2 frame2 = gridFloor + float2(1, 1);

        // Convert frame coordinate to octahedron direction
        float3 frame0ray = FrameXYToRay(frame0, framesMinusOne.xx);
        float3 frame1ray = FrameXYToRay(frame1, framesMinusOne.xx);
        float3 frame2ray = FrameXYToRay(frame2, framesMinusOne.xx);

        float3 planeCenter = float3(0, 0, 0);
    
        float adjustedParallaxScale = parallaxScale * combinedFade;

        float3 plane0x, plane0z;
        float3 plane0normal = frame0ray;
        float3 frame0local = FrameTransform(projInterpolated, frame0ray, plane0x, plane0z);
        frame0local.xz = frame0local.xz / cfg.frames.xx * adjustedParallaxScale;

        frame0local.xz = clamp(frame0local.xz, -0.5 / cfg.frames, 0.5 / cfg.frames);

        // Virtual plane UV
        float2 vUv0 = VirtualPlaneUV(plane0normal, plane0x, plane0z, planeCenter, size, rayLocal);
        vUv0 /= cfg.frames.xx;
        vUv0 = clamp(vUv0, -0.1 / cfg.frames, 1.1 / cfg.frames);

        float3 plane1x, plane1z;
        float3 plane1normal = frame1ray;
        float3 frame1local = FrameTransform(projInterpolated, frame1ray, plane1x, plane1z);
        frame1local.xz = frame1local.xz / cfg.frames.xx * adjustedParallaxScale;
        frame1local.xz = clamp(frame1local.xz, -0.5 / cfg.frames, 0.5 / cfg.frames);

        float2 vUv1 = VirtualPlaneUV(plane1normal, plane1x, plane1z, planeCenter, size, rayLocal);
        vUv1 /= cfg.frames.xx;
        vUv1 = clamp(vUv1, -0.1 / cfg.frames, 1.1 / cfg.frames);

        float3 plane2x, plane2z;
        float3 plane2normal = frame2ray;
        float3 frame2local = FrameTransform(projInterpolated, frame2ray, plane2x, plane2z);
        frame2local.xz = frame2local.xz / cfg.frames.xx * adjustedParallaxScale;
        frame2local.xz = clamp(frame2local.xz, -0.5 / cfg.frames, 0.5 / cfg.frames);

        float2 vUv2 = VirtualPlaneUV(plane2normal, plane2x, plane2z, planeCenter, size, rayLocal);
        vUv2 /= cfg.frames.xx;
        vUv2 = clamp(vUv2, -0.1 / cfg.frames, 1.1 / cfg.frames);

        // OUTPUTS
        positionOS += (finalVertexPosOS - positionOS);
        normalOS = objectCameraDirection;
        tangentOS = tangent;
        viewPosVS = float4(TransformWorldToView(TransformObjectToWorld(positionOS)), 0);
    
        float2 singleFrameUvDim = float2(texcoord.x, texcoord.y) * (1.0 / cfg.frames.x);
        imp.uv = singleFrameUvDim;
    
        imp.grid = grid;
        imp.frame0 = float4(vUv0.xy, frame0local.xz);
        imp.frame1 = float4(vUv1.xy, frame1local.xz);
        imp.frame2 = float4(vUv2.xy, frame2local.xz);
    }

    inline void ImpostorSampler(inout ImpostorOutput o, in ImpostorData imp, in ImpostorConfig cfg, TEXTURE2D_PARAM(albedoTex, albedoSampler), TEXTURE2D_PARAM(normalTex, normalSampler), out float4 positionCS, out float3 positionWS, float4 viewPosVS)
    {
        float2 fracGrid = frac(imp.grid);
        float4 w4 = TriangleInterpolate(fracGrid);
        float3 weights = w4.xyz;

        float frames = max(1.0, cfg.frames);
        float invFrames = rcp(frames);
        float2 invFrames2 = invFrames.xx;

        float2 gridSnap = floor(imp.grid) * invFrames; // 0..1

        float2 frame0 = gridSnap;
        float2 frame1 = gridSnap + (lerp(float2(0, 1), float2(1, 0), w4.w) * invFrames);
        float2 frame2 = gridSnap + (float2(1, 1) * invFrames);

        float2 base0 = frame0 + imp.frame0.xy;
        float2 base1 = frame1 + imp.frame1.xy;
        float2 base2 = frame2 + imp.frame2.xy;

        float2 texDims = float2(cfg.albedoTexelSize.z, cfg.albedoTexelSize.w);
        float2 frameSizePx = texDims / frames;
        float2 actualDims = floor(frameSizePx) * frames;
        float2 scaleFactor = actualDims / texDims;

        float depth0 = SAMPLE_TEXTURE2D_BIAS(normalTex, normalSampler, base0 * scaleFactor, cfg.textureBias).a;
        float depth1 = SAMPLE_TEXTURE2D_BIAS(normalTex, normalSampler, base1 * scaleFactor, cfg.textureBias).a;
        float depth2 = SAMPLE_TEXTURE2D_BIAS(normalTex, normalSampler, base2 * scaleFactor, cfg.textureBias).a;

        float2 uv0 = (base0 + (0.5 - depth0) * imp.frame0.zw) * scaleFactor;
        float2 uv1 = (base1 + (0.5 - depth1) * imp.frame1.zw) * scaleFactor;
        float2 uv2 = (base2 + (0.5 - depth2) * imp.frame2.zw) * scaleFactor;

        float2 gridSize = (invFrames2) * scaleFactor;
        float2 border = (cfg.albedoTexelSize.xy * cfg.borderClamp) * scaleFactor;

        float2 frame0S = frame0 * scaleFactor;
        float2 frame1S = frame1 * scaleFactor;
        float2 frame2S = frame2 * scaleFactor;

        uv0 = clamp(uv0, frame0S - border, frame0S + gridSize + border);
        uv1 = clamp(uv1, frame1S - border, frame1S + gridSize + border);
        uv2 = clamp(uv2, frame2S - border, frame2S + gridSize + border);

        float4 albedo0 = SAMPLE_TEXTURE2D_BIAS(albedoTex, albedoSampler, uv0, cfg.textureBias);
        float4 albedo1 = SAMPLE_TEXTURE2D_BIAS(albedoTex, albedoSampler, uv1, cfg.textureBias);
        float4 albedo2 = SAMPLE_TEXTURE2D_BIAS(albedoTex, albedoSampler, uv2, cfg.textureBias);

        float4 n0 = SAMPLE_TEXTURE2D_BIAS(normalTex, normalSampler, uv0, cfg.textureBias);
        float4 n1 = SAMPLE_TEXTURE2D_BIAS(normalTex, normalSampler, uv1, cfg.textureBias);
        float4 n2 = SAMPLE_TEXTURE2D_BIAS(normalTex, normalSampler, uv2, cfg.textureBias);

        float4 albedoSample = albedo0 * weights.x + albedo1 * weights.y + albedo2 * weights.z;
        float4 normalSample = n0 * weights.x + n1 * weights.y + n2 * weights.z;

        o.Albedo = albedoSample.rgb;
        o.Normal = normalSample.rgb;
        o.Smoothness = cfg.smoothness;
        o.Metallic = cfg.metallic;
        o.Specular = 0;
        o.Occlusion = 1;
        o.Emission = 0;
    
        float3 localN = normalSample.rgb * 2.0 - 1.0;
        o.WorldNormal = normalize(mul((float3x3) Unity_ObjectToWorld, localN));
    
        o.Alpha = (albedoSample.a - cfg.alphaClip);
        clip(o.Alpha);

        float blendedDepth = depth0 * weights.x + depth1 * weights.y + depth2 * weights.z;
        float depth = (blendedDepth - 0.5) * cfg.depthSize * length(Unity_ObjectToWorld[2].xyz);

        #if defined(UNITY_PASS_SHADOWCASTER)
            viewPosVS.z += depth * cfg.shadowView - cfg.shadowBias;
        #else
            viewPosVS.z += depth + cfg.forwardBias;
        #endif

        positionWS = mul(UNITY_MATRIX_I_V, float4(viewPosVS.xyz, 1)).xyz;

        #if defined(SHADERPASS) && defined(UNITY_PASS_SHADOWCASTER)
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize( _LightPosition - positionWS );
                #else
                    float3 lightDirectionWS = _LightDirection;
            #endif
    
            positionCS = TransformWorldToHClip( ApplyShadowBias( positionWS, float3( 0, 0, 0 ), lightDirectionWS ) );
    
            #if UNITY_REVERSED_Z
                    positionCS.z = min( positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE );
                #else
                    positionCS.z = max( positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE );
            #endif
        #else
            positionCS = mul(UNITY_MATRIX_P, float4(viewPosVS.xyz, 1));
        #endif

        positionCS.xyz /= positionCS.w;
    
        if (UNITY_NEAR_CLIP_VALUE < 0)
            positionCS = positionCS * 0.5 + 0.5;
    }

#endif
