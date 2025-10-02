Shader "Project Droids/Droid HD URP"
{
    Properties
    {
        _Color ("Primary Color (R)", Color) = (1,1,1,0.5)
        _Color1 ("Secondary Color (G)", Color) = (0.5,0.5,0.5,0.5)
        _Color2 ("Tertiary Color (B)", Color) = (0,0,0,0.5)
        _MainTex ("Fallback (RGB)", 2D) = "white" {}
        _MaskTex ("Mask", 2D) = "black" {}
        _BumpMap ("Normals", 2D) = "bump" {}
        _SpecMap ("Specular", 2D) = "white" {}
        _AOMap ("Ambient Occlusion", 2D) = "white" {}
        _AOScale ("Ambient Occlusion Intensity", Range(0.0,10.0)) = 1
        _DetailMap ("Pattern (R)", 2D) = "white" {}
        _Power("Vertex Color Intensity", Range(1.0,16.0) ) = 2.0
        _GlowColor ("Self-Illumination Color", Color) = (0,0,0,1)
        _DamageColor ("Damage Color", Color) = (1,1,1,0)
        _BurnLevel("Burn Level", Range(0.0,1.0)) = 0.0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 300

        Pass
        {
            Name "ForwardUnlit"
            Tags{"LightMode"="UniversalForward"}

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #pragma multi_compile __ _NORMALMAP
            #pragma multi_compile __ _DETAILMAP
            #pragma multi_compile __ _AOMAP
            #pragma multi_compile __ _VERTEXCOLOR

            #include "UnityCG.cginc"

            // Properties
            fixed4 _Color;
            fixed4 _Color1;
            fixed4 _Color2;
            fixed4 _GlowColor;
            fixed4 _DamageColor;
            float _BurnLevel;
            float _AOScale;
            float _Power;

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _MaskTex;
            float4 _MaskTex_ST;

            sampler2D _BumpMap;
            float4 _BumpMap_ST;

            sampler2D _SpecMap;
            float4 _SpecMap_ST;

            sampler2D _AOMap;
            float4 _AOMap_ST;

            sampler2D _DetailMap;
            float4 _DetailMap_ST;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangent    : TANGENT;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                float2 uv1        : TEXCOORD1;
            };

            struct Varyings
            {
                float4 posCS      : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 tangentWS  : TEXCOORD1;
                float3 bitangentWS: TEXCOORD2;
                float2 uvMask     : TEXCOORD3;
                float2 uvDetail   : TEXCOORD4;
                fixed4 vertColor  : COLOR;
                float3 viewDirWS  : TEXCOORD5;
            };

            // Unpack normal from normal map (same idea as UnpackNormal)
            float3 UnpackNormalFromMap(float4 packed)
            {
                float3 n;
                n.xy = packed.xy * 2.0 - 1.0;
                n.z = sqrt(saturate(1.0 - n.x*n.x - n.y*n.y));
                return n;
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.posCS = UnityObjectToClipPos(v.positionOS);

                // world space normal/tangent/bitangent
                float3 normalWS = UnityObjectToWorldNormal(v.normalOS);
                o.normalWS = normalWS;

                float3 tangentWS = UnityObjectToWorldDir(v.tangent.xyz);
                o.tangentWS = tangentWS;
                o.bitangentWS = cross(normalWS, tangentWS) * v.tangent.w;

                o.uvMask = TRANSFORM_TEX(v.uv, _MaskTex);
                o.uvDetail = TRANSFORM_TEX(v.uv1, _DetailMap);

                o.vertColor = v.color;
                float3 worldPos = mul(unity_ObjectToWorld, v.positionOS).xyz;
                o.viewDirWS = normalize(_WorldSpaceCameraPos - worldPos);

                return o;
            }

            // Compose albedo from mask + colors + detail (keeps spec/smoothness from spec map)
            fixed4 ComposeAlbedo(Varyings IN, fixed4 mask, fixed4 patt, out float smoothness, out float specular)
            {
                fixed4 col = fixed4(0,0,0,0);

                fixed4 baseColor = _Color;
            #ifdef _DETAILMAP
                baseColor *= patt;
            #endif
                col += baseColor * mask.r;

                fixed4 baseColor1 = _Color1;
            #ifdef _DETAILMAP
                baseColor1 *= patt;
            #endif
                col += baseColor1 * mask.g;

                fixed4 baseColor2 = _Color2;
            #ifdef _DETAILMAP
                baseColor2 *= patt;
            #endif
                col += baseColor2 * mask.b;

                fixed4 spec = tex2D(_SpecMap, IN.uvMask);
                specular = spec.g;
                smoothness = spec.a;

                return col;
            }

            // Fragment
            fixed4 frag(Varyings IN) : SV_Target
            {
                fixed4 mask = tex2D(_MaskTex, IN.uvMask);
                fixed4 detailPatt = fixed4(1,1,1,1);
            #ifdef _DETAILMAP
                detailPatt = tex2D(_DetailMap, IN.uvDetail);
            #endif

                float smoothness;
                float specular;
                fixed4 col = ComposeAlbedo(IN, mask, detailPatt, smoothness, specular);

            #ifdef _AOMAP
                fixed4 ao = tex2D(_AOMap, IN.uvMask);
                col.rgb *= ao.rgb * _AOScale;
            #endif

            #ifdef _VERTEXCOLOR
                float3 vertmul = IN.vertColor.rgb * _Power;
                col.rgb *= vertmul;
                specular *= (IN.vertColor.r + IN.vertColor.g + IN.vertColor.b) / 3.0;
            #endif

                // Burn reduces base color and emission influence
                col.rgb *= (1.0 - _BurnLevel);

                // Normal mapping (if provided)
                float3 n = normalize(IN.normalWS);
            #ifdef _NORMALMAP
                float4 bump = tex2D(_BumpMap, IN.uvMask);
                float3 nmap = UnpackNormalFromMap(bump);
                float3x3 TBN = float3x3(normalize(IN.tangentWS), normalize(IN.bitangentWS), normalize(IN.normalWS));
                n = normalize(mul(TBN, nmap));
            #endif

                // Simple unlit output: we do NOT depend on _LightColor0.
                // But we add a small view-facing specular approximation to mimic shiny parts.
                float3 V = normalize(IN.viewDirWS);
                float ndotv = saturate(dot(n, V));
                // specular-like term (white highlight)
                float specPower = lerp(8.0, 128.0, smoothness);
                float specTerm = specular * pow(ndotv, specPower);

                float3 finalCol = col.rgb;

                // add specular highlight (modulated, small)
                finalCol += specTerm * 0.35; // scale highlight so it won't blow out

                // Emission: glow + damage
                fixed3 emission = clamp((_GlowColor.rgb * mask.a * _GlowColor.a + _DamageColor.rgb * _DamageColor.a), 0.0, 1.0);
                emission *= (1.0 - _BurnLevel);

                finalCol += emission;

                return fixed4(finalCol, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Diffuse"
}
