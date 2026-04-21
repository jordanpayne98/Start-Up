using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

public class TeamIdConverter : TypeConverter {
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
        if (value is string s && int.TryParse(s, out int id))
            return new TeamId(id);
        return base.ConvertFrom(context, culture, value);
    }

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
        if (destinationType == typeof(string) && value is TeamId tid)
            return tid.Value.ToString();
        return base.ConvertTo(context, culture, value, destinationType);
    }
}

[TypeConverter(typeof(TeamIdConverter))]
[Serializable]
public struct TeamId
{
    public int Value;
    
    public TeamId(int value)
    {
        Value = value;
    }
    
    public override bool Equals(object obj)
    {
        if (obj is TeamId other)
        {
            return Value == other.Value;
        }
        return false;
    }
    
    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
    
    public static bool operator ==(TeamId a, TeamId b)
    {
        return a.Value == b.Value;
    }
    
    public static bool operator !=(TeamId a, TeamId b)
    {
        return a.Value != b.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}

public class Team
{
    public TeamId id;
    public string name;
    public List<EmployeeId> members;
    public bool isActive;
    public TeamType teamType;
    public bool isCrunching;
    public CompanyId ownerCompanyId;

    private Team() { }

    public Team(TeamId id, string name)
    {
        this.id = id;
        this.name = name;
        this.members = new List<EmployeeId>();
        this.isActive = true;
        this.teamType = TeamType.Contracts;
        this.isCrunching = false;
    }
    
    public int MemberCount => members.Count;
}

[Serializable]
public class TeamState
{
    public Dictionary<TeamId, Team> teams;
    public Dictionary<EmployeeId, TeamId> employeeToTeam;
    public int nextTeamId;

    public TeamState()
    {
        teams = new Dictionary<TeamId, Team>();
        employeeToTeam = new Dictionary<EmployeeId, TeamId>();
        nextTeamId = 1;
    }
}
