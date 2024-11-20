Shader "Effect/Wipe"
{
    Properties{
        _Progress("Progress", Float) = 0
        _MainTex("MainTex", 2D) = "" {}
        _Color("Color", Color) = (0, 0, 0, 0)
        _Cutoff("Cutoff", Range(0, 2)) = 2.0
        _Center("Center", Vector) = (0,0,0,0)
    }
    SubShader{
        Tags { "RenderType" = "AlphaTest" "Queue" = "AlphaTest" }

        //Circle Wipe
        Pass {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            #pragma target 3.0

            float _Progress;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed _Cutoff;
            fixed4 _Center;

            fixed4 frag(v2f_img i) : COLOR {
                fixed4 c = tex2D(_MainTex, i.uv);
                i.uv -= fixed2(0.5f + _Center.x, 0.5f - _Center.y);
                float4 projectionSpaceUpperRight = float4(1, 1, UNITY_NEAR_CLIP_VALUE, _ProjectionParams.y);
                float4 viewSpaceUpperRight = mul(unity_CameraInvProjection, projectionSpaceUpperRight);
                i.uv.x *= viewSpaceUpperRight.x / viewSpaceUpperRight.y;
                if (distance(i.uv, fixed2(0, 0)) < 1 - _Progress) {
                    return c;
                }
                clip(c.a - _Cutoff);
                return _Color;
            }
            ENDCG
        }

        //Left to Right Wipe
        Pass {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            #pragma target 3.0

            float _Progress;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed _Cutoff;

            fixed4 frag(v2f_img i) : COLOR
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                if (1 - i.uv.x < 1 - _Progress)
                    return c;
                clip(c.a - _Cutoff);
                return _Color;
            }
            ENDCG
        }
        //Rigth to Left Wipe
        Pass {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            #pragma target 3.0

            float _Progress;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed _Cutoff;

            fixed4 frag(v2f_img i) : COLOR
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                if (i.uv.x < 1- _Progress)
                    return c;
                clip(c.a - _Cutoff);
                return _Color;
            }
            ENDCG
        }

        //Top to Bottom Wipe
        Pass {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            #pragma target 3.0

            float _Progress;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed _Cutoff;

            fixed4 frag(v2f_img i) : COLOR
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                if (1 - i.uv.y > _Progress)
                    return c;
                clip(c.a - _Cutoff);
                return _Color;
            }
            ENDCG
        }

        //Bottom to Top Wipe
        Pass {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            #pragma target 3.0

            float _Progress;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed _Cutoff;

            fixed4 frag(v2f_img i) : COLOR
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                if (i.uv.y > _Progress)
                    return c;
                clip(c.a - _Cutoff);
                return _Color;
            }
            ENDCG
        }
    }
}
