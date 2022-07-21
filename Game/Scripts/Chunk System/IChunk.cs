using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IChunk : System.IDisposable
{
    public Vector3 position
    {
        get;
    }
    public Vector3Int size
    {
        get;
    }
    public bool isPending
    {
        get;
    }
    public void Build(float resolution, bool storeingpu, bool drawGrass, bool buildCollision);
    public void BuildMeshOnly(bool drawGrass, bool buildCollision);
    public void EditSpherical(Vector3 position, float radius, float power);
    public float GetChunkResolution();
    public float GetChunkMemoryLocation();
    public void SetPending(bool v);
}
