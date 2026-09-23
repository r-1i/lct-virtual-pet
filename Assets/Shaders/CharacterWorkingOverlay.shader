Shader "Custom/CharacterWorkingOverlay"
{
    // Flat semi-transparent grey wash, added as an EXTRA material on top of the character's
    // existing materials (not a replacement) — no texture sampling, works on any character shader.
    Properties
    {
        _Color ("Overlay Color", Color) = (0.35, 0.35, 0.35, 0.55)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        // Same mesh gets drawn twice (original submesh + this overlay submesh, see
        // CharacterWorkingPresenter.SetOverlay); nudge depth slightly so it wins the depth test
        // cleanly instead of z-fighting with the original pass.
        Offset -1, -1

        Pass
        {
            // Without an explicit LightMode, URP's forward renderer doesn't include this pass at
            // all — it compiles fine but silently never draws. This was the actual bug.
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            float4 _Color;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                return _Color;
            }
            ENDHLSL
        }
    }
}
