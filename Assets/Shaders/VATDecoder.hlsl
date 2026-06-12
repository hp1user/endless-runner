#ifndef VAT_DECODER_INCLUDED
#define VAT_DECODER_INCLUDED

void DecodeVAT_float(
    UnityTexture2D VAT_Texture,
    UnitySamplerState Sampler_State,
    float2 UV2,
    float TotalVertices,
    float CurrentFrame,
    float QuadrantID,
    float AnimID,
    float TotalAnims,
    out float3 PositionOffset,
    out float VertexVelocity
)
{
    int qID = (int)QuadrantID;
    int aID = (int)AnimID;
    int tAnims = (int)TotalAnims;

    // 1. Calculate Quadrant pixel offset
    float offsetX = 0.0;
    float offsetY = 0.0;

    if (qID == 0) // Top-Left
    {
        offsetX = 0.0;
        offsetY = 512.0;
    }
    else if (qID == 1) // Top-Right
    {
        offsetX = 512.0;
        offsetY = 512.0;
    }
    else if (qID == 2) // Bottom-Left
    {
        offsetX = 0.0;
        offsetY = 0.0;
    }
    else if (qID == 3) // Bottom-Right
    {
        offsetX = 512.0;
        offsetY = 0.0;
    }

    // 2. Calculate horizontal coordinate (U) using baked UV2 with half-texel offset
    float localX = round(UV2.x * TotalVertices);
    float u = (offsetX + localX + 0.5) / 1024.0;

    // 3. Calculate frames per animation (splitting the 512 rows)
    float framesPerAnim = 512.0 / max(1.0, (float)tAnims);

    // 4. Handle time wrapping within the current animation range
    float localFrame = fmod(CurrentFrame, framesPerAnim);
    if (localFrame < 0.0) 
    {
        localFrame += framesPerAnim;
    }

    // 5. Interpolate between current frame (floor) and next frame (ceil)
    float frameFloor = floor(localFrame);
    float frameCeil = fmod(frameFloor + 1.0, framesPerAnim);
    float t = frac(localFrame);

    // 6. Map to global rows inside the selected quadrant
    float globalFrameFloor = (float)aID * framesPerAnim + frameFloor;
    float globalFrameCeil = (float)aID * framesPerAnim + frameCeil;

    // 7. Calculate vertical coordinates (V) with half-texel offset
    float v_floor = (offsetY + globalFrameFloor + 0.5) / 1024.0;
    float v_ceil = (offsetY + globalFrameCeil + 0.5) / 1024.0;

    // 8. Sample displacements (RGB) and magnitude/velocity (Alpha)
    float4 sampleFloor = VAT_Texture.tex.SampleLevel(Sampler_State.samplerstate, float2(u, v_floor), 0);
    float4 sampleCeil = VAT_Texture.tex.SampleLevel(Sampler_State.samplerstate, float2(u, v_ceil), 0);

    // 9. Blend frame transitions
    float4 finalSample = lerp(sampleFloor, sampleCeil, t);

    // Outputs
    PositionOffset = finalSample.rgb;
    VertexVelocity = finalSample.a;
}

#endif // VAT_DECODER_INCLUDED
