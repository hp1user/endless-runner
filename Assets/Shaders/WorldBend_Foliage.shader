Shader "EndlessRunner/WorldBend_Foliage"
{
    Properties
    {
        [Header(Base Surface Settings)]
        [MainTexture] _BaseMap("Base Map (Albedo)", 2D) = "white" {}
        _Base("Base Map Alias", 2D) = "white" {}
        _Ba("Ba Map Alias", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color Tint", Color) = (1, 1, 1, 1)
        _Tint("Tint Alias", Color) = (1, 1, 1, 1)
        _BumpMap("Base Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1.0
        _Metallic("Base Metallic", Range(0, 1)) = 0.0
        _Smoothness("Base Smoothness", Range(0, 1)) = 0.35
        _NAMR("Packed (NAMR)", 2D) = "white" {}
        [Toggle(_USE_NAMR)] _UseNAMR("Use Packed NAMR Texture", Float) = 1

        [Header(World Bend Settings)]
        _Curvature("World Curvature", Float) = -0.0008
        _DistanceOffset("Bend Distance Offset", Float) = 12.0

        [Header(Packed RGBA Foliage Optimization)]
        [Toggle(_USE_PACKED_FOLIAGE)] _UsePackedFoliage("Use Packed RGBA Foliage (The-Last-Wish)", Float) = 1
        _PackedFoliageTex("Packed Foliage RGBA (Moss + Gravel)", 2D) = "white" {}
        _FoliageTiling("Packed Foliage Tiling", Float) = 2.0
        _DebrisColor("Debris Tint Color", Color) = (0.75, 0.70, 0.65, 1.0)

        [Header(Layer 2 Debris Mud and Gravel)]
        _DebrisTex("Debris / Gravel Albedo", 2D) = "white" {}
        _DebrisNorm("Debris Normal Map", 2D) = "bump" {}
        _DebrisTiling("Debris Tiling", Float) = 1.8
        _DebrisCoverage("Debris Coverage", Range(0, 1)) = 0.55

        [Header(Layer 3 Moss Texture and Placement)]
        _MossTex("Moss Albedo Map", 2D) = "white" {}
        _MossNorm("Moss Normal Map", 2D) = "bump" {}
        _MossTiling("Moss Tiling", Float) = 2.2
        _MossColor("Moss Tint", Color) = (0.85, 0.95, 0.75, 1.0)
        _MossCoverage("Moss Coverage", Range(0, 1)) = 0.42
        _MossSlopeThreshold("Moss Slope Threshold", Range(0, 1)) = 0.45
        _MossSmoothness("Moss Smoothness", Range(0, 0.4)) = 0.08

        [Header(Creeping Vines)]
        _VineTex("Vine Albedo Map (with Alpha)", 2D) = "white" {}
        _VineNorm("Vine Normal Map", 2D) = "bump" {}
        _VineTiling("Vine Tiling", Float) = 1.5
        _VineIntensity("Vine Crawl Intensity", Range(0, 1)) = 0.0

        [Header(Foliage Wind Sway)]
        _WindSpeed("Wind Speed", Float) = 1.5
        _WindStrength("Wind Sway Amplitude", Float) = 0.015
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Geometry"
        }
        LOD 300

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BaseColor;
            float4 _Tint;
            float4 _MossColor;
            float4 _DebrisColor;
            float _FoliageTiling;
            float _BumpScale;
            float _Metallic;
            float _Smoothness;
            float _Curvature;
            float _DistanceOffset;
            float _DebrisTiling;
            float _DebrisCoverage;
            float _MossTiling;
            float _MossCoverage;
            float _MossSlopeThreshold;
            float _MossSmoothness;
            float _VineTiling;
            float _VineIntensity;
            float _WindSpeed;
            float _WindStrength;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_instancing
            #pragma shader_feature_local _USE_NAMR
            #pragma shader_feature_local _USE_PACKED_FOLIAGE

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float4 tangentWS    : TEXCOORD2;
                float2 uv           : TEXCOORD3;
                float3 positionOS   : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_BaseMap);    SAMPLER(sampler_BaseMap);
            TEXTURE2D(_Base);       SAMPLER(sampler_Base);
            TEXTURE2D(_Ba);         SAMPLER(sampler_Ba);
            TEXTURE2D(_BumpMap);    SAMPLER(sampler_BumpMap);
            TEXTURE2D(_NAMR);       SAMPLER(sampler_NAMR);
            TEXTURE2D(_PackedFoliageTex); SAMPLER(sampler_PackedFoliageTex);
            TEXTURE2D(_DebrisTex);  SAMPLER(sampler_DebrisTex);
            TEXTURE2D(_DebrisNorm); SAMPLER(sampler_DebrisNorm);
            TEXTURE2D(_MossTex);    SAMPLER(sampler_MossTex);
            TEXTURE2D(_MossNorm);   SAMPLER(sampler_MossNorm);
            TEXTURE2D(_VineTex);    SAMPLER(sampler_VineTex);
            TEXTURE2D(_VineNorm);   SAMPLER(sampler_VineNorm);

            float Hash21(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float4 h = frac(sin(float4(
                    dot(i, float2(12.9898, 78.233)),
                    dot(i + float2(1.0, 0.0), float2(12.9898, 78.233)),
                    dot(i + float2(0.0, 1.0), float2(12.9898, 78.233)),
                    dot(i + float2(1.0, 1.0), float2(12.9898, 78.233))
                )) * 43758.5453);
                return lerp(lerp(h.x, h.y, f.x), lerp(h.z, h.w, f.x), f.y);
            }

            float FBM_Organic(float2 p)
            {
                float v = 0.50 * Hash21(p);
                v += 0.28 * Hash21(p * 2.3 + float2(1.7, 3.2));
                v += 0.15 * Hash21(p * 5.1 + float2(4.1, 1.9));
                v += 0.07 * Hash21(p * 11.7 + float2(2.3, 5.7));
                return v;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float3 worldNormal = TransformObjectToWorldNormal(input.normalOS);

                // Runner Curvature (World Bend downwards into horizon)
                float dist = max(worldPos.z - _WorldSpaceCameraPos.z - _DistanceOffset, 0.0);
                worldPos.y += dist * dist * _Curvature;

                // Subtle Wind Flutter on high geometry
                float windPhase = _Time.y * _WindSpeed + worldPos.x * 0.5 + worldPos.z * 0.5;
                worldPos.x += sin(windPhase) * _WindStrength * saturate(worldPos.y * 0.08);

                output.positionWS = worldPos;
                output.positionCS = TransformWorldToHClip(worldPos);
                output.normalWS = worldNormal;
                output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.positionOS = input.positionOS.xyz;

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float3 worldPos = input.positionWS;
                float3 worldNormal = normalize(input.normalWS);

                // 1. Sample Base Material (with fallback to _Base or _Ba if _BaseMap is empty)
                half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                if (dot(baseTex.rgb, baseTex.rgb) < 0.001)
                {
                    half4 alt1 = SAMPLE_TEXTURE2D(_Base, sampler_Base, input.uv);
                    if (dot(alt1.rgb, alt1.rgb) > 0.001)
                    {
                        baseTex = alt1;
                    }
                    else
                    {
                        half4 alt2 = SAMPLE_TEXTURE2D(_Ba, sampler_Ba, input.uv);
                        if (dot(alt2.rgb, alt2.rgb) > 0.001)
                        {
                            baseTex = alt2;
                        }
                    }
                }
                baseTex *= (_BaseColor * _Tint);

                half metallic = _Metallic;
                half smoothness = _Smoothness;

                #if defined(_USE_NAMR)
                    half4 namr = SAMPLE_TEXTURE2D(_NAMR, sampler_NAMR, input.uv);
                    if (dot(namr.xyz, namr.xyz) > 0.05)
                    {
                        metallic = namr.b;
                        smoothness = 1.0 - namr.a;
                    }
                #endif

                // 2. High-Sharpness Triplanar Coordinates (Zero Stretching on Barriers & Slopes)
                float3 absN = abs(worldNormal);
                float3 triWeights = pow(absN, 6.0);
                triWeights /= (triWeights.x + triWeights.y + triWeights.z + 1e-5);

                float2 uvY = worldPos.xz;                       // Horizontal surfaces (Road surface, barrier tops)
                float2 uvX = float2(worldPos.z, worldPos.y);   // Barrier side walls along Z
                float2 uvZ = float2(worldPos.x, worldPos.y);   // Walls/ends facing Z

                #if defined(_USE_PACKED_FOLIAGE)
                    // High-performance single-texture sample (YCoCg-style luminance & relief packing)
                    // Road surface (triWeights.y > 0.8) avoids side fetches; barriers avoid top fetches
                    half4 pY = SAMPLE_TEXTURE2D(_PackedFoliageTex, sampler_PackedFoliageTex, uvY * _FoliageTiling);
                    half4 pX = (triWeights.x > 0.05) ? SAMPLE_TEXTURE2D(_PackedFoliageTex, sampler_PackedFoliageTex, uvX * _FoliageTiling) : pY;
                    half4 pZ = (triWeights.z > 0.05) ? SAMPLE_TEXTURE2D(_PackedFoliageTex, sampler_PackedFoliageTex, uvZ * _FoliageTiling) : pY;
                    half4 packedSample = pY * triWeights.y + pX * triWeights.x + pZ * triWeights.z;

                    half4 mossSample = half4(packedSample.rrr * _MossColor.rgb, 1.0);
                    half4 debrisSample = half4(packedSample.ggg * _DebrisColor.rgb, 1.0);
                #else
                    // Fallback: Sample Layer 2: Debris, Mud & Crushed Rocks (Triplanar)
                    half4 debY = SAMPLE_TEXTURE2D(_DebrisTex, sampler_DebrisTex, uvY * _DebrisTiling);
                    half4 debX = SAMPLE_TEXTURE2D(_DebrisTex, sampler_DebrisTex, uvX * _DebrisTiling);
                    half4 debZ = SAMPLE_TEXTURE2D(_DebrisTex, sampler_DebrisTex, uvZ * _DebrisTiling);
                    half4 debrisSample = debY * triWeights.y + debX * triWeights.x + debZ * triWeights.z;

                    // Sample Layer 3: Lush Green Moss Carpet (Triplanar)
                    half4 mossY = SAMPLE_TEXTURE2D(_MossTex, sampler_MossTex, uvY * _MossTiling) * _MossColor;
                    half4 mossX = SAMPLE_TEXTURE2D(_MossTex, sampler_MossTex, uvX * _MossTiling) * _MossColor;
                    half4 mossZ = SAMPLE_TEXTURE2D(_MossTex, sampler_MossTex, uvZ * _MossTiling) * _MossColor;
                    half4 mossSample = mossY * triWeights.y + mossX * triWeights.x + mossZ * triWeights.z;
                #endif

                // 3. Post-Apocalyptic Road Overgrowth (Interwoven Gravel + Moss)
                float slope = saturate(worldNormal.y);
                float verticalFace = 1.0 - slope;

                // Local X coordinate on road mesh (spans -5.0m to +5.0m):
                float localX = abs(input.positionOS.x);

                // Irregular, ragged curb boundary (wobbles naturally rather than forming a straight line)
                float curbEdgeNoise = FBM_Organic(worldPos.xz * 0.7);
                float curbZone = smoothstep(1.8 + curbEdgeNoise * 0.8, 4.7, localX);

                // Center median seam running down the middle of the road
                float centerNoise = FBM_Organic(worldPos.xz * 1.6 + float2(4.3, 9.1));
                float centerSeam = exp(-localX * localX * 4.5) * (centerNoise * 0.45 + 0.35);

                // Multi-scale organic terrain detail
                float nMacro = FBM_Organic(worldPos.xz * 0.75);
                float nMid   = FBM_Organic(worldPos.xz * 2.2 + float2(14.2, 28.7));
                float nMicro = FBM_Organic(worldPos.xz * 4.8);

                // Seamless Chunk Boundary Seam Transition (leaves clean road at chunk seams so chunks merge smoothly)
                // Road mesh vertices span Z from -25.0 to +15.0 (centered at Z = -5.0)
                float distFromChunkSeam = 20.0 - abs(input.positionOS.z + 5.0);
                float seamFade = smoothstep(0.0, 4.0, distFromChunkSeam);

                // --- ROAD DEBRIS & GRAVEL BED ---
                // Mud, dirt, and gravel settle along curbs, center seams, and scattered road patches
                float debrisField = curbZone * 0.65 + centerSeam * 0.40 + (nMacro * 0.45 + nMid * 0.25 + nMicro * 0.15 - 0.22);
                float debrisMask = slope * smoothstep(1.0 - _DebrisCoverage, 1.0 - _DebrisCoverage + 0.18, debrisField) * seamFade;

                // --- ROAD MOSS LAYER (INTERMINGLED WITH GRAVEL) ---
                // Moss clumps root directly within and around the debris beds rather than in a separate band.
                // High-frequency clump noise determines where moss cushion clusters emerge inside gravel
                float mossClumpNoise = FBM_Organic(worldPos.xz * 2.8 + float2(37.5, 19.2));
                float mossField = (debrisMask * 0.75 + curbZone * 0.35 + centerSeam * 0.35) * (mossClumpNoise * 0.70 + 0.35);
                float mossOnRoad = slope * smoothstep(1.0 - _MossCoverage, 1.0 - _MossCoverage + 0.16, mossField) * seamFade;

                // --- BARRIER & WALL OVERGROWTH (GRAVEL BASE + CLIMBING MOSS) ---
                float wallBase = saturate(1.2 - worldPos.y * 1.1);
                float wallNoise = FBM_Organic(float2(worldPos.z * 0.6, worldPos.y * 1.2));
                float debrisOnWall = verticalFace * smoothstep(0.35, 0.70, wallBase * (wallNoise * 0.6 + 0.4));

                float wallClimb = saturate(1.5 - worldPos.y * 0.75);
                float mossOnWall = verticalFace * smoothstep(1.0 - _MossCoverage - 0.05, 1.0 - _MossCoverage + 0.18, wallNoise * wallClimb * 1.4);

                // --- COMBINE OVERGROWTH LAYERS (ORGANIC INTERMIXING) ---
                float totalMoss = saturate(mossOnRoad + mossOnWall);
                float totalDebris = saturate(debrisMask + debrisOnWall);

                // Gravel and moss co-exist: moss clusters sit on top of the gravel bed,
                // and gravel peeks through around the moss cushions instead of being erased into a solid stripe
                float visibleDebris = saturate(totalDebris * (1.0 - totalMoss * 0.65));

                // 4. Layer Blending (Base Asphalt -> Debris / Gravel -> Moss)
                half3 surfaceColor = baseTex.rgb;
                surfaceColor = lerp(surfaceColor, debrisSample.rgb, visibleDebris);
                surfaceColor = lerp(surfaceColor, mossSample.rgb, totalMoss);

                // Physical Material Properties
                float totalOvergrowth = saturate(visibleDebris + totalMoss);
                metallic = lerp(metallic, 0.0, totalOvergrowth);
                smoothness = lerp(smoothness, 0.20, visibleDebris);
                smoothness = lerp(smoothness, _MossSmoothness, totalMoss);

                // Subtle velvet fresnel for lush moss
                float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos);
                float velvetFresnel = pow(1.0 - saturate(dot(worldNormal, viewDir)), 3.0) * 0.18;
                surfaceColor += velvetFresnel * _MossColor.rgb * totalMoss;

                // 5. Lighting
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(worldPos));
                half3 lightColor = mainLight.color * mainLight.shadowAttenuation;
                half NdotL = saturate(dot(worldNormal, mainLight.direction));

                // Subsurface scatter wrap for moss
                half foliageWrap = saturate((dot(worldNormal, mainLight.direction) + 0.35) / 1.35);
                half diffuseFactor = lerp(NdotL, foliageWrap, totalMoss * 0.5);

                half3 ambient = SampleSH(worldNormal) * 0.90;
                half3 directLighting = surfaceColor * lightColor * diffuseFactor;
                half3 ambientLighting = surfaceColor * ambient;

                half3 finalColor = directLighting + ambientLighting;
                return half4(finalColor, baseTex.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float3 worldNormal = TransformObjectToWorldNormal(input.normalOS);

                float dist = max(worldPos.z - _WorldSpaceCameraPos.z - _DistanceOffset, 0.0);
                worldPos.y += dist * dist * _Curvature;

                float3 lightDirection = _MainLightPosition.xyz;
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(worldPos, worldNormal, lightDirection));

                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings DepthVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float dist = max(worldPos.z - _WorldSpaceCameraPos.z - _DistanceOffset, 0.0);
                worldPos.y += dist * dist * _Curvature;

                output.positionCS = TransformWorldToHClip(worldPos);
                return output;
            }

            half4 DepthFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}