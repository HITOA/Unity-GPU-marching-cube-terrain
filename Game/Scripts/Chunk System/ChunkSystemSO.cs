using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Chunk System SO", menuName = "Game/ChunkSystemSO", order = 1)]
public class ChunkSystemSO : ScriptableObject
{
    [field: SerializeField]
    public float seed
    {
        get;
        private set;
    }
    [field: SerializeField]
    public ComputeShader csVoxelBuilder
    {
        get;
        private set;
    }
    [field: SerializeField]
    public ComputeShader csMeshBuilder
    {
        get;
        private set;
    }
    [field: SerializeField]
    public Material groundMat
    {
        get;
        private set;
    }
    [field: SerializeField]
    public Material grassMat
    {
        get;
        private set;
    }
    [field: SerializeField]
    public float isolevel
    {
        get;
        private set;
    }
    [field: SerializeField]
    public Vector3Int chunkSize
    {
        get;
        private set;
    }
    [field: SerializeField]
    public float chunkMaxResolution
    {
        get;
        private set;
    }
    [field: SerializeField]
    public AnimationCurve lodCurve
    {
        get;
        private set;
    }
    [field: SerializeField]
    public float chunkLodRatio
    {
        get;
        private set;
    }
    [field: SerializeField]
    public int chunkLodMinCPU
    {
        get;
        private set;
    }
    [field: SerializeField]
    public int chunkLodMaxCPU
    {
        get;
        private set;
    }
    [field: SerializeField]
    public int chunkSizeRatio
    {
        get;
        private set;
    }
    [field: SerializeField]
    public GenerationPreset preset
    {
        get;
        private set;
    }

    public Vector3Int GetTransformedChunkSize()
    {
        return chunkSize + Vector3Int.one;
    }

    public Vector3Int GetTransformedChunkSize(float scale)
    {
        return new Vector3Int(
            (int)(chunkSize.x * scale), 
            (int)(chunkSize.y * scale), 
            (int)(chunkSize.z * scale)) 
            + Vector3Int.one;
    }
}
