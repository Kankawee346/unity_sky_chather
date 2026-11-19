Shader "CyberDive/VectorUnlit"
{
 Properties { _BaseColor("Color",Color)=(1,1,1,1) }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
  Pass
  {
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseColor;
   CBUFFER_END
   struct A {float4 positionOS:POSITION;};
   struct V {float4 positionCS:SV_POSITION;};
   V vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);return o;}
   half4 frag(V i):SV_Target{return _BaseColor;}
   ENDHLSL
  }
 }
}
