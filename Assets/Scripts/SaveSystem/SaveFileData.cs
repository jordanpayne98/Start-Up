using System;
using System.Collections.Generic;

[Serializable]
public class SaveFileData
{
    public int FormatVersion = 1;
    public SaveMetadata Metadata;
    public GameState State;
    public Dictionary<string, int> RngInvocationCounts;
}
