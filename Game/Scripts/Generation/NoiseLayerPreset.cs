using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct NoiseLayerPreset
{
    public NoiseType type;
    public float frequency;
    public float octaves;
    public float offset;
    public Vector3 power;
    public float powvalue;
    public NoiseMixMode mixMode;
}