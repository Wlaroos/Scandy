Shader "Custom/XRayEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Tint ("X-Ray Tint", Color) = (0.4,0.9,1,1)
        _Intensity ("Base Intensity", Range(0,4)) = 1.2
        _EdgeIntensity ("Edge Intensity", Range(0,8)) = 3.0
        _EdgeSpread ("Edge Spread (texels)", Range(0.5,5)) = 1.0
        _Threshold ("Edge Threshold", Range(0,1)) = 0.02
        _NoiseIntensity ("Noise Intensity", Range(0,1)) = 0.12
        _NoiseScale ("Noise Scale", Range(1,1024)) = 256
        _ScanlineIntensity ("Scanline Intensity", Range(0,1)) = 0.06
        _ScanlineDensity ("Scanline Density", Range(50,800)) = 300
        _Flicker ("Flicker Amount", Range(0,1)) = 0.04
        _Alpha ("Output Alpha", Range(0,1)) = 1
        [Toggle] _Invert ("Invert X-Ray (negative)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            float4 _Tint;
            float _Intensity;
            float _EdgeIntensity;
            float _EdgeSpread;
            float _Threshold;
            float _NoiseIntensity;
            float _NoiseScale;
            float _ScanlineIntensity;
            float _ScanlineDensity;
            float _Flicker;
            float _Alpha;
            float _Invert;

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
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            // simple hash / noise
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 78.233);
                return frac(p.x * p.y);
            }

            float luminance(float3 c)
            {
                return dot(c, float3(0.299,0.587,0.114));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // read source
                fixed4 src = tex2D(_MainTex, uv);
                float3 srcCol = src.rgb;
                float srcLum = luminance(srcCol);

                // --- Edge detection (3x3 Sobel) ---
                float2 ts = _MainTex_TexelSize.xy * _EdgeSpread;
                float2 o00 = uv + float2(-ts.x, -ts.y);
                float2 o01 = uv + float2(0, -ts.y);
                float2 o02 = uv + float2(ts.x, -ts.y);
                float2 o10 = uv + float2(-ts.x, 0);
                float2 o11 = uv;
                float2 o12 = uv + float2(ts.x, 0);
                float2 o20 = uv + float2(-ts.x, ts.y);
                float2 o21 = uv + float2(0, ts.y);
                float2 o22 = uv + float2(ts.x, ts.y);

                float l00 = luminance(tex2D(_MainTex, o00).rgb);
                float l01 = luminance(tex2D(_MainTex, o01).rgb);
                float l02 = luminance(tex2D(_MainTex, o02).rgb);
                float l10 = luminance(tex2D(_MainTex, o10).rgb);
                float l11 = luminance(tex2D(_MainTex, o11).rgb);
                float l12 = luminance(tex2D(_MainTex, o12).rgb);
                float l20 = luminance(tex2D(_MainTex, o20).rgb);
                float l21 = luminance(tex2D(_MainTex, o21).rgb);
                float l22 = luminance(tex2D(_MainTex, o22).rgb);

                float gx = -l00 - 2.0*l10 - l20 + l02 + 2.0*l12 + l22;
                float gy = -l00 - 2.0*l01 - l02 + l20 + 2.0*l21 + l22;
                float edge = sqrt(gx*gx + gy*gy);

                // edge mask (apply threshold to reduce noise)
                float edgeMask = saturate((edge - _Threshold) * _EdgeIntensity * 4.0);

                // --- Base X-ray color from luminance ---
                float3 xbase = _Tint.rgb * (srcLum * _Intensity);

                // --- add edge highlight (bright along edges) ---
                float3 edgeCol = _Tint.rgb * edgeMask;

                // --- Noise ---
                float n = hash21(uv * _NoiseScale + float2(_Time.y * 1.5, _Time.y * 0.37));
                float3 noise = (n - 0.5) * _NoiseIntensity;

                // --- Scanlines ---
                float scan = sin((uv.y * _ScanlineDensity + _Time.y * 3.0) * 3.14159);
                scan = (scan * 0.5) + 0.5;
                float3 scanCol = 1.0 - _ScanlineIntensity * (1.0 - scan);

                // --- Flicker (low frequency) ---
                float flick = 1.0 + sin(_Time.y * 1.2) * _Flicker * (0.5 + 0.5*hash21(float2(uv.x*12.3, uv.y*7.7)));

                // compose final color
                float3 col = (xbase + edgeCol) * scanCol;
                col += noise;
                col *= flick;

                // optional invert (negative)
                if (_Invert > 0.5)
                    col = 1.0 - saturate(col);

                // premultiply alpha using user alpha and source alpha
                float outA = saturate(_Alpha * src.a);

                return fixed4(saturate(col), outA);
            }
            ENDCG
        }
    }

    FallBack "Unlit/Transparent"
}