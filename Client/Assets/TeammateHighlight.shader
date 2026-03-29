Shader "MiyakoCarryService/TeammateHighlight"
{
    Properties
    {
        _HighlightColor("Highlight Color", Color) = (0, 0, 1, 0.2)
        [Enum(UnityEngine.Rendering.CompareFunction)] _HighlightZTest("Highlight ZTest", Float) = 8
        _HighlightOutlinesWidth("Highlight Outlines Width", Range(0.0, 2.0)) = 0.15
    }

    SubShader {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }

        Pass {
            Name "OCCLUDED_PASS"
            ZTest Always
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragHighlight
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f {
                float4 pos : SV_POSITION;
            };

            uniform float4 _HighlightColor;
            uniform float _HighlightOutlinesWidth;

            v2f vert(appdata v) {
                appdata original = v;

                float3 scaleDir = normalize(v.vertex.xyz - float4(0, 0, 0, 1));
                if (degrees(acos(dot(scaleDir.xyz, v.normal.xyz))) > 89) {
                    v.vertex.xyz += normalize(v.normal.xyz) * _HighlightOutlinesWidth;
                }
                else {
                    v.vertex.xyz += scaleDir * _HighlightOutlinesWidth;
                }

                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            half4 fragHighlight(v2f i) : SV_Target {
                return _HighlightColor;
            }
            ENDCG
        }

    }

    Fallback "Diffuse"
}