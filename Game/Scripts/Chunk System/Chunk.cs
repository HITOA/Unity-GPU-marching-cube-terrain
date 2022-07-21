using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : IChunk
{
    float isolevel;

    MeshBuilder meshBuilder;
    VoxelBuilder voxelBuilder;

    ChunkData chunkData;

    GameObject gObject;
    MeshFilter meshFilter;
    MeshRenderer meshRenderer;
    MeshCollider meshCollider;

    ChunkSystemSO chunkSystemSO;

    public bool isPending
    {
        get;
        private set;
    }

    public Vector3 position
    {
        get;
        private set;
    }
    public Vector3Int size
    {
        get;
        private set;
    }

    public Chunk(VoxelBuilder voxelBuilder, ChunkSystemSO chunkSystemSO, Vector3Int sizeWS, Vector3 position)
    {
        this.position = position;
        this.isolevel = chunkSystemSO.isolevel;
        this.voxelBuilder = voxelBuilder;
        this.chunkSystemSO = chunkSystemSO;
        this.size = sizeWS;
        Initialize("Chunk", chunkSystemSO.csMeshBuilder);
    }

    public void BuildMeshOnly(bool drawGrass, bool buildCollision)
    {
        if (chunkData.GetVoxelsMemoryLocation() == 0)
            return;

        if (meshFilter.mesh != null)
        {
            Object.Destroy(meshFilter.mesh);
            meshFilter.mesh = null;
        }

        Mesh mesh;

        if (chunkData.GetVoxelsMemoryLocation() == 1)
        {
            mesh = meshBuilder.GenerateMesh(
                chunkData.sizeVoxels,
                chunkData.resolution,
                isolevel,
                chunkData.GetVoxelsGPU(), buildCollision);
        }
        else
        {
            mesh = meshBuilder.GenerateMesh(
                chunkData.sizeVoxels,
                chunkData.resolution,
                isolevel,
                chunkData.GetVoxelsCPU(), buildCollision);
        }

        if (buildCollision && meshBuilder.secondTriCount > 3)
        {
            if (meshCollider == null)
                meshCollider = gObject.AddComponent<MeshCollider>();

            Mesh colliderMesh = new Mesh();

            Vector3[] verticies = new Vector3[meshBuilder.secondTriCount * 3];
            int[] indicies = new int[meshBuilder.secondTriCount * 3];

            meshBuilder.secondVertexBuffer.GetData(verticies);
            meshBuilder.secondIndexBuffer.GetData(indicies);

            colliderMesh.SetVertices(verticies);
            colliderMesh.SetIndices(indicies, MeshTopology.Triangles, 0);

            if (meshCollider.sharedMesh != null)
                Object.Destroy(meshCollider.sharedMesh);

            float v = colliderMesh.bounds.extents.x * colliderMesh.bounds.extents.y * colliderMesh.bounds.extents.z;

            if (v > 0)
                meshCollider.sharedMesh = colliderMesh;
        }
        else if (meshCollider != null)
            Object.Destroy(meshCollider);

        meshFilter.mesh = mesh;

        if (drawGrass)
            meshRenderer.materials = new Material[] { chunkSystemSO.groundMat, chunkSystemSO.grassMat };
        else
            meshRenderer.materials = new Material[] { chunkSystemSO.groundMat };
    }

    public void Build(float resolution, bool storeingpu, bool drawGrass, bool buildCollision)
    {
        if (storeingpu)
        {
            BuildGPU(resolution);
        }
        else
        {
            BuildCPU(resolution);
        }

        BuildMeshOnly(drawGrass, buildCollision);
    }
    public void EditSpherical(Vector3 position, float radius, float power)
    {
        if (chunkData.GetVoxelsMemoryLocation() == 1)
        {
            voxelBuilder.EditSpherical(chunkData.sizeVoxels, chunkData.resolution, 
                this.position, position, radius, power, chunkData.GetVoxelsGPU());
        }
        else if (chunkData.GetVoxelsMemoryLocation() == 2)
        {
            chunkData.SetVoxels(voxelBuilder.EditSphericalCPU(chunkData.sizeVoxels, 
                chunkData.resolution, this.position, position, radius, power, chunkData.GetVoxelsCPU()));
        }
    }

    public float GetChunkResolution()
    {
        return chunkData.resolution;
    }

    public float GetChunkMemoryLocation()
    {
        return chunkData.GetVoxelsMemoryLocation();
    }

    public void SetPending(bool v)
    {
        isPending = v;
    }

    private void BuildGPU(float resolution)
    {
        chunkData.resolution = resolution;
        chunkData.SetVoxels(voxelBuilder.GenerateVoxel(chunkData.sizeVoxels, resolution, position));
    }

    private void BuildCPU(float resolution)
    {
        chunkData.resolution = resolution;
        chunkData.SetVoxels(voxelBuilder.GenerateVoxelCPU(chunkData.sizeVoxels, resolution, position));
    }

    private void Initialize(string name, ComputeShader csMeshBuilder)
    {
        meshBuilder = new MeshBuilder(csMeshBuilder);

        chunkData = new ChunkData(size);

        gObject = new GameObject($"{name} [{position.x}, {position.y}, {position.z}]");
        meshFilter = gObject.AddComponent<MeshFilter>();
        meshRenderer = gObject.AddComponent<MeshRenderer>();

        gObject.transform.position = position;
    }

    public void Dispose()
    {
        chunkData.ClearVoxels();
        meshBuilder.Dispose();
        Object.Destroy(gObject);
    }
}
