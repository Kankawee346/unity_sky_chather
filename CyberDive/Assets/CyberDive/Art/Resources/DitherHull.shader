Shader "CyberDive/DitherHull"
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
   struct A {float4 positionOS:POSITION;float3 normal:NORMAL;};
   struct V {float4 positionCS:SV_POSITION;float3 normal:TEXCOORD0;};
   V vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.normal=TransformObjectToWorldNormal(i.normal);return o;}
   half4 frag(V i):SV_Target
   {
    float light=saturate(dot(normalize(i.normal),normalize(float3(-.4,.8,-.7))));
    float2 cell=floor(i.positionCS.xy/2);
    float noise=frac(sin(dot(cell,float2(12.9898,78.233)))*43758.5453);
    float brightness=.06+light*.18;
    return half4(noise<brightness?.7:.008,noise<brightness?.7:.008,noise<brightness?.7:.008,1);
   }
   ENDHLSL
  }
 }
}
