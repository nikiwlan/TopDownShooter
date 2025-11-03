Shader "Custom/PortalGlow"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Glow Color", Color) = (1, 0, 1, 1)
        _Intensity("Glow Intensity", Range(0,5)) = 2
        _Speed("Wave Speed", Range(0,5)) = 1
        _Distortion("Distortion Strength", Range(0,1)) = 0.2
    }
        SubShader
        {
            Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
            LOD 100
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                sampler2D _MainTex;
                float4 _Color;
                float _Intensity;
                float _Speed;
                float _Distortion;

                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                };

                struct v2f
                {
                    float2 uv : TEXCOORD0;
                    float4 vertex : SV_POSITION;
                };

                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    float wave = sin(_Time.y * _Speed + i.uv.y * 10) * _Distortion;
                    float2 uv = i.uv + wave;

                    fixed4 col = tex2D(_MainTex, uv) * _Color;
                    col.rgb *= _Intensity;
                    col.a = _Color.a;
                    return col;
                }
                ENDCG
            }
        }
}
