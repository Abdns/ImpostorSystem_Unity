Shader "Impostor/ImpostorURP"
{
    Properties
    {
        [Header(Impostor Maps)]
        [NoScaleOffset]_ImpostorAlbedoMap("Impostor Albedo (RGBA)", 2D) = "white" {}
        [NoScaleOffset]_ImpostorNormalMap("Impostor Normal (RGB) Depth (A)", 2D) = "gray" {}

        [Header(Impostor Settings)]
        [Toggle(_IMPOSTOR_USE_HEMI)]_ImpostorIsHalfSphere("Impostor Is Half Sphere", Float) = 0
        _ImpostorFrames("Impostor Frames", Float) = 12
        _ImpostorSize("Impostor Size", Float) = 1
        _ImpostorOffset("Impostor Offset", Vector) = (0, 0, 0, 0)
        _ImpostorBorderClamp("Impostor Border Clamp (px)", Range(0, 32)) = 2.0

        _ImpostorShadowBias("Shadow Bias", Range(0, 2)) = 0.333
        _ImpostorShadowView("Shadow View", Range(0, 1)) = 1
        _ImpostorForwardBias("Forward Bias", Range(0, 2)) = 0

        _Parallax("Parallax", Range(-2, 2)) = -2
        _TextureBias("Texture Bias (Mip bias)", Range(-2, 2)) = 0
        _DepthSize("DepthSize", Float) = 1

        [Header(Material)]
        _AlphaClipp("Alpha Cutoff", Range(0, 1)) = 0.5
        _Smoothness("Smoothness", Range(0, 1)) = 0.5
        _Metallic("Metallic", Range(0, 1)) = 0

        [HideInInspector][ToggleUI]_ZWrite("_ZWrite", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="AlphaTest"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
        }

        Cull Back
        ZWrite On
        ZTest LEqual
        Offset 0, 0

        HLSLINCLUDE
        #pragma target 4.5

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
        #include "ImpostorStructs.hlsl"

        #pragma shader_feature_local _IMPOSTOR_USE_HEMI
        
        #ifdef _IMPOSTOR_USE_HEMI
            #define IMPOSTOR_USE_HEMI
        #endif

        TEXTURE2D(_ImpostorAlbedoMap);
        SAMPLER(sampler_ImpostorAlbedoMap);

        TEXTURE2D(_ImpostorNormalMap);
        SAMPLER(sampler_ImpostorNormalMap);

        float4 _ImpostorAlbedoMap_TexelSize;
        float4 _ImpostorNormalMap_TexelSize;

        CBUFFER_START(UnityPerMaterial)
            float3 _ImpostorOffset;
            float  _ImpostorFrames;
            float  _ImpostorSize;
            float  _ImpostorIsHalfSphere;
            float  _ImpostorBorderClamp;

            float _AlphaClipp;
            float _Smoothness;
            float _Metallic;

            float _Parallax;
            float _TextureBias;
            float _DepthSize;
            float _ImpostorShadowBias;
            float _ImpostorShadowView;
            float _ImpostorForwardBias;
        CBUFFER_END

        ImpostorConfig SetupImpostorConfig()
        {
            ImpostorConfig cfg;
            cfg.frames = _ImpostorFrames;
            cfg.size = _ImpostorSize;
            cfg.offset = _ImpostorOffset;
            cfg.borderClamp = _ImpostorBorderClamp;
            cfg.parallax = _Parallax;
            cfg.textureBias = _TextureBias;
            cfg.depthSize = _DepthSize;
            cfg.alphaClip = _AlphaClipp;
            cfg.metallic = _Metallic;
            cfg.smoothness = _Smoothness;
            cfg.shadowView = _ImpostorShadowView;
            cfg.shadowBias = _ImpostorShadowBias;
            cfg.forwardBias = _ImpostorForwardBias;
            cfg.albedoTexelSize = _ImpostorAlbedoMap_TexelSize;
            
            return cfg;
        }

        ENDHLSL

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            Blend One Zero, One Zero
            ZWrite On
            ZTest LEqual
            Offset 0,0
            ColorMask RGBA      

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #define SHADERPASS SHADERPASS_FORWARD

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile_fog

            #include "ImpostorAlgo.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half3  normalOS   : NORMAL;
                half4  tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;

                #if defined(LIGHTMAP_ON)
                    float4 texcoord1 : TEXCOORD1;
                #endif
                #if defined(DYNAMICLIGHTMAP_ON)
                    float4 texcoord2 : TEXCOORD2;
                #endif
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3  normalWS   : TEXCOORD1;
                float4 tangentWS  : TEXCOORD2;

                float4 uvGrid  : TEXCOORD3;
                float4 plane0  : TEXCOORD4;
                float4 plane1  : TEXCOORD5;
                float4 plane2  : TEXCOORD6;
                float4 viewPos : TEXCOORD7;

                
                float4 lightmapUVOrVertexSH : TEXCOORD8;
                half4 fogFactorAndVertexLight : TEXCOORD9;

                #if defined(DYNAMICLIGHTMAP_ON)
                    float2 dynamicLightmapUV : TEXCOORD10;
                #endif

                float4 probeOcclusion : TEXCOORD11;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;

                ImpostorData imp;
                imp.uv = input.uv;
                ImpostorConfig cfg = SetupImpostorConfig(); 
                
                ImpostorVertex(imp, cfg, input.positionOS.xyz, input.normalOS, input.tangentOS, o.viewPos);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs   normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                // GI
                #if defined(LIGHTMAP_ON)
                    OUTPUT_LIGHTMAP_UV(input.texcoord1, unity_LightmapST, o.lightmapUVOrVertexSH.xy);
                #else
                    o.lightmapUVOrVertexSH.xy = 0;
                #endif

                #if defined(DYNAMICLIGHTMAP_ON)
                    o.dynamicLightmapUV = input.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
                #endif

                OUTPUT_SH4(vertexInput.positionWS, normalInput.normalWS.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), o.lightmapUVOrVertexSH.xyz, o.probeOcclusion);

                o.fogFactorAndVertexLight = 0;
                #if !defined(_FOG_FRAGMENT)
                    o.fogFactorAndVertexLight.x = ComputeFogFactor(vertexInput.positionCS.z);
                #endif
                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                    half3 vLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
                    o.fogFactorAndVertexLight.yzw = vLight;
                #endif

                o.uvGrid.xy = imp.uv;
                o.uvGrid.zw = imp.grid;
                o.plane0 = imp.frame0;
                o.plane1 = imp.frame1;
                o.plane2 = imp.frame2;

                o.positionCS = vertexInput.positionCS;
                o.positionWS = vertexInput.positionWS;
                o.normalWS   = normalInput.normalWS;
                o.tangentWS  = float4(normalInput.tangentWS, (input.tangentOS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale());

                return o;
            }

            half4 Frag(Varyings input, out float outputDepth : SV_Depth) : SV_Target
            {
                float4 shadowCoord = TransformWorldToShadowCoord( input.positionWS );
                float renormFactor = 1.0 / max( FLT_MIN, length( input.normalWS ) );

                float3 PositionWS = input.positionWS;
                float3 ViewDirWS = GetWorldSpaceNormalizeViewDir( PositionWS );
                float4 ShadowCoord = shadowCoord;
                float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
                float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;
                float4 ScreenPos = ComputeScreenPos( ClipPos );
                
                ImpostorData imp;
                imp.uv = input.uvGrid.xy;
                imp.grid = input.uvGrid.zw;
                imp.frame0 = input.plane0;
                imp.frame1 = input.plane1;
                imp.frame2 = input.plane2;

                ImpostorOutput io = (ImpostorOutput)0;
                ImpostorConfig cfg = SetupImpostorConfig(); 

                ImpostorSampler(
                    io, imp, cfg,
                    TEXTURE2D_ARGS(_ImpostorAlbedoMap, sampler_ImpostorAlbedoMap),
                    TEXTURE2D_ARGS(_ImpostorNormalMap, sampler_ImpostorNormalMap),
                    ClipPos, PositionWS, input.viewPos
                );

                float3 BaseColor = io.Albedo;
                float3 Normal = io.WorldNormal;
                float3 Specular = io.Specular;
                float Smoothness = io.Smoothness;
                float Occlusion = io.Occlusion;
                float3 Emission = io.Emission;
                float Alpha = io.Alpha;
                
                float DeviceDepth = ClipPos.z;
                            
                ShadowCoord = TransformWorldToShadowCoord( PositionWS );

                InputData inputData = (InputData)0;
                inputData.positionWS = PositionWS;
                inputData.positionCS = float4( input.positionCS.xy, ClipPos.zw / ClipPos.w );
                inputData.normalizedScreenSpaceUV = ScreenPosNorm.xy;
                inputData.viewDirectionWS = ViewDirWS;
                inputData.shadowCoord = ShadowCoord;
                inputData.normalWS = Normal;

                #if defined(FOG_ANY)
                    inputData.fogCoord = InitializeInputDataFog(float4(inputData.positionWS, 1.0), input.fogFactorAndVertexLight.x);
                #endif
                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
                #endif

                float3 SH = SampleSH(inputData.normalWS.xyz);

                #if defined(DYNAMICLIGHTMAP_ON)
                    inputData.bakedGI = SAMPLE_GI(input.lightmapUVOrVertexSH.xy, input.dynamicLightmapUV.xy, SH, inputData.normalWS);
                    inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUVOrVertexSH.xy);
                #elif !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
                    inputData.bakedGI = SAMPLE_GI( SH, GetAbsolutePositionWS(inputData.positionWS),
                        inputData.normalWS,
                        inputData.viewDirectionWS,
                        input.positionCS.xy,
                        input.probeOcclusion,
                        inputData.shadowMask );
                #else
                    inputData.bakedGI = SAMPLE_GI(input.lightmapUVOrVertexSH.xy, SH, inputData.normalWS);
                    inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUVOrVertexSH.xy);
                #endif

                SurfaceData surfaceData;
                surfaceData.albedo              = BaseColor;
                surfaceData.metallic            = saturate(_Metallic);
                surfaceData.specular            = Specular;
                surfaceData.smoothness          = saturate(Smoothness);
                surfaceData.occlusion           = Occlusion;
                surfaceData.emission            = Emission;
                surfaceData.alpha               = saturate(Alpha);
                surfaceData.normalTS            = Normal;
                surfaceData.clearCoatMask       = 0;
                surfaceData.clearCoatSmoothness = 1;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);

                outputDepth = DeviceDepth;

                return half4( color.rgb, OutputAlpha( color.a, 0 ) );
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------------
        // DepthOnly
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }

            ZWrite On
            ColorMask R
            AlphaToMask Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #define ASE_DEPTH_WRITE_ON
            #define SHADERPASS SHADERPASS_DEPTHONLY

            #include "ImpostorAlgo.hlsl"
            
            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                half3  normalOS   : NORMAL;
                half4  tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 uvGrid  : TEXCOORD1;
                float4 plane0  : TEXCOORD2;
                float4 plane1  : TEXCOORD3;
                float4 plane2  : TEXCOORD4;
                float4 viewPos : TEXCOORD5;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;

                ImpostorData imp;
                imp.uv = input.uv;
                ImpostorConfig cfg = SetupImpostorConfig(); 
                
                ImpostorVertex(imp, cfg, input.positionOS.xyz, input.normalOS , input.tangentOS, o.viewPos);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS);

                o.positionCS = vertexInput.positionCS; 
                o.positionWS = vertexInput.positionWS;

                o.uvGrid.xy = imp.uv;
                o.uvGrid.zw = imp.grid;
                o.plane0 = imp.frame0;
                o.plane1 = imp.frame1;
                o.plane2 = imp.frame2;

                return o;
            }

            half4 Frag(Varyings input, out float outputDepth : SV_Depth) : SV_Target
            {
                float3 PositionWS = input.positionWS;
                float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
                float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;

                ImpostorData imp;
                imp.uv = input.uvGrid.xy;
                imp.grid = input.uvGrid.zw;
                imp.frame0 = input.plane0;
                imp.frame1 = input.plane1;
                imp.frame2 = input.plane2;

                ImpostorOutput io = (ImpostorOutput)0;
                ImpostorConfig cfg = SetupImpostorConfig(); 

                ImpostorSampler(
                    io, imp, cfg,
                    TEXTURE2D_ARGS(_ImpostorAlbedoMap, sampler_ImpostorAlbedoMap),
                    TEXTURE2D_ARGS(_ImpostorNormalMap, sampler_ImpostorNormalMap),
                    ClipPos, PositionWS, input.viewPos
                );

                outputDepth = ClipPos.z;
                return 0;
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------------
        // ShadowCaster
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            AlphaToMask Off
            ColorMask 0

            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #define SHADERPASS SHADERPASS_SHADOWCASTER
            #define UNITY_PASS_SHADOWCASTER 1

            float3 _LightDirection;
            float3 _LightPosition;

            #include "ImpostorAlgo.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half3  normalOS   : NORMAL;
                half4  tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 uvGrid  : TEXCOORD1;
                float4 plane0  : TEXCOORD2;
                float4 plane1  : TEXCOORD3;
                float4 plane2  : TEXCOORD4;
                float4 viewPos : TEXCOORD5;
            };
     

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;

                ImpostorData imp;
                imp.uv = input.uv;
                ImpostorConfig cfg = SetupImpostorConfig(); 

                ImpostorVertex(imp, cfg, input.positionOS.xyz, input.normalOS, input.tangentOS, o.viewPos);

                float3 positionWS = TransformObjectToWorld( input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldDir(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                o.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                o.positionCS = ApplyShadowClamping( o.positionCS);
                o.positionWS = positionWS;

                o.uvGrid.xy = imp.uv;
                o.uvGrid.zw = imp.grid;
                o.plane0 = imp.frame0;
                o.plane1 = imp.frame1;
                o.plane2 = imp.frame2;

                return o;
            }

            half4 Frag(Varyings input, out float outputDepth : SV_Depth) : SV_Target
            {      
                ImpostorData imp;
                imp.uv = input.uvGrid.xy;
                imp.grid = input.uvGrid.zw;
                imp.frame0 = input.plane0;
                imp.frame1 = input.plane1;
                imp.frame2 = input.plane2;

                ImpostorOutput io = (ImpostorOutput)0;

                float3 PositionWS = input.positionWS;
                float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
                float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;

                ImpostorConfig cfg = SetupImpostorConfig(); 

                ImpostorSampler(
                    io, imp, cfg,
                    TEXTURE2D_ARGS(_ImpostorAlbedoMap, sampler_ImpostorAlbedoMap),
                    TEXTURE2D_ARGS(_ImpostorNormalMap, sampler_ImpostorNormalMap),
                    ClipPos, PositionWS, input.viewPos
                );

                float DeviceDepth = ClipPos.z;
                outputDepth = DeviceDepth;

                return 0;
            }

            ENDHLSL
        }
    }
}
