using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ChunkSystemSettings
{
    #region Chunk Render Distance
    const string chunkRenderDistanceVariableName = "chunkRenderDistance";
    const int chunkRenderDistanceDefaultValue = 16;

    public static int GetChunkRenderDistance()
    {
        return PlayerPrefs.GetInt(
            chunkRenderDistanceVariableName, 
            chunkRenderDistanceDefaultValue);
    }

    public static void SetChunkRenderDistance(int value)
    {
        PlayerPrefs.SetInt(chunkRenderDistanceVariableName, value);
    }
    #endregion
    #region Chunk Height Render Distance
    const string chunkHeightRenderDistanceVariableName = "chunkHeightRenderDistance";
    const int chunkHeightRenderDistanceDefaultValue = 3;

    public static int GetChunkHeightRenderDistance()
    {
        return PlayerPrefs.GetInt(
            chunkHeightRenderDistanceVariableName,
            chunkHeightRenderDistanceDefaultValue);
    }

    public static void SetChunkHeightRenderDistance(int value)
    {
        PlayerPrefs.SetInt(chunkHeightRenderDistanceVariableName, value);
    }
#endregion
    #region Chunk High Render Distance
    const string chunkHighRenderDistanceVariableName = "chunkHighRenderDistance";
    const int chunkHighRenderDistanceDefaultValue = 3;

    public static int GetChunkHighRenderDistance()
    {
        return PlayerPrefs.GetInt(
            chunkHighRenderDistanceVariableName,
            chunkHighRenderDistanceDefaultValue);
    }

    public static void SetChunkHighRenderDistance(int value)
    {
        PlayerPrefs.SetInt(chunkHighRenderDistanceVariableName, value);
    }
    #endregion
    #region Chunk Process Time
    const string chunkProcessTimeVariableName = "chunkProcessTime";
    const float chunkProcessTimeDefaultValue = 5;

    public static float GetChunkProcessTime()
    {
        return PlayerPrefs.GetFloat(
            chunkProcessTimeVariableName,
            chunkProcessTimeDefaultValue);
    }

    public static void SetChunkProcessTime(float value)
    {
        PlayerPrefs.SetFloat(chunkProcessTimeVariableName, value);
    }
    #endregion
}
