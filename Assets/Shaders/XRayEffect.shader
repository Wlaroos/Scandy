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

        // speck / imperfection controls
        _SpeckIntensity ("Speck Intensity", Range(0,1)) = 0.6
        _SpeckScale ("Speck Scale (cells per UV)", Range(1,128)) = 16
        _SpeckDensity ("Speck Density (chance)", Range(0,1)) = 0.06
        _SpeckSeed ("Speck Seed", Float) = 17
        [Toggle] _SpeckAnimate ("Speck Animate (time)", Float) = 1
        [Toggle] _SpeckUseWorldPos ("Specks use World Pos", Float) = 0
        [Toggle] _SpeckInvertShape ("Invert Speck Shape", Float) = 0
        _SpeckColor ("Speck Color", Color) = (1,1,1,1)
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

            // speck variables (must match Properties)
            float _SpeckIntensity;
            float _SpeckScale;
            float _SpeckDensity;
            float _SpeckSeed;
            float _SpeckAnimate;
            float _SpeckUseWorldPos;
            float _SpeckInvertShape;
            float4 _SpeckColor;
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                // pass world pos if user wants world-based specks
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
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

                // --- Speck imperfections (cell-based, multiple shapes) ---
                float2 speckCoords;
                if (_SpeckUseWorldPos > 0.5)
                {
                    speckCoords = i.worldPos.xz * 0.1 * _SpeckScale;
                }
                else
                {
                    speckCoords = uv * _SpeckScale;
                    if (_SpeckAnimate > 0.5)
                        speckCoords += float2(_Time.y * 0.02, _Time.y * 0.013) * _SpeckScale;
                }

                float2 cell = floor(speckCoords);
                float2 local = frac(speckCoords) - 0.5; // center at 0 (-0.5..0.5)
                float h = hash21(cell + _SpeckSeed);
                float hasSpeck = step(h, _SpeckDensity); // 1 if h < density

                // normalized local coords in -1..1
                float2 p = local * 2.0;

                // shape choice per-cell
                float shapePick = hash21(cell + float2(_SpeckSeed, 7.3));
                int shapeID = (int)floor(shapePick * 6.0); // 0..5 shapes

                // common size/thickness controls
                float baseSize = 0.35; // tune per-shape
                // per-cell random size & thickness (0..1 from hash21)
                float sizeSeed = hash21(cell + float2(11.7, 4.2));
                float thicknessSeed = hash21(cell + float2(21.3, 9.1));
                // map seeds to useful ranges (tweak min/max as desired)
                float sizeJitter = lerp(0.6, 1.4, sizeSeed);      // 60%..140% of base
                float thicknessJitter = lerp(0.6, 1.6, thicknessSeed); // 60%..160% of base
                float size = baseSize * sizeJitter;
                float thickness = 0.12 * thicknessJitter;

                // rotation for oriented shapes
                float ang = hash21(cell + float2(2.1, 9.3)) * 6.2831853;
                float ca = cos(ang), sa = sin(ang);
                float2 rp = float2(ca * p.x - sa * p.y, sa * p.x + ca * p.y);

                // shape masks (1=center, 0=outside)
                float circle = 1.0 - smoothstep(size, size * 0.65, length(p));
                float square = 1.0 - smoothstep(size, size * 0.65, max(abs(p.x), abs(p.y)));
                float thinLine = 1.0 - smoothstep(thickness, thickness * 0.7, abs(rp.y)); // thin line oriented by ang
                float plus = (1.0 - smoothstep(thickness, thickness * 0.7, abs(p.x))) + (1.0 - smoothstep(thickness, thickness * 0.7, abs(p.y)));
                      plus = saturate(plus); // combine hor+vert
                float xcross = (1.0 - smoothstep(thickness, thickness * 0.7, abs(rp.x))) * (1.0 - smoothstep(thickness, thickness * 0.7, abs(rp.y)));
                      // blob: use small cell-local noise for an irregular spot
                float blob = 0.0;
                {
                    // sample a few hash offsets for lumpy look
                    float a = hash21(cell + p.xy * 13.7);
                    float b = hash21(cell + p.yx * 7.1);
                    float lump = (a * 0.6 + b * 0.4);
                    float r = length(p) * (0.9 + 0.6 * (lump - 0.5));
                    blob = 1.0 - smoothstep(size * 1.1, size * 0.7, r);
                }

                // pick mask by shapeID
                float shapeMask = 0.0;
                if (shapeID == 0) shapeMask = circle;
                else if (shapeID == 1) shapeMask = square;
                else if (shapeID == 2) shapeMask = plus;
                else if (shapeID == 3) shapeMask = xcross;
                else if (shapeID == 4) shapeMask = thinLine;
                else shapeMask = blob;

                // final speck mask: fade out toward the cell border
                float edgeFade = smoothstep(0.49, 0.45, max(abs(local.x), abs(local.y)));

                // allow flipping the shape (if you previously had shapes cut out of squares)
                float shapeFinal = lerp(shapeMask, 1.0 - shapeMask, _SpeckInvertShape);

                // only apply speck inside chosen cells and inside the shape
                float speckMask = hasSpeck * edgeFade * shapeFinal;

                // speck darkness factor (0 = no effect, 1 = full dark)
                float speckDark = saturate(speckMask * _SpeckIntensity);

                // --- Compose final color (before transparency) ---
                float3 baseCol = xbase * scanCol * flick + edgeCol + noise;

                // apply invert only to the base X-ray/edge/noise
                float3 baseAfterInvert = (_Invert > 0.5) ? (1.0 - baseCol) : baseCol;

                // speck color (white by default)
                float3 speckCol = _SpeckColor.rgb * (speckMask * _SpeckIntensity);

                // combine base + specks
                float3 composed = baseAfterInvert + speckCol;

                // use sprite alpha so the shader follows sprite transparency
                float srcA = src.a;

                // optional: skip pixels that are effectively transparent
                if (srcA <= 0.01)
                    discard;

                // modulate final RGB by the sprite alpha and output alpha by sprite alpha * _Alpha
                float3 outRgb = composed * srcA;
                float outA = srcA * _Alpha;

                return float4(saturate(outRgb), outA);
            }
            ENDCG
        }
    }

    FallBack "Unlit/Transparent"
}