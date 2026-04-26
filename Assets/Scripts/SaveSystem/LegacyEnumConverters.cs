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
