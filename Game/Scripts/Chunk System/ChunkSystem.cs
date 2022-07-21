using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.Threading.Tasks;

public class ChunkSystem : System.IDisposable
{
    Dictionary<Vector3Int, IChunk> loadedChunks;
    LinkedList<PendingChunkData> pendingChunkList;

    VoxelBuilder voxelBuilder;

    ChunkSystemSO chunkSystemSO;

    int chunkRenderDistance;
    int chunkHeightRenderDistance;
    int chunkHighRenderDistance;
    float chunkProcessTime;

    Stopwatch sw;
    Vector3Int lastViewerChunkPos;

    public ChunkSystem(ChunkSystemSO chunkSystemSO)
    {
        this.chunkSystemSO = chunkSystemSO;
        Initialize();
    }

    public void EditTerrainSpherical(Vector3 position, float radius, float power)
    {
        Vector3Int editChunkPos = World2ChunkPos(position);

        for (int z = -1; z <= 1; z++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    Vector3Int currentChunkPos = editChunkPos + new Vector3Int(x, y, z);

                    Bounds a = new Bounds(Chunk2WorldPos(currentChunkPos) + chunkSystemSO.chunkSize / 2, chunkSystemSO.chunkSize);
                    Bounds b = new Bounds(position, new Vector3(radius * 2, radius * 2, radius * 2));

                    if (CheckBoxCollision(a, b))
                    {
                        IChunk chunk = loadedChunks[currentChunkPos];
                        chunk.EditSpherical(position, radius, power);
                        if (!chunk.isPending)
                        {
                            bool nc = chunk.GetChunkMemoryLocation() == 1;
                            EnqueueChunk(currentChunkPos, nc, nc);
                        }
                    }
                }
            }
        }
    }

    private bool CheckBoxCollision(Bounds a, Bounds b)
    {
        return
            (a.min.x <= b.max.x && a.max.x >= b.min.x) &&
            (a.min.y <= b.max.y && a.max.y >= b.min.y) &&
            (a.min.z <= b.max.z && a.max.z >= b.min.z);
    }

    public void UpdateChunks(Vector3 viewerPosition)
    {
        Vector3Int viewerChunkPos = World2ChunkPos(viewerPosition - chunkSystemSO.chunkSize / 2) / chunkSystemSO.chunkSizeRatio;

        sw.Start();
        if (viewerChunkPos != lastViewerChunkPos)
        {
            ClearPendingQueue();
            UpdateLoadedChunks(viewerChunkPos * chunkSystemSO.chunkSizeRatio);
            UnloadDistantChunks(viewerChunkPos * chunkSystemSO.chunkSizeRatio);
            lastViewerChunkPos = viewerChunkPos;
        }
        ProcessPendingChunk();
        sw.Stop();
        sw.Reset();
    }

    public void UnloadDistantChunks(Vector3Int viewerChunkPos)
    {
        int keysCount = 0;
        Vector3Int[] keys = new Vector3Int[loadedChunks.Keys.Count];

        foreach(Vector3Int key in loadedChunks.Keys)
        {
            if (Vector3Int.Distance(key, viewerChunkPos) > chunkRenderDistance * chunkSystemSO.chunkSizeRatio)
            {
                keys[keysCount++] = key;
            }
        }

        for (int i = 0; i < keysCount; i++)
        {
            Vector3Int key = keys[i];
            loadedChunks[key].Dispose();
            loadedChunks.Remove(key);
        }
    }

    public void ClearPendingQueue()
    {
        while (pendingChunkList.Count > 0)
        {
            PendingChunkData pendingChunk = pendingChunkList.First.Value;
            pendingChunkList.RemoveFirst();
            if (!loadedChunks.ContainsKey(pendingChunk.position))
                continue;
            loadedChunks[pendingChunk.position].SetPending(false);
        }
    }

    public void ProcessPendingChunk()
    {
        while (pendingChunkList.Count > 0 &&
            sw.ElapsedMilliseconds < chunkProcessTime)
        {
            PendingChunkData pendingChunk = pendingChunkList.First.Value;
            pendingChunkList.RemoveFirst();

            if (!loadedChunks.ContainsKey(pendingChunk.position))
                continue;

            if (pendingChunk.meshOnly)
            {
                loadedChunks[pendingChunk.position].BuildMeshOnly(
                    pendingChunk.drawgrass,
                    pendingChunk.buildCollision);
            }else
            {

                loadedChunks[pendingChunk.position].Build(
                pendingChunk.resolution,
                pendingChunk.ongpu,
                pendingChunk.drawgrass,
                pendingChunk.buildCollision);
            }

            loadedChunks[pendingChunk.position].SetPending(false);
        }
    }

    public void UpdateLoadedChunks(Vector3Int viewerChunkPos)
    {
        int xmin, xmax, zmin, zmax;
        int half = chunkRenderDistance;
        int dst, x, y, z;
        bool isEven = chunkRenderDistance % 2 == 0;

        for (dst = 0; dst <= 3 * half; dst++)
        {
            xmin = dst < chunkRenderDistance ? 0 : dst - chunkRenderDistance;
            xmax = dst < half ? dst : half;

            for (x = xmin; x <= xmax; x++)
            {
                zmin = dst < x + half ? 0 : dst - x - half;
                zmax = dst > x + half ? half : dst - x;
                for (z = zmin; z <= zmax; z++)
                {
                    y = dst - x - z;

                    if (y > chunkHeightRenderDistance)
                        continue;

                    if (isEven)
                    {
                        ProcessChunkEven(viewerChunkPos, x, y, z, dst);
                        //Even
                    } else
                    {
                        ProcessChunkOdd(viewerChunkPos, x, y, z, dst);
                        //Odd
                    }
                }
            }
        }
    }

    public void Dispose()
    {
        foreach(IChunk chunk in loadedChunks.Values)
        {
            chunk.Dispose();
        }
        voxelBuilder.Dispose();
    }

    private void ProcessChunkEven(Vector3Int viewerChunkPos, int x, int y, int z, float dst)
    {
        ProcessChunk(viewerChunkPos, new Vector3Int(x + 1, y + 1, z + 1));
        ProcessChunk(viewerChunkPos, new Vector3Int(x + 1, y + 1, -z));
        ProcessChunk(viewerChunkPos, new Vector3Int(x + 1, -y, z + 1));
        ProcessChunk(viewerChunkPos, new Vector3Int(x + 1, -y, -z));
        ProcessChunk(viewerChunkPos, new Vector3Int(-x, y + 1, z + 1));
        ProcessChunk(viewerChunkPos, new Vector3Int(-x, y + 1, -z));
        ProcessChunk(viewerChunkPos, new Vector3Int(-x, -y, z + 1));
        ProcessChunk(viewerChunkPos, new Vector3Int(-x, -y, -z));
    }

    private void ProcessChunkOdd(Vector3Int viewerChunkPos, int x, int y, int z, float dst)
    {
        ProcessChunk(viewerChunkPos, new Vector3Int(x, y, z));
        if (z != 0) ProcessChunk(viewerChunkPos, new Vector3Int(x, y, -z));
        if (y != 0) ProcessChunk(viewerChunkPos, new Vector3Int(x, -y, z));
        if (y != 0 && z != 0) ProcessChunk(viewerChunkPos, new Vector3Int(x, -y, -z));
        if (x != 0) ProcessChunk(viewerChunkPos, new Vector3Int(-x, y, z));
        if (x != 0 && z != 0) ProcessChunk(viewerChunkPos, new Vector3Int(-x, y, -z));
        if (x != 0 && y != 0) ProcessChunk(viewerChunkPos, new Vector3Int(-x, -y, z));
        if (x != 0 && y != 0 && z != 0) ProcessChunk(viewerChunkPos, new Vector3Int(-x, -y, -z));
    }

    private void ProcessChunk(Vector3Int viewerChunkPos, Vector3Int offset)
    {
        Vector3Int chunkPos = viewerChunkPos + offset;
        Vector3Int chunkPosTransformed = viewerChunkPos + offset * chunkSystemSO.chunkSizeRatio;

        float chunkDistance = Vector3Int.Distance(viewerChunkPos, chunkPos);

        if (chunkDistance > chunkRenderDistance)
            return;

        if (chunkDistance < chunkHighRenderDistance)
        {
            //Full LOD & on GPU
            for (int z = 0; z < 2; z++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int x = 0; x < 2; x++)
                    {
                        UpdateNearestChunk(chunkPosTransformed + new Vector3Int(x, y, z));
                    }
                }
            }
        }
        else
        {
            //Compute LOD & on CPU
            UpdateFarestChunk(viewerChunkPos, offset, chunkDistance);
        }
    }

    private void UpdateFarestChunk(Vector3Int viewerChunkPos, Vector3Int offset, float chunkDistance)
    {
        float lod = CalculateLod(chunkDistance);

        Vector3Int chunkPos = viewerChunkPos + offset * chunkSystemSO.chunkSizeRatio;

        if (loadedChunks.ContainsKey(chunkPos))
        {
            IChunk chunk = loadedChunks[chunkPos];
            if (chunk.size != chunkSystemSO.GetTransformedChunkSize(chunkSystemSO.chunkSizeRatio))
            {
                for (int z = 0; z < 2; z++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        for (int x = 0; x < 2; x++)
                        {
                            Vector3Int sChunkPos = chunkPos + new Vector3Int(x, y, z);
                            if (loadedChunks.ContainsKey(sChunkPos))
                            {
                                loadedChunks[sChunkPos].Dispose();
                                loadedChunks.Remove(sChunkPos);
                            }
                        }
                    }
                }

                Chunk newChunk = new Chunk(
                    voxelBuilder,
                    chunkSystemSO,
                    chunkSystemSO.GetTransformedChunkSize(chunkSystemSO.chunkSizeRatio),
                    Chunk2WorldPos(chunkPos));
                loadedChunks.Add(chunkPos, newChunk);
                EnqueueChunk(chunkPos, lod, false, false, false);
                return;
            }
            if (chunk.isPending)
                return;
            if (chunk.GetChunkResolution() != lod ||
                chunk.GetChunkMemoryLocation() != 2)
                EnqueueChunk(chunkPos, lod, false, false, false);
        }
        else
        {
            Chunk chunk = new Chunk(
                voxelBuilder,
                chunkSystemSO,
                chunkSystemSO.GetTransformedChunkSize(chunkSystemSO.chunkSizeRatio),
                Chunk2WorldPos(chunkPos));
            loadedChunks.Add(chunkPos, chunk);
            EnqueueChunk(chunkPos, lod, false, false, false);
        }
    }


    private float CalculateLod(float distance)
    {
        float lod = chunkSystemSO.lodCurve.Evaluate((distance - chunkHighRenderDistance) / chunkSystemSO.chunkLodRatio) *
            (chunkSystemSO.chunkLodMaxCPU - chunkSystemSO.chunkLodMinCPU) + chunkSystemSO.chunkLodMinCPU;
        if (lod <= 1.5)
            return 1;
        lod = Mathf.Pow(2, Mathf.Ceil(Mathf.Log(lod) / Mathf.Log(2)));
        return lod;
    }
    private void UpdateNearestChunk(Vector3Int chunkPos)
    {
        if (loadedChunks.ContainsKey(chunkPos))
        {
            IChunk chunk = loadedChunks[chunkPos];
            if (chunk.size != chunkSystemSO.GetTransformedChunkSize())
            {
                chunk.Dispose();
                loadedChunks.Remove(chunkPos);
                Chunk newChunk = new Chunk(
                    voxelBuilder,
                    chunkSystemSO,
                    chunkSystemSO.GetTransformedChunkSize(),
                    Chunk2WorldPos(chunkPos));
                loadedChunks.Add(chunkPos, newChunk);
                EnqueueChunk(chunkPos, chunkSystemSO.chunkMaxResolution, true, true, true);
                return;
            }
            if (chunk.isPending)
                return;
            if (chunk.GetChunkResolution() != chunkSystemSO.chunkMaxResolution ||
                chunk.GetChunkMemoryLocation() != 1)
                EnqueueChunk(chunkPos, chunkSystemSO.chunkMaxResolution, true, true, true);
        }
        else
        {
            Chunk chunk = new Chunk(
                voxelBuilder,
                chunkSystemSO,
                chunkSystemSO.GetTransformedChunkSize(),
                Chunk2WorldPos(chunkPos));
            loadedChunks.Add(chunkPos, chunk);
            EnqueueChunk(chunkPos, chunkSystemSO.chunkMaxResolution, true, true, true);
        }
    }

    private void EnqueueChunk(Vector3Int position, float resolution, bool ongpu, bool drawgrass, bool buildCollision)
    {
        loadedChunks[position].SetPending(true);
        pendingChunkList.AddLast(new PendingChunkData(position, resolution, ongpu, drawgrass, buildCollision));
    }

    private void EnqueueChunk(Vector3Int position, bool drawgrass, bool buildCollision)
    {
        loadedChunks[position].SetPending(true);
        pendingChunkList.AddFirst(new PendingChunkData(position, drawgrass, buildCollision));
    }

    private void Initialize()
    {
        loadedChunks = new Dictionary<Vector3Int, IChunk>();
        pendingChunkList = new LinkedList<PendingChunkData>();

        voxelBuilder = new VoxelBuilder(chunkSystemSO.csVoxelBuilder, chunkSystemSO.preset, chunkSystemSO.seed);

        sw = new Stopwatch();
        lastViewerChunkPos = Vector3Int.FloorToInt(Vector3.negativeInfinity);

        LoadChunkSystemSettings();
    }

    private void LoadChunkSystemSettings()
    {
        chunkRenderDistance = ChunkSystemSettings.GetChunkRenderDistance();
        chunkHeightRenderDistance = ChunkSystemSettings.GetChunkHeightRenderDistance();
        chunkHighRenderDistance = ChunkSystemSettings.GetChunkHighRenderDistance();
        chunkProcessTime = ChunkSystemSettings.GetChunkProcessTime();
    }

    private Vector3Int World2ChunkPos(Vector3 pos)
    {
        return Vector3Int.FloorToInt(new Vector3(
            pos.x / (chunkSystemSO.GetTransformedChunkSize().x - 1), 
            pos.y / (chunkSystemSO.GetTransformedChunkSize().y - 1), 
            pos.z / (chunkSystemSO.GetTransformedChunkSize().z - 1)));
    }

    private Vector3 Chunk2WorldPos(Vector3Int pos)
    {
        return new Vector3(
            pos.x * (chunkSystemSO.GetTransformedChunkSize().x - 1),
            pos.y * (chunkSystemSO.GetTransformedChunkSize().y - 1),
            pos.z * (chunkSystemSO.GetTransformedChunkSize().z - 1));
    }
}
