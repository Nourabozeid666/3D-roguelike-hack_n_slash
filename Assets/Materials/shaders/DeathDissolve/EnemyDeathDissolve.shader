Shader "Roguelike/Enemy Death Dissolve"
{
    Properties
    {
        [Header(Surface)]
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [Normal] _NormalMap("Normal Map", 2D) = "bump" {}
        _NormalScale("Normal Strength", Range(0, 2)) = 1
        _R_Metallic_G_Occulsion_A_Smoothness("Packed Map (R Metallic, G Occlusion, A Smoothness)", 2D) = "white" {}
        _Metallic("Metallic", Range(0, 1)) = 0
        _Smoothness("Smoothness", Range(0, 1)) = 0.45
        _OcclusionStrength("Occlusion Strength", Range(0, 1)) = 1

        [Header(Death Dissolve)]
        _DissolveAmount("Death Progress", Range(0, 1)) = 0
        [HDR] _EdgeColor("Edge Color", Color) = (0.04, 5, 0.18, 1)
        _EdgeIntensity("Edge Glow", Range(0, 8)) = 2.5
        _EdgeWidth("Edge Width", Range(0.001, 0.3)) = 0.075
        _NoiseScale("Noise Scale", Range(0.1, 20)) = 4
        _NoiseStrength("Noise Distortion", Range(0, 1)) = 0.45
        _NoiseSpeed("Noise Movement (XYZ)", Vector) = (0.08, 0.18, 0.06, 0)

        [Header(Sweep)]
        _DissolveDirection("Direction (Object Space)", Vector) = (0, 1, 0, 0)
        _DissolveOrigin("Origin (Object Space)", Vector) = (0, 0, 0, 0)
        _DissolveRange("Direction Range (Min, Max)", Vector) = (-1, 1, 0, 0)
        [Toggle] _UseWorldSpace("Use World Space", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Render Faces", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "AlphaTest"
            "UniversalMaterialType" = "Lit"
        }
        LOD 300

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

        TEXTURE2D(_NormalMap);
        SAMPLER(sampler_NormalMap);
        TEXTURE2D(_R_Metallic_G_Occulsion_A_Smoothness);
        SAMPLER(sampler_R_Metallic_G_Occulsion_A_Smoothness);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _EdgeColor;
            half _NormalScale;
            half _Metallic;
            half _Smoothness;
            half _OcclusionStrength;
            half _DissolveAmount;
            half _EdgeIntensity;
            half _EdgeWidth;
            half _NoiseScale;
            half _NoiseStrength;
            half _UseWorldSpace;
            float4 _NoiseSpeed;
            float4 _DissolveDirection;
            float4 _DissolveOrigin;
            float4 _DissolveRange;
        CBUFFER_END

        float Hash31(float3 value)
        {
            value = frac(value * 0.1031);
            value += dot(value, value.yzx + 33.33);
            return frac((value.x + value.y) * value.z);
        }

        float ValueNoise(float3 position)
        {
            float3 cell = floor(position);
            float3 localPosition = frac(position);
            float3 blend = localPosition * localPosition * (3.0 - 2.0 * localPosition);

            float n000 = Hash31(cell + float3(0, 0, 0));
            float n100 = Hash31(cell + float3(1, 0, 0));
            float n010 = Hash31(cell + float3(0, 1, 0));
            float n110 = Hash31(cell + float3(1, 1, 0));
            float n001 = Hash31(cell + float3(0, 0, 1));
            float n101 = Hash31(cell + float3(1, 0, 1));
            float n011 = Hash31(cell + float3(0, 1, 1));
            float n111 = Hash31(cell + float3(1, 1, 1));

            float nearPlane = lerp(lerp(n000, n100, blend.x), lerp(n010, n110, blend.x), blend.y);
            float farPlane = lerp(lerp(n001, n101, blend.x), lerp(n011, n111, blend.x), blend.y);
            return lerp(nearPlane, farPlane, blend.z);
        }

        float FractalNoise(float3 position)
        {
            float noise = ValueNoise(position) * 0.5714;
            noise += ValueNoise(position * 2.03 + 17.17) * 0.2857;
            noise += ValueNoise(position * 4.01 + 43.73) * 0.1429;
            return noise;
        }

        float GetDissolveDistance(float3 positionOS, float3 positionWS)
        {
            float3 dissolvePosition = lerp(positionOS, positionWS, saturate(_UseWorldSpace));
            float3 direction = _DissolveDirection.xyz;
            direction *= rsqrt(max(dot(direction, direction), 0.0001));

            float rangeSize = max(_DissolveRange.y - _DissolveRange.x, 0.0001);
            float directionValue = dot(dissolvePosition - _DissolveOrigin.xyz, direction);
            float sweep = saturate((directionValue - _DissolveRange.x) / rangeSize);

            float3 movingPosition = dissolvePosition * _NoiseScale + _Time.y * _NoiseSpeed.xyz;
            float noise = FractalNoise(movingPosition);
            float field = sweep + (noise - 0.5) * _NoiseStrength;

            // The small margins guarantee a completely solid result at 0 and
            // a completely invisible result at 1.
            float thresholdMin = -0.5 * _NoiseStrength - 0.002;
            float thresholdMax = 1.0 + 0.5 * _NoiseStrength + 0.002;
            float threshold = lerp(thresholdMin, thresholdMax, saturate(_DissolveAmount));
            return field - threshold;
        }

        void ApplyDissolve(float3 positionOS, float3 positionWS, out half edgeMask)
        {
            float distanceToEdge = GetDissolveDistance(positionOS, positionWS);
            clip(distanceToEdge);
            edgeMask = 1.0h - smoothstep(0.0h, max(_EdgeWidth, 0.0001h), distanceToEdge);
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ForwardVertex
            #pragma fragment ForwardFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                float2 staticLightmapUV : TEXCOORD1;
                float2 dynamicLightmapUV : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                half3 normalWS : TEXCOORD3;
                half4 tangentWS : TEXCOORD4;
                half4 fogAndVertexLight : TEXCOORD5;
                float4 shadowCoord : TEXCOORD6;
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 7);
                #ifdef DYNAMICLIGHTMAP_ON
                    float2 dynamicLightmapUV : TEXCOORD8;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings ForwardVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = positionInputs.positionCS;
                output.positionOS = input.positionOS.xyz;
                output.positionWS = positionInputs.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = half4(normalInputs.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                output.shadowCoord = GetShadowCoord(positionInputs);

                half fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                half3 vertexLight = VertexLighting(positionInputs.positionWS, normalInputs.normalWS);
                output.fogAndVertexLight = half4(fogFactor, vertexLight);

                OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
                #ifdef DYNAMICLIGHTMAP_ON
                    output.dynamicLightmapUV = input.dynamicLightmapUV * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
                #endif
                OUTPUT_SH(output.normalWS, output.vertexSH);

                return output;
            }

            half4 ForwardFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half edgeMask;
                ApplyDissolve(input.positionOS, input.positionWS, edgeMask);

                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                half4 packedSample = SAMPLE_TEXTURE2D(
                    _R_Metallic_G_Occulsion_A_Smoothness,
                    sampler_R_Metallic_G_Occulsion_A_Smoothness,
                    input.uv);
                half3 normalTS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv),
                    _NormalScale);

                half3 bitangentWS = input.tangentWS.w * cross(input.normalWS, input.tangentWS.xyz);
                half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangentWS, input.normalWS);
                half3 normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, tangentToWorld));

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = baseSample.rgb;
                surfaceData.metallic = saturate(packedSample.r * _Metallic);
                surfaceData.specular = half3(0, 0, 0);
                surfaceData.smoothness = saturate(packedSample.a * _Smoothness);
                surfaceData.normalTS = normalTS;
                surfaceData.occlusion = lerp(1.0h, packedSample.g, _OcclusionStrength);
                surfaceData.emission = _EdgeColor.rgb * (_EdgeIntensity * edgeMask);
                surfaceData.alpha = 1.0h;
                surfaceData.clearCoatMask = 0.0h;
                surfaceData.clearCoatSmoothness = 0.0h;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = input.shadowCoord;
                inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogAndVertexLight.x);
                inputData.vertexLighting = input.fogAndVertexLight.yzw;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

                #ifdef DYNAMICLIGHTMAP_ON
                    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, normalWS);
                #else
                    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, normalWS);
                #endif
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            Cull [_Cull]
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            ShadowVaryings ShadowVertex(ShadowAttributes input)
            {
                ShadowVaryings output = (ShadowVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                output.positionCS = positionCS;
                output.positionOS = input.positionOS.xyz;
                output.positionWS = positionWS;
                return output;
            }

            half4 ShadowFragment(ShadowVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half edgeMask;
                ApplyDissolve(input.positionOS, input.positionWS, edgeMask);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            Cull [_Cull]
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthVertex
            #pragma fragment DepthFragment
            #pragma multi_compile_instancing

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            DepthVaryings DepthVertex(DepthAttributes input)
            {
                DepthVaryings output = (DepthVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionOS = input.positionOS.xyz;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }

            half4 DepthFragment(DepthVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half edgeMask;
                ApplyDissolve(input.positionOS, input.positionWS, edgeMask);
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
