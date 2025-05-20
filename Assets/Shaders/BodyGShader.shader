Shader "Custom/BruiseShader"
{
    Properties
    {
        _MainTex("Base Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0

        _NoBruiseColor("No Bruise", Color) = (1,1,1,1)
        _WeakBruiseColor("Weak Bruise", Color) = (0.6, 0.4, 0.5, 1)
        _MediumBruiseColor("Medium Bruise", Color) = (0.4, 0.2, 0.3, 1)
        _StrongBruiseColor("Strong Bruise", Color) = (0.2, 0, 0.1, 1)

        _BruiseCount("Bruise Count", Int) = 0

        _TestBruisePosition("TestBruisePosition", Vector) = (0,0,0,0)
        _TestBruiseRaduis("TestBruisePosition", Float) = 0.01
        // [HideInInspector]_BruisePositions("Bruise Positions", Vector) = (0,0,0,0)
        // [HideInInspector]_BruiseStrengths("Bruise Strengths", Float) = 0
        // [HideInInspector]_BruiseRadii("Bruise Radii", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        half _Glossiness;
        half _Metallic;

        int _BruiseCount;
        float3 _BruisePositions[100];
        float _BruiseStrengths[100];
        float _BruiseRadii[100];

        fixed4 _NoBruiseColor;
        fixed4 _WeakBruiseColor;
        fixed4 _MediumBruiseColor;
        fixed4 _StrongBruiseColor;

        float3 _TestBruisePosition;
        float _TestBruiseRaduis;
        float4x4 _WorldToRoot;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            INTERNAL_DATA
        };

        float3 ToLocal(float3 worldPos)
        {
            return mul(_WorldToRoot, float4(worldPos, 1)).xyz;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 localPos = ToLocal(IN.worldPos);

            float totalStrength = 0;

            for (int i = 0; i < _BruiseCount; ++i)
            {
                float dist = distance(localPos, _BruisePositions[i]);
                float radius = _BruiseRadii[i];
                if (dist < radius)
                {
                    float influence = 1.0 - (dist / radius);
                    totalStrength += _BruiseStrengths[i] * influence;
                }
            }

            totalStrength = saturate(totalStrength); // Clamp to max 1

            fixed4 bruiseColor;
            if (totalStrength < 0.25)
                bruiseColor = lerp(_NoBruiseColor, _WeakBruiseColor, totalStrength * 4.0);
            else if (totalStrength < 0.5)
                bruiseColor = lerp(_WeakBruiseColor, _MediumBruiseColor, (totalStrength - 0.25) * 4.0);
            else
                bruiseColor = lerp(_MediumBruiseColor, _StrongBruiseColor, (totalStrength - 0.5) * 2.0);

            fixed4 baseColor = tex2D(_MainTex, IN.uv_MainTex) * _Color;

            fixed4 finalColor = lerp(baseColor, bruiseColor, totalStrength);

            o.Albedo = finalColor.rgb;
            o.Alpha = finalColor.a;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
