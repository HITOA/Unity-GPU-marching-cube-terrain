using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GenerationPreset", menuName = "Game/Generation Preset", order = 1)]
public class GenerationPreset : ScriptableObject
{
    [field: SerializeField]
    public Vector2 minMaxHeight
    {
        get;
        private set;
    }
    [field: SerializeField]
    public NoiseLayerPreset[] layers
    {
        get;
        private set;
    }
}
