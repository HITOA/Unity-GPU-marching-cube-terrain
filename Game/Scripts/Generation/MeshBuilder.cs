using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;

public class MeshBuilder : System.IDisposable
{
    static readonly int voxelstructStride = Marshal.SizeOf(typeof(Voxel));

    ComputeShader csMeshBuilder;

    int csMeshBuilderKernelId;
    int csMeshBuilderDualKernelId;
    int csTriEstimatorKernelId;
    int csTriEstimatorDualKernelId;

    int csVoxelDataId;
    int csDimId;
    int csScaleId;
    int csIsoLevelId;
    int csVertexBufferId;
    int csIndexBufferId;
    int csSecondVertexBufferId;
    int csSecondIndexBufferId;
    int csCounterId;
    int csSecondCounterId;

    ComputeBuffer counterBuffer;
    
    ComputeBuffer secondCounterBuffer;

    ComputeBuffer counterArgsBuffer;

    GraphicsBuffer vertexBuffer;
    GraphicsBuffer indexBuffer;

    public ComputeBuffer secondVertexBuffer
    {
        get;
        private set;
    }

    public ComputeBuffer secondIndexBuffer
    {
        get;
        private set;
    }

    public int lastTriCount
    {
        get;
        private set;
    }

    public int secondTriCount
    {
        get;
        private set;
    }

    Mesh mesh;

    public MeshBuilder(ComputeShader csShader)
    {
        Initialize(csShader);
    }

    public Mesh GenerateMesh(Vector3Int size, float scale, float isoLevel, Voxel[,,] voxels, bool dual = false)
    {
        ComputeBuffer voxelsBuffer = new ComputeBuffer(size.x * size.y * size.z, voxelstructStride);
        voxelsBuffer.SetData(voxels);
        GenerateMesh(size, scale, isoLevel, voxelsBuffer, dual);
        voxelsBuffer.Release();
        return mesh;
    }

    public Mesh GenerateMesh(Vector3Int size, float scale, float isoLevel, ComputeBuffer voxels, bool dual = false)
    {
        EstimateTri(size, isoLevel, voxels, dual);

        ClearMesh();
        mesh = CreateMesh(lastTriCount);

        if (lastTriCount <= 0)
            return mesh;

        vertexBuffer = mesh.GetVertexBuffer(0);
        indexBuffer = mesh.GetIndexBuffer();

        if (dual && secondTriCount > 0)
        {
            secondVertexBuffer = new ComputeBuffer(secondTriCount * 3, 12);
            secondIndexBuffer = new ComputeBuffer(secondTriCount * 3, 4);
        }else
        {
            dual = false;
        }

        int currentKernelid = dual ? csMeshBuilderDualKernelId : csMeshBuilderKernelId;

        uint xc, yc, zc;

        csMeshBuilder.GetKernelThreadGroupSizes(
            currentKernelid, out xc, out yc, out zc);

        int dx = (int)(size.x / xc) + 1,
            dy = (int)(size.y / yc) + 1,
            dz = (int)(size.z / zc) + 1;

        csMeshBuilder.SetBuffer(currentKernelid, csVoxelDataId, voxels);
        csMeshBuilder.SetFloats(csDimId, size.x, size.y, size.x);
        csMeshBuilder.SetFloat(csScaleId, scale);
        csMeshBuilder.SetFloat(csIsoLevelId, isoLevel);

        csMeshBuilder.SetBuffer(currentKernelid, csVertexBufferId, vertexBuffer);
        csMeshBuilder.SetBuffer(currentKernelid, csIndexBufferId, indexBuffer);

        if (dual)
        {
            csMeshBuilder.SetBuffer(currentKernelid, csSecondVertexBufferId, secondVertexBuffer);
            csMeshBuilder.SetBuffer(currentKernelid, csSecondIndexBufferId, secondIndexBuffer);

            secondCounterBuffer.SetCounterValue(0);
            csMeshBuilder.SetBuffer(currentKernelid, csSecondCounterId, secondCounterBuffer);
        }

        counterBuffer.SetCounterValue(0);

        csMeshBuilder.SetBuffer(currentKernelid, csCounterId, counterBuffer);

        csMeshBuilder.Dispatch(currentKernelid, dx, dy, dz);

        mesh.bounds = new Bounds((Vector3)size / 2 * scale, (Vector3)size * scale);

        return mesh;
    }

    private void EstimateTri(Vector3Int size, float isoLevel, ComputeBuffer voxels, bool dual)
    {
        uint xc, yc, zc;

        int currentKernelId = dual ? csTriEstimatorDualKernelId : csTriEstimatorKernelId;

        csMeshBuilder.GetKernelThreadGroupSizes(
            currentKernelId, out xc, out yc, out zc);

        int dx = (int)(size.x / xc) + 1,
            dy = (int)(size.y / yc) + 1,
            dz = (int)(size.z / zc) + 1;

        csMeshBuilder.SetBuffer(currentKernelId, csVoxelDataId, voxels);
        csMeshBuilder.SetFloats(csDimId, size.x, size.y, size.x);
        csMeshBuilder.SetFloat(csIsoLevelId, isoLevel);

        counterBuffer.SetCounterValue(0);

        csMeshBuilder.SetBuffer(currentKernelId, csCounterId, counterBuffer);

        if (dual)
        {
            secondCounterBuffer.SetCounterValue(0);

            csMeshBuilder.SetBuffer(currentKernelId, csSecondCounterId, secondCounterBuffer);
        }

        csMeshBuilder.Dispatch(currentKernelId, dx, dy, dz);

        lastTriCount = GetCounterValue(counterBuffer);
        secondTriCount = GetCounterValue(secondCounterBuffer);
    }

    private void ClearMesh()
    {
        if (vertexBuffer != null)
            vertexBuffer.Dispose();
        if (indexBuffer != null)
            indexBuffer.Dispose();
        if (secondVertexBuffer != null)
            secondVertexBuffer.Dispose();
        if (secondIndexBuffer != null)
            secondIndexBuffer.Dispose();
        if (mesh != null)
            Object.Destroy(mesh);
    }

    private Mesh CreateMesh(int triCount)
    {
        Mesh mesh = new Mesh();

        mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;

        mesh.SetVertexBufferParams(
            triCount * 3,
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3));
        mesh.SetIndexBufferParams(triCount * 3, IndexFormat.UInt32);

        mesh.SetSubMesh(0, new SubMeshDescriptor(0, triCount * 3, MeshTopology.Triangles), MeshUpdateFlags.DontRecalculateBounds);

        return mesh;
    }

    private int GetCounterValue(ComputeBuffer counter)
    {
        int[] args = new int[1];
        counterArgsBuffer.SetData(args);
        ComputeBuffer.CopyCount(counter, counterArgsBuffer, 0);
        counterArgsBuffer.GetData(args);
        return args[0];
    }

    private void Initialize(ComputeShader csShader)
    {
        csMeshBuilder = csShader;

        csMeshBuilderKernelId = csMeshBuilder.FindKernel("CSMeshBuilder");
        csMeshBuilderDualKernelId = csMeshBuilder.FindKernel("CSMeshBuilderDual");
        csTriEstimatorKernelId = csMeshBuilder.FindKernel("CSTriEstimator");
        csTriEstimatorDualKernelId = csMeshBuilder.FindKernel("CSTriEstimatorDual");

        csVoxelDataId = Shader.PropertyToID("_VoxelData");
        csDimId = Shader.PropertyToID("_Dim");
        csScaleId = Shader.PropertyToID("_Scale");
        csIsoLevelId = Shader.PropertyToID("_IsoLevel");
        csVertexBufferId = Shader.PropertyToID("_VertexBuffer");
        csIndexBufferId = Shader.PropertyToID("_IndexBuffer");
        csSecondVertexBufferId = Shader.PropertyToID("_SecondVertexBuffer");
        csSecondIndexBufferId = Shader.PropertyToID("_SecondIndexBuffer");
        csCounterId = Shader.PropertyToID("_Counter");
        csSecondCounterId = Shader.PropertyToID("_SecondCounter");
        

        counterBuffer = new ComputeBuffer(1, 4, ComputeBufferType.Counter);
        counterArgsBuffer = new ComputeBuffer(1, 4, ComputeBufferType.IndirectArguments);
        secondCounterBuffer = new ComputeBuffer(1, 4, ComputeBufferType.Counter);
    }

    public void Dispose()
    {
        ClearMesh();
        counterBuffer.Dispose();
        counterArgsBuffer.Dispose();
        secondCounterBuffer.Dispose();
    }
}
