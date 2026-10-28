Shader "Deepsea/StylizedSurface"
{
    Properties
    {
        _BaseColor("Colour", Color) = (0.4,0.5,0.5,1)
        _Color("Fallback colour", Color) = (0.4,0.5,0.5,1)
        _BaseMap("Base", 2D) = "white" {}
        _Cutoff("Cutoff", Float) = 0.5
        _Glow("Window emission", Range(0,2)) = 0
        _Smoothness("Wetness", Range(0,1)) = 0.3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Glow;
                float _Smoothness;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float3 positionWS:TEXCOORD1; float fog:TEXCOORD2; };
            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionWS=TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS=TransformWorldToHClip(o.positionWS);
                o.normalWS=TransformObjectToWorldNormal(v.normalOS);
                o.fog=ComputeFogFactor(o.positionCS.z);
                return o;
            }
            half4 frag(Varyings i):SV_Target
            {
                float3 normal=normalize(i.normalWS);
                Light sun=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float ndl=saturate(dot(normal,sun.direction));
                float light=.42+ndl*.65*sun.shadowAttenuation;
                float3 view=normalize(GetWorldSpaceViewDir(i.positionWS));
                float sheen=pow(saturate(dot(normal,normalize(sun.direction+view))),36)*_Smoothness*.32;
                float3 colour=_BaseColor.rgb*light*lerp(float3(.8,.95,1.03),sun.color,.65)+sheen*sun.color;
                colour+=_BaseColor.rgb*_Glow;
                return half4(MixFog(colour,i.fog),1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            float4 _BaseColor;
            float _Glow,_Smoothness;
            float3 _DeepseaLightDirection;
            struct v2f{float4 pos:SV_POSITION;float3 n:TEXCOORD0;UNITY_FOG_COORDS(1)};
            v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.n=UnityObjectToWorldNormal(v.normal);UNITY_TRANSFER_FOG(o,o.pos);return o;}
            fixed4 frag(v2f i):SV_Target{float l=.42+saturate(dot(normalize(i.n),_DeepseaLightDirection))*.65;fixed4 c=fixed4(_BaseColor.rgb*(l+_Glow),1);UNITY_APPLY_FOG(i.fogCoord,c);return c;}
            ENDCG
        }
    }
}
