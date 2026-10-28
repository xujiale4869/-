Shader "Deepsea/ShowcaseFoam" {
 Properties{_Tint("Foam tint",Color)=(.74,.85,.8,.65)}
 SubShader {Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 Pass{HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 float4 _Tint;
 struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;float2 uv:TEXCOORD1;};
 V Vert(A a){V o;o.w=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.w);o.uv=a.uv;return o;}
 half4 Frag(V i):SV_Target{float phase=_Time.y*.7;float noise=.5+.24*sin(i.w.x*13+i.w.z*7+phase)+.21*sin(i.w.z*17-i.w.x*5-phase*.8);float patch=smoothstep(.28,.68,noise);float pulse=.6+.4*sin(_Time.y*1.1+i.w.x*.3+i.w.z*.2);float a=pow(saturate(1-i.uv.y),1.6)*patch*pulse*_Tint.a;if(_WorldSpaceCameraPos.y<-.22)a=0;return half4(_Tint.rgb,a);}
 ENDHLSL}
 }
}
