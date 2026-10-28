Shader "Deepsea/Ocean"
{
    Properties
    {
        _DeepColor("Deep blue grey",Color)=(0.055,0.18,0.22,1)
        _ShallowColor("Shallow water",Color)=(0.19,0.37,0.37,1)
        _WaveHeight("Swell",Float)=0.15
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _DeepColor,_ShallowColor;
                float _WaveHeight;
            CBUFFER_END
            struct Attributes{float4 positionOS:POSITION;};
            struct Varyings{float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float fog:TEXCOORD1;};
            Varyings vert(Attributes v)
            {
                Varyings o;float3 p=TransformObjectToWorld(v.positionOS.xyz);
                p.y+=sin(p.x*.13+p.z*.08+_Time.y*.8)*_WaveHeight*.5;
                p.y+=sin(p.z*.19-p.x*.07-_Time.y*.6)*_WaveHeight*.3;
                o.world=p;o.positionCS=TransformWorldToHClip(p);o.fog=ComputeFogFactor(o.positionCS.z);return o;
            }
            half4 frag(Varyings i):SV_Target
            {
                float2 p=i.world.xz;
                float shoal=exp(-dot(p-float2(58,61),p-float2(58,61))*.00065)*.68;
                shoal+=exp(-dot(p-float2(0,-45),p-float2(0,-45))*.0012)*.62;
                float longWave=sin(p.x*.13+p.y*.08+_Time.y*.8);
                float crossWave=sin(p.y*.19-p.x*.07-_Time.y*.6);
                float crest=pow(saturate(sin(p.x*.5+p.y*.27+sin(p.y*.14-_Time.y*.5)*1.8)),26);
                float3 c=lerp(_DeepColor.rgb,_ShallowColor.rgb,saturate(shoal));
                c+=float3(.038,.064,.067)*(longWave*.35+crossWave*.3);
                c+=float3(.14,.19,.18)*crest*.16;
                float3 n=normalize(float3(-cos(p.x*.13+p.y*.08+_Time.y*.8)*.045,1,-cos(p.y*.19-p.x*.07-_Time.y*.6)*.04));
                float3 view=normalize(GetWorldSpaceViewDir(i.world));
                float highlight=pow(saturate(dot(n,normalize(view+float3(-.45,.65,.35)))),85);
                c+=float3(.65,.65,.52)*highlight*.25;
                return half4(MixFog(c,i.fog),1);
            }
            ENDHLSL
        }
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
            float4 _DeepColor,_ShallowColor;float _WaveHeight;
            struct v2f{float4 pos:SV_POSITION;float3 w:TEXCOORD0;UNITY_FOG_COORDS(1)};
            v2f vert(appdata_base v){v2f o;float3 p=mul(unity_ObjectToWorld,v.vertex).xyz;p.y+=sin(p.x*.13+p.z*.08+_Time.y*.8)*_WaveHeight*.5;o.w=p;o.pos=mul(UNITY_MATRIX_VP,float4(p,1));UNITY_TRANSFER_FOG(o,o.pos);return o;}
            fixed4 frag(v2f i):SV_Target{float2 p=i.w.xz;float shoal=exp(-dot(p-float2(58,61),p-float2(58,61))*.00065)*.68+exp(-dot(p-float2(0,-45),p-float2(0,-45))*.0012)*.62;float wave=sin(p.x*.13+p.y*.08+_Time.y*.8);fixed4 c=fixed4(lerp(_DeepColor.rgb,_ShallowColor.rgb,saturate(shoal))+float3(.02,.035,.035)*wave,1);UNITY_APPLY_FOG(i.fogCoord,c);return c;}
            ENDCG
        }
    }
}
