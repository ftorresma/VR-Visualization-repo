Shader "Flat/CircleBillboard"
{
    Properties
    {
        _PointSize("Point Size", Float) = 0.2
        _DistanceScale("Distance Scale", Float) = 0.2
        _Color("Base Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        //Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        //ZWrite Off
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha
        //
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            // Propiedades globales
            float _PointSize;
            float _DistanceScale;
            fixed4 _Color;

            // Por instancia (desde script)
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _InstanceColor)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                // Posición del quad en espacio mundo
                float3 worldPos = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;

                // Orientación hacia cámara
                float3 camRight = UNITY_MATRIX_V._m00_m01_m02;
                float3 camUp = UNITY_MATRIX_V._m10_m11_m12;

                // Escalado por distancia
                float3 camPos = _WorldSpaceCameraPos;
                float dist = distance(camPos, worldPos);
                //float size = _PointSize * dist * _DistanceScale;
                //float size = _PointSize * dist * _DistanceScale * saturate(1.0 / (0.2 + dist * 0.1));
                //float size = _PointSize * _DistanceScale * dist * pow(1.0 / (1.0 + 0.1 * dist), 1.5);
                float size = _PointSize * _DistanceScale * 2;



                // Offset del vértice (quad centrado)
                float3 offset = (v.vertex.x * camRight + v.vertex.y * camUp) * size;
                float3 finalPos = worldPos + offset;

                o.pos = UnityWorldToClipPos(float4(finalPos, 1.0));
                o.uv = v.vertex.xy + 0.5;

                return o;
            }

            // fixed4 frag (v2f i) : SV_Target
            // {
            //     UNITY_SETUP_INSTANCE_ID(i);
            //     fixed4 col = UNITY_ACCESS_INSTANCED_PROP(Props, _InstanceColor);

            //     // Círculo suave en el fragmento
            //     float2 uv = i.uv - 0.5;
            //     float r = length(uv);
            //     float alpha = smoothstep(0.5, 0.45, r);

            //     return fixed4(col.rgb, alpha * col.a);
            // }
            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                fixed4 col = UNITY_ACCESS_INSTANCED_PROP(Props, _InstanceColor);

                float2 uv = i.uv - 0.5;
                float r = length(uv);

                // círculo suave
                float alpha = smoothstep(0.5, 0.48, r);

                // sombreado esférico (iluminación simulada)
                //float shade = saturate(1.2 - 2.0 * r); // centro brillante, borde oscuro
                //float3 shadedColor = col.rgb * (0.5 + 0.5 * shade);

                // dirección de luz simulada
                float3 lightDir = normalize(float3(0.4, 0.6, 0.7));
                float shade = saturate(dot(normalize(float3(uv.x, uv.y, 0.5)), lightDir));
                float3 shadedColor = col.rgb * (0.4 + 0.6 * shade);

                // DESCARTA píxeles transparentes
                clip(alpha - 0.1);
                
                return fixed4(shadedColor, alpha * col.a);
            }

            ENDCG
        }
    }
}
