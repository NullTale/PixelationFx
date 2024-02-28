Shader "Hidden/Vol/Pixelation"
{
    Properties
    {
        _Pixels("Pixels", Vector) = (1, 1, 0, 1)
        _Color("Color", Color) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline" = "UniversalPipeline" }
        LOD 0
        
        ZTest Always
        ZWrite Off
        ZClip false
        Cull Off

        Pass
        {
            name "Pixelation"
            
            HLSLPROGRAM
            
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            #pragma vertex vert
            #pragma fragment frag
 
            #pragma multi_compile_local _SQUARE _CIRCLE
            #pragma multi_compile_local _ _CRISP
            #pragma multi_compile_local _ _USE_PALETTE

            sampler2D _MainTex;
            sampler2D _LutTex;

            float4 _Pixels;     // x, y - desired resolution, z - gap, w - posterization
            float4 _Color;
            float  _Roundness;
#ifdef _CRISP
            float4 _MainTex_TexelSize;
#endif
            
            struct vert_in
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct frag_in
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            frag_in vert(const vert_in v)
            {
                frag_in o;
                o.vertex = v.vertex;
                o.uv = v.uv;
                return o;
            }
            
            half3 GetLinearToSRGB(half3 c)
            {
#if _USE_FAST_SRGB_LINEAR_CONVERSION
                return FastLinearToSRGB(c);
#else
                return LinearToSRGB(c);
#endif
            }

            real3 GetSRGBToLinear(real3 c)
            {
#if _USE_FAST_SRGB_LINEAR_CONVERSION
                return FastSRGBToLinear(c);
#else
                return SRGBToLinear(c);
#endif
            }
                        
            #define LUT_SIZE 16.
            #define LUT_SIZE_MINUS (16. - 1.)
            
            float3 lut_sample(in float3 col, const sampler2D tex)
            {
#if !defined(UNITY_COLORSPACE_GAMMA)
                float3 uvw = GetLinearToSRGB(col);
#else
                float3 uvw = col;
#endif
                float2 uv;
                
                // get replacement color from the lut set
                uv.y = uvw.y * (LUT_SIZE_MINUS / LUT_SIZE) + .5 * (1. / LUT_SIZE);
                uv.x = uvw.x * (LUT_SIZE_MINUS / (LUT_SIZE * LUT_SIZE)) + .5 * (1. / (LUT_SIZE * LUT_SIZE)) + floor(uvw.z * LUT_SIZE) / LUT_SIZE;    

                float3 lutColor = tex2D(tex, uv).rgb;
                
#if !defined(UNITY_COLORSPACE_GAMMA)
                lutColor = GetSRGBToLinear(lutColor.xyz);
#endif

                return lutColor;
            }

            half luma(half3 rgb)
            {
                return dot(rgb, half3(0.299, 0.587, 0.114));
            }
            
            inline float2 uvSnap(float2 uv)
            {
#ifdef _CRISP
                float2 res = float2(round((uv.x - 0.5) * _Pixels.x) / _Pixels.x + 0.5, round((uv.y - 0.5) * _Pixels.y) / _Pixels.y + 0.5);

                // snap pixel to the center of the main tex
                res.x -= res.x % _MainTex_TexelSize.x;
                res.x += _MainTex_TexelSize.x * .5;
                
                res.y -= res.y % _MainTex_TexelSize.y;
                res.y += _MainTex_TexelSize.y * .5;

                return res;
#else
                return float2(round((uv.x - 0.5) * _Pixels.x) / _Pixels.x + 0.5, round((uv.y - 0.5) * _Pixels.y) / _Pixels.y + 0.5);
#endif
            }

            half4 frag(const frag_in i) : SV_Target
            {
                float2 snap   = uvSnap(i.uv);
                float2 offset = abs(i.uv - snap) * _Pixels.xy;
                half4 sample  = tex2D(_MainTex, snap);

#ifdef _USE_PALETTE
                sample.rgb = lerp(sample.rgb, lut_sample(sample.rgb, _LutTex), _Pixels.w);
#endif
                float shape = 1 - step(length(offset), _Pixels.z * _Roundness) * step(max(offset.x, offset.y), _Pixels.z);
                return lerp(sample, _Color, shape);
            }
            ENDHLSL
        }
    }
}
