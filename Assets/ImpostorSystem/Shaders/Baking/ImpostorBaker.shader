Shader "IMP/ImpostorBaker"
{
    Properties
    {
    }
    SubShader
    {
        ZTest LEqual
        ZWrite On
        Cull Off

        // --- 1. MinMax ---
        Pass
        {
            Blend One One

            HLSLPROGRAM
            #pragma target 4.0
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes 
            { 
                float3 positionOS : POSITION; 
            };

            struct Varyings 
            { 
                float4 positionCS : SV_POSITION; 
            };

            Varyings vert(Attributes input) 
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                return output;
            }

            float4 frag(Varyings input) : SV_Target 
            { 
                return 1; 
            }
            ENDHLSL
        }
        
        // --- 2. Alpha Copy ---
        Pass
        {
            Blend One One

            HLSLPROGRAM
            #pragma target 4.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_AlphaMap);
            SAMPLER(sampler_AlphaMap);
            float4 _Channels;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.uv = input.uv;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float alpha = SAMPLE_TEXTURE2D_LOD(_AlphaMap, sampler_AlphaMap, input.uv, 0).a;
                return alpha * _Channels;
            }
            ENDHLSL
        }

        // --- 3. Merge Normals ---
        Pass
        {
            Blend One Zero

            HLSLPROGRAM
            #pragma target 4.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"

            SAMPLER(SamplerState_Point_Clamp);
            TEXTURE2D(_NormalMap);
            TEXTURE2D(_DepthMap);
            float4 _NormalMap_ST;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _NormalMap);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float4 normalData = SAMPLE_TEXTURE2D_LOD(_NormalMap, SamplerState_Point_Clamp, input.uv, 0);
                float depthSample = SAMPLE_TEXTURE2D_LOD(_DepthMap, SamplerState_Point_Clamp, input.uv, 0).r;
                
                float3 worldNormal = normalData.rgb;
                
                #if UNITY_REVERSED_Z != 1
                    depthSample = 1.0 - depthSample;
                #endif
                
                float3 packedNormal = worldNormal * 0.5 + 0.5;
                return float4(packedNormal, depthSample);
            }
            ENDHLSL
        }

        // --- 4. Metallic Correction ---
        Pass
        {
            Blend One Zero

            HLSLPROGRAM
            #pragma target 4.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            SAMPLER(SamplerState_Point_Clamp);
            TEXTURE2D(_MainTex);
            TEXTURE2D(_GBuffer1);
            float4 _MainTex_TexelSize;
            float4 _MainTex_ST;

            #define kDielectricSpec float4(0.04, 0.04, 0.04, 1.0 - 0.04)

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float4 outColor = SAMPLE_TEXTURE2D_LOD(_MainTex, SamplerState_Point_Clamp, input.uv, 0);
                float3 result = outColor.rgb;
                float4 reflectivity = SAMPLE_TEXTURE2D_LOD(_GBuffer1, SamplerState_Point_Clamp, input.uv, 0);
                float3 oneMinusReflectivity = saturate(outColor.rgb - reflectivity.rgb);
                result.rgb *= oneMinusReflectivity;

                return float4(result.rgb, outColor.a);
            }
            ENDHLSL
        }

        // --- 5. Dilate ---
        Pass
        {
            Blend One Zero

            HLSLPROGRAM
            #pragma target 4.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            SAMPLER(SamplerState_Point_Clamp);
            TEXTURE2D(_MainTex);
            TEXTURE2D(_DilateMask);
            float4 _MainTex_TexelSize;
            float4 _MainTex_ST;
            float4 _Channels;
            float _UseMaskAsAlpha;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float4 outColor = SAMPLE_TEXTURE2D_LOD(_MainTex, SamplerState_Point_Clamp, input.uv, 0);
                float mask = SAMPLE_TEXTURE2D_LOD(_DilateMask, SamplerState_Point_Clamp, input.uv, 0).r;

                float4 result = outColor;

                if (mask <= 0)
                {
                    const int radius = 20;
                    float minDistance = 999999;
                    float4 closestColor = outColor;

                    UNITY_LOOP for (int i = -radius; i <= radius; ++i)
                    {
                        UNITY_LOOP for (int j = -radius; j <= radius; ++j)
                        {
                            if (i == 0 && j == 0) continue;

                            float2 offset = float2(i, j) * _MainTex_TexelSize.xy;
                            float2 sampleUV = input.uv + offset;

                            if (sampleUV.x < 0 || sampleUV.x > 1 || sampleUV.y < 0 || sampleUV.y > 1) continue;

                            float texelDistance = length(float2(i, j));
                            float sampleMask = SAMPLE_TEXTURE2D_LOD(_DilateMask, SamplerState_Point_Clamp, sampleUV, 0).r;

                            if (sampleMask > 0 && texelDistance < minDistance)
                            {
                                minDistance = texelDistance;
                                closestColor = SAMPLE_TEXTURE2D_LOD(_MainTex, SamplerState_Point_Clamp, sampleUV, 0);
                            }
                        }
                    }
                    result = closestColor;
                }

                result = lerp(outColor, result, _Channels);
              
                if (_UseMaskAsAlpha > 0.5)
                {
                    result.a = mask;
                }

                return result;
            }
            ENDHLSL
        }        
    }
}
