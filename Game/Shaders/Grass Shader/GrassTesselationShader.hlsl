#ifndef GRASSSHADER_INCLUDED
#define GRASSSHADER_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "FastNoiseLite.hlsl"

struct attributes {
	float4 positionOS : POSITION;
	float3 normalWS	  : NORMAL;
	float4 tangent	  : TANGENT;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct tcp {
	float3 positionWS : INTERNALTESSPOS;
	float4 positionCS : SV_POSITION;
	float3 normalWS   : NORMAL;
	float4 tangent    : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct tesselationfactors {
	float edge[3] : SV_TessFactor;
	float inside : SV_InsideTessFactor;
	float3 data[3] : BEZIERPOS;
};

struct d2g {
	float3 positionWS : TEXCOORD0;
	float3 normalWS   : TEXCOORD1;
	float3 tangent    : TEXCOORD2;
};

struct g2f {
	float3 positionWS : TEXCOORD0;
	float3 normalWS   : TEXCOORD1;
	float3 uv		  : TEXCOORD2;
	float3 colormapuv : TEXCOORD3;
	float3 baseNormalWS : TEXCOORD4;
	float4 shadowCoord	: TEXCOORD5;
	float3 viewDir    : TEXCOORD6;

	float4 positionCS : SV_POSITION;
};

#define BARYCENTRIC_INTERPOLATE(fieldName) \
		patch[0].fieldName * barycentricCoordinates.x + \
		patch[1].fieldName * barycentricCoordinates.y + \
		patch[2].fieldName * barycentricCoordinates.z

tcp vert(attributes i) {
	tcp o = (tcp)0;

	UNITY_SETUP_INSTANCE_ID(i);
	UNITY_TRANSFER_INSTANCE_ID(i, o);

	VertexPositionInputs vInput = GetVertexPositionInputs(i.positionOS.xyz);

	o.positionWS = vInput.positionWS;
	o.positionCS = vInput.positionCS;
	o.normalWS = i.normalWS;
	o.tangent = i.tangent;

	return o;
}

// Returns true if the point is outside the bounds set by lower and higher
bool IsOutOfBounds(float3 p, float3 lower, float3 higher) {
	return p.x < lower.x || p.x > higher.x || p.y < lower.y || p.y > higher.y || p.z < lower.z || p.z > higher.z;
}

// Returns true if the given vertex is outside the camera fustum and should be culled
bool IsPointOutOfFrustum(float4 positionCS, float tolerance) {
	float3 culling = positionCS.xyz;
	float w = positionCS.w;
	// UNITY_RAW_FAR_CLIP_VALUE is either 0 or 1, depending on graphics API
	// Most use 0, however OpenGL uses 1
	float3 lowerBounds = float3(-w - tolerance, -w - tolerance, -w * UNITY_RAW_FAR_CLIP_VALUE - tolerance);
	float3 higherBounds = float3(w + tolerance, w + tolerance, w + tolerance);
	return IsOutOfBounds(culling, lowerBounds, higherBounds);
}

[domain("tri")]
[outputcontrolpoints(3)]
[outputtopology("triangle_cw")]
[patchconstantfunc("PatchConstantFunction")]
[partitioning("integer")]
tcp hull(
	InputPatch<tcp, 3> patch,
	uint id : SV_OutputControlPointID) {
	return patch[id];
}

float _GrassTesselation;

tesselationfactors PatchConstantFunction(InputPatch<tcp, 3> patch) {
	UNITY_SETUP_INSTANCE_ID(patch[0]);

	tesselationfactors o = (tesselationfactors)0;

	if (IsPointOutOfFrustum(patch[0].positionCS, 4)) {
		o.edge[0] = 0;
		return o;
	}

	o.edge[0] = _GrassTesselation;
	o.edge[1] = _GrassTesselation;
	o.edge[2] = _GrassTesselation;
	o.inside = _GrassTesselation;
	o.data[0] = patch[0].tangent.xyz;
	o.data[1] = patch[1].tangent.xyz;
	o.data[2] = patch[2].tangent.xyz;

	return o;
}

[domain("tri")]
d2g domain(
	tesselationfactors factors,
	OutputPatch<tcp, 3> patch,
	float3 barycentricCoordinates : SV_DomainLocation) {

	d2g o = (d2g)0;

	UNITY_SETUP_INSTANCE_ID(patch[0]);
	UNITY_TRANSFER_INSTANCE_ID(patch[0], o);

	float3 positionWS = BARYCENTRIC_INTERPOLATE(positionWS);
	float3 normalWS = BARYCENTRIC_INTERPOLATE(normalWS);
	float3 tangent = BARYCENTRIC_INTERPOLATE(tangent);

	o.positionWS = positionWS;
	o.normalWS = normalWS;
	o.tangent = tangent;

	return o;
}

g2f SetupVertex(float3 positionWS, float3 normalWS, float2 uv, float2 colormapuv, float3 baseNormalWS) {
	g2f o = (g2f)0;

	o.positionWS = positionWS;
	o.normalWS = normalWS;
	o.uv = float3(uv.xy, 0);
	o.positionCS = TransformWorldToHClip(positionWS);
	o.baseNormalWS = baseNormalWS;
	o.viewDir = normalize(GetWorldSpaceViewDir(positionWS));

	return o;
}

void AppendTriangle(inout TriangleStream<g2f> tStream, float3 p1, float3 p2, float3 p3, float2 uv1, float2 uv2, float2 uv3, float2 colormapuv, float3 baseNormalWS) {
	float3 u = p2 - p1;
	float3 v = p3 - p1;

	float3 normalWS = normalize(cross(u, v));

	tStream.Append(SetupVertex(p1, normalWS, uv1, colormapuv, baseNormalWS));
	tStream.Append(SetupVertex(p2, normalWS, uv2, colormapuv, baseNormalWS));
	tStream.Append(SetupVertex(p3, normalWS, uv3, colormapuv, baseNormalWS));

	tStream.RestartStrip();
}

float3x3 RotY(float ang) {
	return float3x3(
		cos(ang), 0, sin(ang),
		0, 1, 0,
		-sin(ang), 0, cos(ang));
}

float rand(float3 co)
{
	return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 53.539))) * 43758.5453);
}

float GetWindOffset(float2 pos) {
	fnl_state noise = fnlCreateState();
	noise.frequency = 0.1;
	noise.seed = 349834;
	noise.octaves = 1;

	return fnlGetNoise2D(noise, pos.x, pos.y);
}

float _GrassSize;
float _GrassRenderDistance;

float _WindSize;
float _WindSpeed;

[maxvertexcount(6)]
void geo(point d2g i[1], inout TriangleStream<g2f> tStream) {
	float3 normalWS = i[0].normalWS;

	if (normalWS.y < 0.6)
		return;

	float3 newNormalWS = normalize(normalWS + float3(0, 1, 0));

	float3 positionWS = i[0].positionWS;

	if (length(abs(_WorldSpaceCameraPos.xyz - positionWS)) > _GrassRenderDistance)
		return;

	float3 tangent = normalize(mul(i[0].tangent.xyz, RotY(rand(positionWS) * 3)));

	float2 noiseUv = positionWS.xz * _WindSize + _Time.y * _WindSpeed;
	float3 noiseOffset = float3((GetWindOffset(noiseUv) + 1) / 2, 0, 0) * _WindSpeed;

	float2 colormapuv = positionWS.xz;
	
	float3 v0 = positionWS - tangent * _GrassSize / 2;
	float3 v1 = positionWS + tangent * _GrassSize / 2;
	float3 v2 = positionWS + newNormalWS * _GrassSize + noiseOffset;

	float2 uv0 = float2(-0.5, 0);
	float2 uv1 = float2(1.5, 0);
	float2 uv2 = float2(0.5, 1.5);

	AppendTriangle(tStream, v0, v1, v2, uv0, uv1, uv2, colormapuv, normalWS);
}

float4 _Color;
sampler2D _GrassTexture;
sampler2D _GrassColorMap;
float4 _GrassColorMap_ST;
float _AlphaClip;
float4 _FogColor;
float _FogStart;
float _FogEnd;
float _GrassShadowAttenuation;
float _GrassSmoothness;
float _GrassSmoothnessPower;

float GetSmoothnessValue(float rawSmoothness) {
	return exp2(10 * clamp(rawSmoothness, 0, 1) + 1);
}

float3 ComputeMainLight(g2f i, float3 albedo) {
	Light light = GetMainLight(i.shadowCoord, i.positionWS, 1);

	float3 radiance = light.color * (_GrassShadowAttenuation + light.shadowAttenuation);

	float diffuse = saturate(dot(i.baseNormalWS, light.direction));

	float specularDot = saturate(dot(i.baseNormalWS, normalize(light.direction + i.viewDir)));
	float specular = pow(specularDot * _GrassSmoothnessPower, GetSmoothnessValue(_GrassSmoothness)) * diffuse;

	float3 ambiant = float3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);

	float3 color = albedo * (ambiant + radiance * (diffuse + specular));

	return color;
}

float3 ComputeAdditionalLight(g2f i, Light light, float3 albedo) {
	float3 radiance = light.color * (light.shadowAttenuation + _GrassShadowAttenuation) * light.distanceAttenuation;

	float diffuse = saturate(dot(i.baseNormalWS, light.direction));

	float3 color = albedo * (radiance * diffuse);

	return color;
}

float3 ComputeLighting(g2f i, float3 albedo) {
	i.shadowCoord = TransformWorldToShadowCoord(i.positionWS);

	float3 color = ComputeMainLight(i, albedo);

	uint numAdditionalLights = GetAdditionalLightsCount();

	for (uint j = 0; j < numAdditionalLights; j++) {
		Light light = GetAdditionalLight(j, i.positionWS, 1);
		color += ComputeAdditionalLight(i, light, albedo);
	}

	float dst = length(abs(_WorldSpaceCameraPos.xyz - i.positionWS));

	float fogPower = smoothstep(_FogStart, _FogEnd, dst);

	color = lerp(color, _FogColor.xyz, fogPower);

	return color;
}

float4 frag(g2f i) : SV_TARGET{
	float4 albedo = tex2D(_GrassTexture, i.uv.xy);

	if (i.uv.x < 0 ||
		i.uv.x > 1 ||
		i.uv.y > 1)
		discard;

	if (albedo.w < _AlphaClip)
		discard;

	float2 colorMapUv = i.positionWS.xz;

	float3 color = tex2D(_GrassColorMap, colorMapUv * _GrassColorMap_ST.xy);

	albedo = albedo * float4(color, 1) * _Color;

	return float4(ComputeLighting(i, albedo.xyz), 1);
}

#endif