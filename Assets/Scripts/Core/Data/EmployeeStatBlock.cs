using System;
using UnityEngine;

[Serializable]
public struct EmployeeStatBlock
{
    public int[] Skills;
    public int[] VisibleAttributes;
    public int[] HiddenAttributes;
    public float[] SkillXp;
    public sbyte[] SkillDeltaDirection;
    public int PotentialAbility;

    public int GetSkill(SkillId id)
    {
        return Skills[(int)id];
    }

    public void SetSkill(SkillId id, int value)
    {
        Skills[(int)id] = value;
    }

    public int GetVisibleAttribute(VisibleAttributeId id)
    {
        return VisibleAttributes[(int)id];
    }

    public void SetVisibleAttribute(VisibleAttributeId id, int value)
    {
        VisibleAttributes[(int)id] = value;
    }

    public int GetHiddenAttribute(HiddenAttributeId id)
    {
        return HiddenAttributes[(int)id];
    }

    public void SetHiddenAttribute(HiddenAttributeId id, int value)
    {
        HiddenAttributes[(int)id] = value;
    }

    public static EmployeeStatBlock Create()
    {
        var block = new EmployeeStatBlock();
        block.Skills = new int[SkillIdHelper.SkillCount];
        block.VisibleAttributes = new int[VisibleAttributeHelper.AttributeCount];
        block.HiddenAttributes = new int[HiddenAttributeHelper.AttributeCount];
        block.SkillXp = new float[SkillIdHelper.SkillCount];
        block.SkillDeltaDirection = new sbyte[SkillIdHelper.SkillCount];
        block.PotentialAbility = 0;
        for (int i = 0; i < VisibleAttributeHelper.AttributeCount; i++)
            block.VisibleAttributes[i] = 10;
        for (int i = 0; i < HiddenAttributeHelper.AttributeCount; i++)
            block.HiddenAttributes[i] = 10;
        return block;
    }

}
