Shader "Deepsea/SoftLine"
{
    Properties { _Color("Tint",Color)=(1,1,1,1) }
    SubShader
    {
        Tags { "Queue"="Transparent+20" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color;
            struct a2v{float4 vertex:POSITION;float4 color:COLOR;};
            struct v2f{float4 vertex:SV_POSITION;float4 color:COLOR;};
            v2f vert(a2v v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.color=v.color*_Color;return o;}
            fixed4 frag(v2f i):SV_Target{return i.color;}
            ENDCG
        }
    }
}
