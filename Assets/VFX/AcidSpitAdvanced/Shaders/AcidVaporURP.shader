Shader "AcidSpitAdvanced/SparseVaporURP"
{
 Properties { _BaseColor("Tint",Color)=(0.48,0.72,0.08,0.16) }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  Pass
  {
   Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
    half4 _BaseColor;
   CBUFFER_END
   struct A { float4 p:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
   struct V { float4 p:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
   V vert(A i) { V o; o.p=TransformObjectToHClip(i.p.xyz); o.uv=i.uv; o.color=i.color; return o; }
   half4 frag(V i):SV_Target { half d=length(i.uv*2-1); half a=saturate(1-d); return half4(_BaseColor.rgb*i.color.rgb,_BaseColor.a*i.color.a*a*a); }
   ENDHLSL
  }
 }
}
