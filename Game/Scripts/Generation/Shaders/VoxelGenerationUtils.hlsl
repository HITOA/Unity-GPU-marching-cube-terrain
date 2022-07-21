#include "FastNoiseLite.hlsl"

float SampleVerticalGradient3D(float min, float max, float3 pos) {
	return 1 - smoothstep(min, max, clamp(pos.y, min, max));
}

float SampleFBMSimplex3D(float3 pos, float frequency, float octaves, float seed) {
    fnl_state noise = fnlCreateState();
    noise.frequency = frequency;
    noise.seed = seed;
    noise.octaves = octaves;
    noise.fractal_type = FNL_FRACTAL_FBM;
    noise.rotation_type_3d = FNL_ROTATION_IMPROVE_XZ_PLANES;

    return fnlGetNoise3D(noise, pos.x, pos.y, pos.z);
}

float SampleFBMSimplex2D(float3 pos, float frequency, float octaves, float seed) {
    fnl_state noise = fnlCreateState();
    noise.frequency = frequency;
    noise.seed = seed;
    noise.octaves = octaves;
    noise.fractal_type = FNL_FRACTAL_FBM;
    noise.rotation_type_3d = FNL_ROTATION_IMPROVE_XZ_PLANES;

    return fnlGetNoise3D(noise, pos.x, 0, pos.z);
}