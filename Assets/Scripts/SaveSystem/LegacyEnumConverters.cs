using System;
using Newtonsoft.Json;

public class LegacyTeamTypeConverter : JsonConverter<TeamType>
{
    public override TeamType ReadJson(JsonReader reader, Type objectType, TeamType existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return TeamType.Development;

        string raw = reader.Value?.ToString() ?? "";
        switch (raw)
        {
            case "Development": return TeamType.Development;
            case "Design":      return TeamType.Design;
            case "QA":          return TeamType.QA;
            case "Marketing":   return TeamType.Marketing;
            case "HR":          return TeamType.HR;
            case "Contracts":   return TeamType.Development;
            case "Programming": return TeamType.Development;
            case "SFX":         return TeamType.Development;
            case "VFX":         return TeamType.Development;
            case "Accounting":  return TeamType.Development;
            default:
                if (int.TryParse(raw, out int intVal))
                    return MapLegacyTeamTypeInt(intVal);
                return TeamType.Development;
        }
    }

    private static TeamType MapLegacyTeamTypeInt(int intVal)
    {
        switch (intVal)
        {
            case 0: return TeamType.Development;
            case 1: return TeamType.Development;
            case 2: return TeamType.Design;
            case 3: return TeamType.Development;
            case 4: return TeamType.Development;
            case 5: return TeamType.Marketing;
            case 6: return TeamType.Development;
            case 7: return TeamType.HR;
            case 8: return TeamType.QA;
            default: return TeamType.Development;
        }
    }

    public override void WriteJson(JsonWriter writer, TeamType value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString());
    }
}

public class LegacyProductTeamRoleConverter : JsonConverter<ProductTeamRole>
{
    public override ProductTeamRole ReadJson(JsonReader reader, Type objectType, ProductTeamRole existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return ProductTeamRole.Development;

        string raw = reader.Value?.ToString() ?? "";
        switch (raw)
        {
            case "Development": return ProductTeamRole.Development;
            case "Design":      return ProductTeamRole.Design;
            case "QA":          return ProductTeamRole.QA;
            case "Marketing":   return ProductTeamRole.Marketing;
            case "Programming": return ProductTeamRole.Development;
            case "SFX":         return ProductTeamRole.Development;
            case "VFX":         return ProductTeamRole.Development;
            default:
                if (int.TryParse(raw, out int intVal))
                    return MapLegacyProductTeamRoleInt(intVal);
                return ProductTeamRole.Development;
        }
    }

    private static ProductTeamRole MapLegacyProductTeamRoleInt(int intVal)
    {
        switch (intVal)
        {
            case 0: return ProductTeamRole.Development;
            case 1: return ProductTeamRole.Design;
            case 2: return ProductTeamRole.QA;
            case 3: return ProductTeamRole.Development;
            case 4: return ProductTeamRole.Development;
            case 5: return ProductTeamRole.Marketing;
            default: return ProductTeamRole.Development;
        }
    }

    public override void WriteJson(JsonWriter writer, ProductTeamRole value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString());
    }
}

public class LegacyRoleIdConverter : JsonConverter<RoleId>
{
    public override RoleId ReadJson(JsonReader reader, Type objectType, RoleId existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return RoleId.SoftwareEngineer;

        string raw = reader.Value?.ToString() ?? "";

        // Try new RoleId name first
        if (System.Enum.TryParse<RoleId>(raw, out RoleId newRole))
            return newRole;

        // Map old EmployeeRole names to new RoleId
        switch (raw)
        {
            case "Developer":     return RoleId.SoftwareEngineer;
            case "Designer":      return RoleId.ProductDesigner;
            case "QAEngineer":    return RoleId.QaEngineer;
            case "HR":            return RoleId.HrSpecialist;
            case "SoundEngineer": return RoleId.AudioDesigner;
            case "VFXArtist":     return RoleId.TechnicalArtist;
            case "Accountant":    return RoleId.Accountant;
            case "Marketer":      return RoleId.Marketer;
            default:
                if (int.TryParse(raw, out int intVal))
                    return MapLegacyRoleInt(intVal);
                return RoleId.SoftwareEngineer;
        }
    }

    private static RoleId MapLegacyRoleInt(int intVal)
    {
        switch (intVal)
        {
            case 0: return RoleId.SoftwareEngineer;
            case 1: return RoleId.ProductDesigner;
            case 2: return RoleId.QaEngineer;
            case 4: return RoleId.HrSpecialist;
            case 5: return RoleId.AudioDesigner;
            case 6: return RoleId.TechnicalArtist;
            case 7: return RoleId.Accountant;
            case 8: return RoleId.Marketer;
            default: return RoleId.SoftwareEngineer;
        }
    }

    public override void WriteJson(JsonWriter writer, RoleId value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString());
    }
}

public class LegacySkillIdConverter : JsonConverter<SkillId>
{
    public override SkillId ReadJson(JsonReader reader, Type objectType, SkillId existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return SkillId.Programming;

        string raw = reader.Value?.ToString() ?? "";

        // Try new SkillId name first
        if (System.Enum.TryParse<SkillId>(raw, out SkillId newSkill))
            return newSkill;

        // Map old SkillType names to new SkillId
        switch (raw)
        {
            case "Programming":  return SkillId.Programming;
            case "Design":       return SkillId.ProductDesign;
            case "QA":           return SkillId.QaTesting;
            case "VFX":          return SkillId.Vfx;
            case "SFX":          return SkillId.AudioDesign;
            case "HR":           return SkillId.HrRecruitment;
            case "Negotiation":  return SkillId.Negotiation;
            case "Accountancy":  return SkillId.Accountancy;
            case "Marketing":    return SkillId.Marketing;
            default:
                if (int.TryParse(raw, out int intVal) && intVal >= 0 && intVal < SkillIdHelper.SkillCount)
                    return (SkillId)intVal;
                return SkillId.Programming;
        }
    }

    public override void WriteJson(JsonWriter writer, SkillId value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString());
    }
}
