Shader "AcidSpitAdvanced/PuddleURP"
{
 Properties
 {
  _AcidColor("Acid",Color)=(0.21,0.85,0.07,1)
  _HotColor("Hot acid",Color)=(0.898,1,0.333,1)
  _EdgeColor("Corrosive edge",Color)=(0.018,0.12,0.025,1)
  _BubbleSpeed("Bubble speed",Float)=1.6
  _BubbleScale("Bubble count",Range(1,12))=5
  _FlowSpeed("Flow speed",Float)=1.2
  _EmissionIntensity("Emission",Range(0,1))=0.15
  _FadeAmount("Fade",Range(0,1))=0
 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+5" }
  Pass
  {
   Tags { "LightMode"="UniversalForward" }
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
    half4 _AcidColor,_HotColor,_EdgeColor;
    float _BubbleSpeed,_BubbleScale,_FlowSpeed,_EmissionIntensity,_FadeAmount;
   CBUFFER_END
   struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
   struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float radial:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
   V vert(A i) { V o; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.uv=i.uv; o.radial=i.color.r; return o; }
   half4 frag(V i):SV_Target
   {
    float2 p=(i.uv-0.5)*2; float t=_Time.y*_FlowSpeed;
    float flow=sin(p.x*9+sin(p.y*6+t))*cos(p.y*8-t*0.7);
    half edge=smoothstep(0.72,0.98,i.radial+flow*0.025);
    half3 col=lerp(_AcidColor.rgb,_HotColor.rgb,smoothstep(0.0,0.8,flow)*0.72);
    float2 grid=p*_BubbleScale;
    float2 cell=floor(grid); float seed=frac(sin(dot(cell,float2(127.1,311.7)))*43758.5453);
    float phase=frac(_Time.y*_BubbleSpeed*0.55+seed);
    float2 offset=float2(sin(seed*37),cos(seed*19))*0.18;
    float r=length(frac(grid)-0.5-offset);
    float ring=(1-smoothstep(0.018,0.048,abs(r-(0.05+phase*0.27))))*sin(phase*3.14159);
    col=lerp(col,_HotColor.rgb,ring*0.8*(1-edge));
    col=lerp(col,_EdgeColor.rgb,edge);
    col*=1+_EmissionIntensity*(0.7+0.3*sin(t*2));
    // Opaque erosion toward the outline plus transform shrink: no alpha sorting/overdraw.
    clip(1-_FadeAmount-i.radial*0.12);
    return half4(col,1);
   }
   ENDHLSL
  }
 }
}
