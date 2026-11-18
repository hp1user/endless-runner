#ifndef FOLIAGE_BLEND_INCLUDED
#define FOLIAGE_BLEND_INCLUDED

// =============================================================================
// Post-Apocalyptic Road & Foliage Blending Shader Library
// Realistic 3-Layer Surface:
//   Layer 1: Base Highway Prop / Asphalt
//   Layer 2: Debris, Mud & Crushed Gravel (brown_mud_rocks)
//   Layer 3: Seamless Moss & Climbing Vines (Lush Green Moss Carpet)
// Features Non-Stretching Triplanar Mapping on Side Road Barriers & Vertical Walls
// =============================================================================

float Hash21_Foliage(float2 p)
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
    float v = 0.50 * Hash21_Foliage(p);
    v += 0.28 * Hash21_Foliage(p * 2.3 + float2(1.7, 3.2));
    v += 0.15 * Hash21_Foliage(p * 5.1 + float2(4.1, 1.9));
    v += 0.07 * Hash21_Foliage(p * 11.7 + float2(2.3, 5.7));
    return v;
}

void FoliageSurfaceBlend_float(
    float3 BaseColor,
    float3 BaseNormalTS,
    float BaseMetallic,
    float BaseSmoothness,
    float BaseAO,
    float3 WorldPos,
    float3 WorldNormal,
    float4 DebrisAlbedo,
    float3 DebrisNormalTS,
    float DebrisCoverage,
    float4 MossAlbedo,
    float3 MossNormalTS,
    float4 MossColor,
    float MossCoverage,
    float MossSlopeThreshold,
    float MossSmoothness,
    float4 VineAlbedo,
    float3 VineNormalTS,
    float VineIntensity,
    out float3 OutColor,
    out float3 OutNormalTS,
    out float OutMetallic,
    out float OutSmoothness,
    out float OutAO
)
{
    float slope = saturate(WorldNormal.y);
    float verticalFace = 1.0 - slope;

    // Triplanar blend weights based on absolute surface normal
    float3 triW = abs(WorldNormal);
    triW = pow(triW, 4.0);
    triW /= (triW.x + triW.y + triW.z + 1e-4);

    // Multi-octave FBM triplanar noise
    float2 sUV = float2(WorldPos.z, WorldPos.y) * 0.4;
    float2 si = floor(sUV); float2 sf = frac(sUV); sf = sf * sf * (3.0 - 2.0 * sf);
    float4 sh = frac(sin(float4(dot(si, float2(12.9898, 78.233)), dot(si + float2(1,0), float2(12.9898, 78.233)), dot(si + float2(0,1), float2(12.9898, 78.233)), dot(si + float2(1,1), float2(12.9898, 78.233)))) * 43758.5453);
    float sideNoise = lerp(lerp(sh.x, sh.y, sf.x), lerp(sh.z, sh.w, sf.x), sf.y);

    float2 tUV = WorldPos.xz * 0.4;
    float2 ti = floor(tUV); float2 tf = frac(tUV); tf = tf * tf * (3.0 - 2.0 * tf);
    float4 th = frac(sin(float4(dot(ti, float2(12.9898, 78.233)), dot(ti + float2(1,0), float2(12.9898, 78.233)), dot(ti + float2(0,1), float2(12.9898, 78.233)), dot(ti + float2(1,1), float2(12.9898, 78.233)))) * 43758.5453);
    float topNoise = lerp(lerp(th.x, th.y, tf.x), lerp(th.z, th.w, tf.x), tf.y);

    float2 fUV = float2(WorldPos.x, WorldPos.y) * 0.4;
    float2 fi = floor(fUV); float2 ff = frac(fUV); ff = ff * ff * (3.0 - 2.0 * ff);
    float4 fh = frac(sin(float4(dot(fi, float2(12.9898, 78.233)), dot(fi + float2(1,0), float2(12.9898, 78.233)), dot(fi + float2(0,1), float2(12.9898, 78.233)), dot(fi + float2(1,1), float2(12.9898, 78.233)))) * 43758.5453);
    float frontNoise = lerp(lerp(fh.x, fh.y, ff.x), lerp(fh.z, fh.w, ff.x), ff.y);

    float surfaceNoise = sideNoise * triW.x + topNoise * triW.y + frontNoise * triW.z;

    // Fine detail noise
    float2 tfUV = WorldPos.xz * 1.8;
    float2 tfi = floor(tfUV); float2 tff = frac(tfUV); tff = tff * tff * (3.0 - 2.0 * tff);
    float4 tfh = frac(sin(float4(dot(tfi, float2(12.9898, 78.233)), dot(tfi + float2(1,0), float2(12.9898, 78.233)), dot(tfi + float2(0,1), float2(12.9898, 78.233)), dot(tfi + float2(1,1), float2(12.9898, 78.233)))) * 43758.5453);
    float fineNoise = lerp(lerp(tfh.x, tfh.y, tff.x), lerp(tfh.z, tfh.w, tff.x), tff.y);

    // 1. Road edge bias (road width: lanes at X=0, +-2; curbs at +-3)
    float edgeBoost = smoothstep(1.0, 3.2, abs(WorldPos.x)) * 0.35;
    float roadNoise = surfaceNoise * 0.65 + fineNoise * 0.35 + edgeBoost;

    // Seamless Chunk Boundary Seam Transition (leaves clean road at chunk seams so chunks merge smoothly)
    // Road mesh vertices span Z from -25.0 to +15.0 (centered at Z = -5.0)
    float distFromChunkSeam = 20.0 - abs(WorldPos.z + 5.0);
    float seamFade = smoothstep(0.0, 4.0, distFromChunkSeam);

    // 2. Layer 2: Debris & Gravel Mask
    float debThresh = 1.0 - saturate(DebrisCoverage);
    float debrisMask = slope * smoothstep(debThresh - 0.12, debThresh + 0.12, roadNoise) * seamFade;

    // 3. Layer 3: Moss Mask
    float mossThresh = 1.0 - saturate(MossCoverage);
    float mossOnRoad = slope * smoothstep(mossThresh - 0.12, mossThresh + 0.12, roadNoise) * seamFade;

    // 4. Barrier & Wall Overgrowth
    float wallClimb = saturate(1.8 - WorldPos.y * 0.5);
    float wallNoise = sideNoise * wallClimb;
    float mossOnWall = verticalFace * smoothstep(mossThresh - 0.15, mossThresh + 0.15, wallNoise * 1.3);
    float vineOnWall = verticalFace * VineAlbedo.a * smoothstep(1.0 - saturate(VineIntensity) - 0.15, 1.0 - saturate(VineIntensity) + 0.15, wallNoise * 1.4);

    float totalMossMask = saturate(mossOnRoad + mossOnWall);
    float totalVineMask = saturate(vineOnWall);
    float visibleDebris = saturate(debrisMask - totalMossMask * 0.5);

    // Safe Base Normal: when _NAMR is missing or null (like on road meshes), normal is degenerate (-1, -1, 0)
    float3 safeBaseNormal = (BaseNormalTS.z > 0.1 && dot(BaseNormalTS, BaseNormalTS) > 0.5) 
        ? BaseNormalTS 
        : float3(0.0, 0.0, 1.0);

    // Safe Base AO: JPEG textures (like Road Stylized.jpg) have no alpha (alpha = 0).
    // In outdoor open highway daylight, AO must be 1.0 when not provided.
    float safeBaseAO = (BaseAO < 0.05) ? 1.0 : BaseAO;

    // Safe Base Smoothness: when _NAMR is missing, smoothness evaluates to 1.0 (pure wet mirror)
    float safeBaseSmoothness = (BaseSmoothness > 0.95 && BaseNormalTS.z <= 0.1) ? 0.25 : BaseSmoothness;

    // Layer Color Blending: Base -> Debris (Mud & Gravel) -> Moss -> Vines
    float3 col = lerp(BaseColor, DebrisAlbedo.rgb, visibleDebris);
    col = lerp(col, MossAlbedo.rgb * MossColor.rgb, totalMossMask);
    col = lerp(col, VineAlbedo.rgb, totalVineMask);

    // Physical Material Properties
    float totalOvergrowth = saturate(visibleDebris + totalMossMask + totalVineMask);
    float met = lerp(BaseMetallic, 0.0, totalOvergrowth);
    float smt = lerp(safeBaseSmoothness, 0.2, visibleDebris);
    smt = lerp(smt, MossSmoothness, totalMossMask);

    // Normal Blending
    float3 norm = lerp(safeBaseNormal, DebrisNormalTS, visibleDebris);
    norm = lerp(norm, MossNormalTS, totalMossMask);
    norm = normalize(lerp(norm, VineNormalTS, totalVineMask));

    // Crevice Occlusion
    float ao = safeBaseAO * lerp(1.0, 0.7, saturate(visibleDebris * 0.4 + totalMossMask * 0.6));

    OutColor = col;
    OutNormalTS = norm;
    OutMetallic = met;
    OutSmoothness = smt;
    OutAO = ao;
}

// =============================================================================
// YCoCg / Packed RGBA Foliage Texture Sampler
// Inspired by The-Last-Wish packed texture optimization
// R: Moss Albedo Luminance
// G: Debris / Gravel Albedo Luminance
// B: Moss Relief / Normal Depth
// A: Debris Relief / Normal Depth
// =============================================================================
void UnpackFoliagePacked_float(
    UnityTexture2D PackedTex,
    float2 UV,
    float3 MossColor,
    float3 DebrisColor,
    out float3 OutMossAlbedo,
    out float3 OutDebrisAlbedo,
    out float OutMossRelief,
    out float OutDebrisRelief
)
{
    float4 packedData = PackedTex.tex.Sample(PackedTex.samplerstate, UV);
    OutMossAlbedo = packedData.r * MossColor;
    OutDebrisAlbedo = packedData.g * DebrisColor;
    OutMossRelief = packedData.b;
    OutDebrisRelief = packedData.a;
}

#endif // FOLIAGE_BLEND_INCLUDED
