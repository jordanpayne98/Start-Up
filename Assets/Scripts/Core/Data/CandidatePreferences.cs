public enum FtPtPreference
{
    PrefersFullTime,
    Flexible,
    PrefersPartTime
}

public enum LengthPreference
{
    PrefersSecurity,
    NoPreference,
    PrefersFlexibility
}

public struct CandidatePreferences
{
    public FtPtPreference FtPtPref;
    public LengthPreference LengthPref;
}

public enum PreferenceMatchState
{
    BothMatched,
    OneMatchedOneNeutral,
    BothNeutral,
    OneMismatched,
    BothMismatched
}
