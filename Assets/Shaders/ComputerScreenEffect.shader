Shader "Custom/ComputerScreenEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Tint ("Tint Color", Color) = (0.6,1,0.5,1)
        _Brightness ("Brightness", Range(0,2)) = 1
        _Monochrome ("Monochrome Amount", Range(0,1)) = 0.85
        _ScanlineDensity ("Scanline Density", Range(50,800)) = 300
        _ScanlineIntensity ("Scanline Intensity", Range(0,1)) = 0.12
        _NoiseIntensity ("Noise Intensity", Range(0,1)) = 0.06
        _FlickerAmount ("Flicker Amount", Range(0,1)) = 0.06
        _FlickerSpeed ("Flicker Speed", Range(0.1,10)) = 1.2
        _Vignette ("Vignette Strength", Range(0,1)) = 0.5
        [Toggle] _Greyscale ("Greyscale", Float) = 0
        _Alpha ("Output Alpha", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
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
            fixed4 _Tint;
            float _Brightness;
            float _Monochrome;
            float _ScanlineDensity;
            float _ScanlineIntensity;
            float _NoiseIntensity;
            float _FlickerAmount;
            float _FlickerSpeed;
            float _Vignette;
            float _Greyscale;
            float _Alpha;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            // cheap pseudo-random
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 78.233);
                return frac(p.x * p.y);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                fixed4 src = tex2D(_MainTex, uv);

                // monochrome latch (simulate phosphor)
                float lum = dot(src.rgb, float3(0.299,0.587,0.114));
                float3 mono = lum * _Tint.rgb;
                float3 color = lerp(src.rgb, mono, saturate(_Monochrome));

                // scanlines
                float scan = sin((uv.y * _ScanlineDensity + _Time.y * 0.5) * 3.14159);
                scan = (scan * 0.5) + 0.5; // 0..1
                color *= lerp(1.0 - _ScanlineIntensity, 1.0 + _ScanlineIntensity, scan);

                // subtle horizontal glow band that slowly moves (idle warmth)
                float band = exp(-pow((uv.y - 0.5 - 0.02*sin(_Time.y*0.5))/0.12, 2.0)) * 0.12;
                color += band * _Tint.rgb;

                // noise / static
                float n = hash21(float2(uv.x * 512.0, uv.y * 512.0 + _Time.y * 10.0));
                color += (n - 0.5) * _NoiseIntensity;

                // flicker (low frequency)
                float flick = 1.0 + sin(_Time.y * _FlickerSpeed + uv.x * 10.0) * _FlickerAmount * (0.5 + 0.5*hash21(float2(uv.y, _Time.y)));
                color *= flick;

                // vignette
                float2 center = uv - 0.5;
                float dist = length(center);
                float vig = smoothstep(0.8, 0.3, dist);
                color = lerp(color, color * (1.0 - _Vignette), 1.0 - vig);

                color *= _Brightness;

                // apply greyscale checkbox (blends to luminance when checked)
                float finalLum = dot(color, float3(0.299,0.587,0.114));
                color = lerp(color, float3(finalLum, finalLum, finalLum), saturate(_Greyscale));

                // apply global alpha slider multiplied by source alpha
                float outA = saturate(_Alpha * src.a);

                return fixed4(saturate(color), outA);
            }
            ENDCG
        }
    }
    FallBack "Unlit/Transparent"
}
