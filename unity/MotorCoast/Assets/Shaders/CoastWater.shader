Shader "MotorCoast/CoastWater" {
 Properties { _DeepColor("Deep water",Color)=(0.018,0.12,0.16,1) _ShallowColor("Fresnel",Color)=(0.16,0.39,0.45,1) }
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
  Pass {
   Name "Water"
   Tags { "LightMode"="UniversalForward" }
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _DeepColor, _ShallowColor;
   CBUFFER_END
   struct Attributes {float4 positionOS:POSITION;};
   struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;};
   Varyings vert(Attributes i) { Varyings o;o.positionWS=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);return o; }
   half4 frag(Varyings i):SV_Target {
    float t=_Time.y;float2 p=i.positionWS.xz;
    half3 n=normalize(half3(sin(p.x*.62+t*.7)*.065+cos(p.y*.23-t*.5)*.04,1,cos(p.y*.44+t*.52)*.065));
    half3 v=SafeNormalize(GetWorldSpaceViewDir(i.positionWS));half f=pow(1-saturate(dot(n,v)),4);
    Light light=GetMainLight();half spec=pow(saturate(dot(n,normalize(v+light.direction))),180)*1.4;
    half3 environment=GlossyEnvironmentReflection(reflect(-v,n),.16,1);
    half3 col=lerp(_DeepColor.rgb,_ShallowColor.rgb,f*.55)+environment*f*.55+light.color*spec;
    return half4(col,1);
   }
   ENDHLSL
  }
 }
}
