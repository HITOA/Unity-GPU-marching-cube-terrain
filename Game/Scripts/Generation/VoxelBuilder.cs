using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class VoxelBuilder : System.IDisposable
{
    static readonly int voxelstructStride = Marshal.SizeOf(typeof(Voxel));
    static readonly int noiseLayerstructStride = Marshal.SizeOf(typeof(NoiseLayerPreset));

    ComputeShader csVoxelBuilder;

    int csVoxelGeneratorKernelId;
    int csVoxelEditSphericalId;

    int csResultId;
    int csDimId;
    int csOffsetId;
    int csScaleId;
    int csSeedId;

    int csTerrainHeightId;
    int csLayersId;
    int csLayersCountId;

    int csCenterId;
    int csRadiusId;
    int csPowerId;

    float seed;

    GenerationPreset preset;

    ComputeBuffer noiseLayerBuffer;

    public VoxelBuilder(ComputeShader csShader, GenerationPreset preset, float seed)
    {
        this.seed = seed;
        this.preset = preset;

        Initialize(csShader);
    }

    public ComputeBuffer GenerateVoxel(Vector3Int size, float scale, Vector3 offset)
    {
        uint xc, yc, zc;

        csVoxelBuilder.GetKernelThreadGroupSizes(
            csVoxelGeneratorKernelId, out xc, out yc, out zc);

        int dx = (int)(size.x / xc) + 1,
            dy = (int)(size.y / yc) + 1,
            dz = (int)(size.z / zc) + 1;

        ComputeBuffer csResult = new ComputeBuffer(size.x * size.y * size.z, voxelstructStride);

        csVoxelBuilder.SetBuffer(csVoxelGeneratorKernelId, csResultId, csResult);
        csVoxelBuilder.SetFloats(csDimId, size.x, size.y, size.z);
        csVoxelBuilder.SetFloats(csOffsetId, offset.x, offset.y, offset.z);
        csVoxelBuilder.SetFloat(csScaleId, scale);
        csVoxelBuilder.SetFloat(csSeedId, seed);
        csVoxelBuilder.SetFloats(csTerrainHeightId, preset.minMaxHeight.x, preset.minMaxHeight.y);
        csVoxelBuilder.SetBuffer(csVoxelGeneratorKernelId, csLayersId, noiseLayerBuffer);
        csVoxelBuilder.SetInt(csLayersCountId, preset.layers.Length);

        csVoxelBuilder.Dispatch(csVoxelGeneratorKernelId, dx, dy, dz);

        return csResult;
    }

    public Voxel[,,] GenerateVoxelCPU(Vector3Int size, float scale, Vector3 offset)
    {
        ComputeBuffer csResult = GenerateVoxel(size, scale, offset);

        Voxel[,,] result = new Voxel[size.x, size.y, size.z];
        csResult.GetData(result);

        csResult.Release();

        return result;
    }

    public void EditSpherical(Vector3Int size, float scale, Vector3 offset, Vector3 position, float radius, float power, ComputeBuffer csBuffer)
    {
        uint xc, yc, zc;

        csVoxelBuilder.GetKernelThreadGroupSizes(
            csVoxelEditSphericalId, out xc, out yc, out zc);

        int dx = (int)(size.x / xc) + 1,
            dy = (int)(size.y / yc) + 1,
            dz = (int)(size.z / zc) + 1;

        csVoxelBuilder.SetBuffer(csVoxelEditSphericalId, csResultId, csBuffer);
        csVoxelBuilder.SetFloats(csDimId, size.x, size.y, size.z);
        csVoxelBuilder.SetFloats(csOffsetId, offset.x, offset.y, offset.z);
        csVoxelBuilder.SetFloat(csScaleId, scale);

        csVoxelBuilder.SetFloats(csCenterId, position.x, position.y, position.z);
        csVoxelBuilder.SetFloat(csRadiusId, radius);
        csVoxelBuilder.SetFloat(csPowerId, power);

        csVoxelBuilder.Dispatch(csVoxelEditSphericalId, dx, dy, dz);
    }

    public Voxel[,,] EditSphericalCPU(Vector3Int size, float scale, Vector3 offset, Vector3 position, float radius, float power, Voxel[,,] csBuffer)
    {
        ComputeBuffer csResult = new ComputeBuffer(size.x * size.y * size.z, voxelstructStride);
        csResult.SetData(csBuffer);
        EditSpherical(size, scale, offset, position, radius, power, csResult);
        csResult.GetData(csBuffer);
        return csBuffer;
    }

    private void Initialize(ComputeShader csShader)
    {
        csVoxelBuilder = csShader;

        csVoxelGeneratorKernelId = csVoxelBuilder.FindKernel("CSVoxelGenerator");
        csVoxelEditSphericalId = csVoxelBuilder.FindKernel("CSEditSpherical");

        csResultId = Shader.PropertyToID("_Result");
        csDimId = Shader.PropertyToID("_Dim");
        csOffsetId = Shader.PropertyToID("_Offset");
        csScaleId = Shader.PropertyToID("_Scale");
        csSeedId = Shader.PropertyToID("_Seed");

        csTerrainHeightId = Shader.PropertyToID("_TerrainHeight");
        csLayersId = Shader.PropertyToID("_Layers");
        csLayersCountId = Shader.PropertyToID("_LayersCount");

        csCenterId = Shader.PropertyToID("_Center");
        csRadiusId = Shader.PropertyToID("_Radius");
        csPowerId = Shader.PropertyToID("_Power");

        noiseLayerBuffer = new ComputeBuffer(preset.layers.Length, noiseLayerstructStride);
        noiseLayerBuffer.SetData(preset.layers);
    }

    public void Dispose()
    {
        if (noiseLayerBuffer != null)
            noiseLayerBuffer.Dispose();
    }
}
