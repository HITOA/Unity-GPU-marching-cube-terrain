Shader "Game/GrassShaderTesselation"
{
    Properties
    {
        _GrassTexture("Grass Texture", 2D) = "white" {}
        _GrassColorMap("Grass Color Map", 2D) = "white" {}
        _Color("Grass Color", Color) = (1, 1, 1, 1)
        _GrassSize("Grass Size", Float) = 1
        _GrassRenderDistance("Grass Render Distance", Float) = 1
        _AlphaClip("Alpha Clip", Float) = 0.5
        _WindSize("Wind Size", Float) = 1
        _WindSpeed("Wind Speed", Float) = 1
        _FogColor("Fog Color", Color) = (1, 1, 1, 1)
        _FogStart("Fog Start", Float) = 1
        _FogEnd("Fog End", Float) = 1
        _GrassTesselation("Grass Tesselation", Float) = 1
        _GrassShadowAttenuation("Grass Shadow Attenuation", Float) = 1
        _GrassSmoothness("Grass Smoothness", Float) = 0.5
        _GrassSmoothnessPower("Grass Smoothness Power", Float) = 1
    }
    SubShader
    {
        Tags {"RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass {
            Name "ForwardLit"
            Tags {"LightMode" = "UniversalForward"}
            Cull Off

            HLSLPROGRAM
            
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 5.0
            #pragma require geometry

            // Lighting and shadow keywords
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT

            // Register our functions
            #pragma vertex vert
            #pragma hull hull
            #pragma domain domain
            #pragma geometry geo
            #pragma fragment frag

            #include "GrassTesselationShader.hlsl"

            ENDHLSL
        }
    }
}
