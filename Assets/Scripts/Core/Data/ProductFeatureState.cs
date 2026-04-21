using System;
using UnityEngine;

[Serializable]
public class ProductFeatureState {
    public string FeatureId;
    public float Quality;
    public float TechnicalDebt;
    public int LastUpgradeTick;
    public bool IsNew;

    public float EffectiveQuality => Mathf.Max(0f, Quality - TechnicalDebt);
}
