Shader "Flat/CircleBillboardDepth"
{
    Properties
    {
        _PointSize("Point Size", Float) = 0.1
        _DistanceScale("Distance Scale", Float) = 1.0
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _FogColor("Fog Color", Color) = (0.6, 0.7, 0.8, 1)
        _FogDensity("Fog Density", Float) = 0.002
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
                UNITY_FOG_COORDS(2)
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _InstanceColor)
            UNITY_INSTANCING_BUFFER_END(Props)

            float _PointSize;
            float _DistanceScale;
            float4 _BaseColor;
            float4 _FogColor;
            float _FogDensity;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 camPos = _WorldSpaceCameraPos;

                float3 toCam = normalize(camPos - worldPos);
                float3 up = float3(0, 1, 0);
                float3 right = normalize(cross(up, toCam));
                up = normalize(cross(toCam, right));

                // calcular distancia y aplicar escala por distancia
                float dist = distance(camPos, worldPos);
                float perspective = pow(1.0 / (1.0 + 0.1 * dist), 1.5);
                float size = _PointSize * _DistanceScale * perspective;

                // billboard local pos
                float3 localOffset = (v.vertex.x * right + v.vertex.y * up) * size;
                o.worldPos = worldPos + localOffset;
                o.pos = UnityObjectToClipPos(float4(o.worldPos, 1.0));

                o.uv = v.vertex.xy * 0.5 + 0.5; // 0..1 para circular mask
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                // forma circular (descartar fuera del radio)
                float2 centeredUV = i.uv * 2.0 - 1.0;
                float r2 = dot(centeredUV, centeredUV);
                if (r2 > 1.0) discard;

                // suavizar borde
                float alpha = smoothstep(1.0, 0.8, 1.0 - r2);

                // color instanciado
                float4 instColor = UNITY_ACCESS_INSTANCED_PROP(Props, _InstanceColor);
                float4 col = instColor * _BaseColor;

                // simular profundidad con niebla
                float dist = distance(_WorldSpaceCameraPos, i.worldPos);
                float fogFactor = saturate(1.0 - dist * _FogDensity);
                float3 finalColor = lerp(_FogColor.rgb, col.rgb, fogFactor);

                col.rgb = finalColor;
                col.a *= alpha * fogFactor;

                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
