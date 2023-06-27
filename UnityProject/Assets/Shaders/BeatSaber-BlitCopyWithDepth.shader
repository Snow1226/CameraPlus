// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "BeatSaber/BlitCopyWithDepth" {
    Properties
    {
        _MainTex ("Texture", any) = "" {}
        _Color("Multiplicative color", Color) = (1.0, 1.0, 1.0, 1.0)
        [MaterialToggle] _IsVRCameraOnly ("Is VR Camera Only", Float) = 1
        [HideInInspector] _CullMode ("_CullMode", Float) = 2.0
    }
    SubShader {
        Tags {"Queue" = "Geometry" }
        Pass{
            ZTest Less
            Cull[_CullMode]
            Blend Off 
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            sampler2D _MainTex;

            uniform float4 _MainTex_ST;
            uniform float4 _Color;
            float _IsVRCameraOnly;

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            bool IsVRCamera() {
#if defined(UNITY_SINGLE_PASS_STEREO)
                return true;
#endif
                return false;
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                if(_IsVRCameraOnly == 1)
                    o.vertex.xyz *= IsVRCamera();
                o.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.texcoord) * _Color;
            }
            ENDCG

        }

    }
    Fallback Off
}