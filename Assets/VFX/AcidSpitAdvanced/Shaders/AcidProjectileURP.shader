Shader "AcidSpitAdvanced/ProjectileURP"
{
 Properties
 {
  _BaseColor("Toxic body",Color)=(0.467,1,0.129,1)
  _HotColor("Acid hot spots",Color)=(0.898,1,0.333,1)
  _DarkColor("Chemical contours",Color)=(0.071,0.42,0.137,1)
  _EmissionStrength("Emission",Range(0,1))=0.15
  _RimPower("Rim power",Range(1,8))=3
  _RimIntensity("Rim intensity",Range(0,1))=0.25
  _WobbleStrength("Wobble",Range(0,0.3))=0.09
  _WobbleSpeed("Wobble speed",Float)=5
  _NoiseScale("Noise scale",Float)=4
  _FlowSpeed("Flow speed",Float)=2
  _GlossStrength("Gloss",Range(0,2))=0.9
  _RippleIntensity("Ripple intensity",Range(0,1))=0.35
 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
  Pass
  {
   Name "AcidForward"
   Tags { "LightMode"="UniversalForward" }
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   CBUFFER_START(UnityPerMaterial)
    half4 _BaseColor,_HotColor,_DarkColor;
    float _EmissionStrength,_RimPower,_RimIntensity,_WobbleStrength,_WobbleSpeed,_NoiseScale,_FlowSpeed,_GlossStrength,_RippleIntensity;
   CBUFFER_END
   struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
   struct V { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; half3 normalWS:TEXCOORD1; float3 local:TEXCOORD2; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };
   V vert(A i)
   {
    V o; UNITY_SETUP_INSTANCE_ID(i); UNITY_TRANSFER_INSTANCE_ID(i,o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    float3 p=i.positionOS.xyz; float t=_Time.y*_WobbleSpeed;
    float wave=sin(p.y*4+p.z*3+t)*cos(p.x*3-t*0.73);
    float ripple=sin(dot(p,float3(13,9,11))+t*1.71)*_RippleIntensity*0.3;
    p+=i.normalOS*(wave+ripple)*_WobbleStrength;
    p.xy*=1+0.025*sin(t*1.13); p.x+=sin(p.z*5+t*0.81)*_WobbleStrength*0.35;
    o.positionWS=TransformObjectToWorld(p); o.positionCS=TransformWorldToHClip(o.positionWS);
    o.normalWS=TransformObjectToWorldNormal(i.normalOS); o.local=p; return o;
   }
   half4 frag(V i):SV_Target
   {
    UNITY_SETUP_INSTANCE_ID(i);
    float t=_Time.y*_FlowSpeed; float3 p=i.local*_NoiseScale;
    half flow=sin(p.x*1.7+sin(p.z*1.4-t)+t*0.4)*cos(p.y*2.1-p.z*0.8+t*0.71);
    half vein=smoothstep(-0.65,-0.08,flow);
    half hot=smoothstep(0.5,0.92,flow);
    half3 col=lerp(_DarkColor.rgb,_BaseColor.rgb,vein);
    col=lerp(col,_HotColor.rgb,hot);
    half3 n=normalize(i.normalWS); half3 v=GetWorldSpaceNormalizeViewDir(i.positionWS);
    Light light=GetMainLight(); half ndl=dot(n,light.direction)*0.5+0.5;
    half band=ndl>0.72?1.0:(ndl>0.35?0.77:0.48);
    half3 h=SafeNormalize(light.direction+v);
    half gloss=smoothstep(0.85,0.97,dot(n,h)+flow*0.018)*_GlossStrength;
    half rim=pow(1-saturate(dot(n,v)),_RimPower)*_RimIntensity;
    col=col*band+_HotColor.rgb*(gloss+rim+hot*_EmissionStrength);
    return half4(col,1);
   }
   ENDHLSL
  }
 }
}
