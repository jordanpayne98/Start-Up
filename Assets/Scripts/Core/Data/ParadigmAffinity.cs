using System;
using UnityEngine;

[Serializable]
public struct ParadigmAffinity
{
    public string paradigmId;
    [Range(0f, 2f)]
    public float affinity;
}
