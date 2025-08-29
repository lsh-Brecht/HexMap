Shader "Custom/Road_VF" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Noise Texture", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }

    SubShader {
Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }
ZWrite On
Offset -2, -2
        LOD 200

        Pass {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Color;
            half _Glossiness;
            half _Metallic;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float4 frag (v2f i) : SV_Target {
                // world position 기반 UV
                float2 worldUV = i.worldPos.xz * 0.025;
                float4 noise = tex2D(_MainTex, worldUV);

                // 기본 색상과 노이즈 결합
                float4 c = _Color * (noise.y * 0.75 + 0.25);

                // blend factor (원래 uv.x 기반)
                float blend = i.uv.x;
                blend *= noise.x + 0.5;
                blend = smoothstep(0.4, 0.7, blend);

                // 최종 출력
                c.a = blend;   // 알파는 blend로
                return c;
            }
            ENDHLSL
        }
    }
}
