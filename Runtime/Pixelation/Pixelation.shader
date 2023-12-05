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
            
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            #pragma vertex vert
            #pragma fragment frag
 
            #pragma multi_compile_local _SQUARE _CIRCLE
            #pragma multi_compile_local _ _POSTER

            sampler2D _MainTex;

            float4 _Pixels;     // x, y - desired resolution, z - gap, w - posterization
            float4 _Color;
            //float4 _MainTex_TexelSize;

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


            half luma(half3 rgb)
            {
                return dot(rgb, half3(0.299, 0.587, 0.114));
            }
            
            inline float2 uvSnap(float2 uv)
            {
                return float2(round((uv.x - 0.5) * _Pixels.x) / _Pixels.x + 0.5, round((uv.y - 0.5) * _Pixels.y) / _Pixels.y + 0.5);
                // crisp test
                // float2 res = float2(round((uv.x - 0.5) * _Pixels.x) / _Pixels.x + 0.5, round((uv.y - 0.5) * _Pixels.y) / _Pixels.y + 0.5);

                // res.x -= res.x % _MainTex_TexelSize.x;
                // res.x += _MainTex_TexelSize.x * .5;
                //
                // res.y -= res.y % _MainTex_TexelSize.y;
                // res.y += _MainTex_TexelSize.y * .5;

                // return res;
            }

            frag_in vert(const vert_in v)
            {
                frag_in o;
                o.vertex = v.vertex;
                o.uv = v.uv;
                return o;
            }

            half4 frag(const frag_in i) : SV_Target
            {
                float2 snap   = uvSnap(i.uv);
                float2 offset = abs(i.uv - snap) * _Pixels.xy;
                half4 sample  = tex2D(_MainTex, snap);
#ifdef _SQUARE
                float val = max(offset.x, offset.y);
#endif
#ifdef _CIRCLE  
                float val = length(offset);
#endif
#ifdef _POSTER
                sample = pow(abs(sample), 0.4545);
                float3 c = RgbToHsv(sample.xyz);
                c.z = round(c.z * _Pixels.w) / _Pixels.w;
                sample = float4(HsvToRgb(c), sample.a);
                sample = pow(abs(sample), 2.1);
#endif
                return lerp(sample, half4(lerp(sample.rgb, _Color.rgb, _Color.a), sample.a), step(_Pixels.z, val));
            }
            ENDHLSL
        }
    }
}
