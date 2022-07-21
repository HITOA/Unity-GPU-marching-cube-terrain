using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public struct ChunkData
{

    ComputeBuffer voxelsGPU;
    Voxel[,,] voxelsCPU;
    byte voxelsMemoryLocation; //0 : non, 1 : gpu, 2 : cpu

    public Vector3Int sizeWS
    {
        get;
        private set;
    }

    public Vector3Int sizeVoxels
    {
        get
        {
            return new Vector3Int(
                Mathf.CeilToInt((sizeWS.x - 1) / resolution + 1),
                Mathf.CeilToInt((sizeWS.y - 1) / resolution + 1),
                Mathf.CeilToInt((sizeWS.z - 1) / resolution + 1));
        }
    }

    public float resolution;

    public ChunkData(Vector3Int sizeWS)
    {
        this.sizeWS = sizeWS;
        voxelsGPU = null;
        voxelsCPU = null;
        voxelsMemoryLocation = 0;
        resolution = 1;
    }

    public void SetVoxels(ComputeBuffer voxels)
    {
        ClearVoxels();
        voxelsGPU = voxels;
        voxelsMemoryLocation = 1;
    }

    public void SetVoxels(Voxel[,,] voxels)
    {
        ClearVoxels();
        voxelsCPU = voxels;
        voxelsMemoryLocation = 2;
    }

    public int GetVoxelsMemoryLocation()
    {
        return voxelsMemoryLocation;
    }

    public ComputeBuffer GetVoxelsGPU()
    {
        return voxelsGPU;
    }

    public Voxel[,,] GetVoxelsCPU()
    {
        return voxelsCPU;
    }

    public void ClearVoxels()
    {
        if (voxelsMemoryLocation == 1)
            voxelsGPU.Dispose();
        else if (voxelsMemoryLocation == 2)
            voxelsCPU = null;
    }
}