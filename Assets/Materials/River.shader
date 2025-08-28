Shader "Custom/RiverTest" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
    }
    SubShader {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _Color;

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float2 uv = i.uv;
                uv.x = uv.x * 0.0625 + _Time.y * 0.005;
                uv.y -= _Time.y * 0.25;
                float4 noise = tex2D(_MainTex, uv);

                float2 uv2 = i.uv;
                uv2.x = uv2.x * 0.0625 - _Time.y * 0.0052;
                uv2.y -= _Time.y * 0.23;
                float4 noise2 = tex2D(_MainTex, uv2);

                fixed4 c = saturate(_Color + noise.r * noise2.a);
                return c;
            }
            ENDCG
        }
    }
}