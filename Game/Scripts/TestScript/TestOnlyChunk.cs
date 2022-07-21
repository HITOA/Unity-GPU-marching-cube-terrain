using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestOnlyChunk : MonoBehaviour
{
    [SerializeField]
    ChunkSystemSO cSo;
    [SerializeField]
    Transform viewer;
    [SerializeField]
    int renderDistance;

    public ChunkSystem chunkSystem;

    private void Start()
    {
        ChunkSystemSettings.SetChunkRenderDistance(renderDistance);
        chunkSystem = new ChunkSystem(cSo);
    }

    private void Update()
    {
        chunkSystem.UpdateChunks(viewer.position);
    }

    private void OnDisable()
    {
        chunkSystem.Dispose();
    }
}
