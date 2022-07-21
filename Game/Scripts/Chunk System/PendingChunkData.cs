using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct PendingChunkData
{
    public Vector3Int position;
    public float resolution;
    public bool ongpu;
    public bool drawgrass;
    public bool buildCollision;
    public bool meshOnly;
    public PendingChunkData(Vector3Int position, float resolution, bool ongpu, bool drawgrass, bool buildCollision)
    {
        this.position = position;
        this.resolution = resolution;
        this.ongpu = ongpu;
        this.drawgrass = drawgrass;
        this.buildCollision = buildCollision;
        this.meshOnly = false;
    }

    public PendingChunkData(Vector3Int position, bool drawgrass, bool buildCollision)
    {
        this.position = position;
        this.resolution = 0;
        this.ongpu = false;
        this.drawgrass = drawgrass;
        this.buildCollision = buildCollision;
        this.meshOnly = true;
    }
}
