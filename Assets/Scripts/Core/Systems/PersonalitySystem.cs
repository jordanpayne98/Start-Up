using System;

public class PersonalitySystem : ISystem
{
    // Spawn weights — total 100
    // Collaborative=14, Professional=13, Easygoing=13, Independent=12,
    // Competitive=12, Perfectionist=12, Intense=10, Abrasive=8, Volatile=6
    private static readonly int[] SpawnWeights = { 14, 13, 13, 12, 12, 12, 10, 8, 6 };

    // Compatibility matrix — upper triangle of 9x9 symmetric matrix.
    // Index ordering: [i][j] where i <= j, packed as i*(9+9-i-1)/2 + j-i-1
    // Values range [-3, +3].
    // Personality order: Col=0, Pro=1, Easy=2, Ind=3, Comp=4, Perf=5, Int=6, Abr=7, Vol=8
    private static readonly int[,] CompatMatrix = new int[9, 9]
    {
        //        Col  Pro  Easy Ind  Comp Perf Int  Abr  Vol
        /* Col */ {  0,  2,   3,  1,   0,   1,  -1,  -2,  -3 },
        /* Pro */ {  2,  0,   1,  2,   1,   2,   0,  -2,  -3 },
        /* Easy*/  {  3,  1,   0,  1,   0,   0,  -1,  -2,  -3 },
        /* Ind */ {  1,  2,   1,  0,   1,  -1,   0,  -1,  -2 },
        /* Comp*/  {  0,  1,   0,  1,   0,   2,   1,  -1,  -2 },
        /* Perf*/  {  1,  2,   0, -1,   2,   0,   1,  -2,  -3 },
        /* Int */ { -1,  0,  -1,  0,   1,   1,   0,  -2,  -2 },
        /* Abr */ { -2, -2,  -2, -1,  -1,  -2,  -2,   0,  -1 },
        /* Vol */ { -3, -3,  -3, -2,  -2,  -3,  -2,  -1,   0 },
    };

    public static int GetBaseCompatibility(Personality a, Personality b)
    {
        return CompatMatrix[(int)a, (int)b];
    }

    public static bool IsDisruptive(Personality p)
    {
        return p == Personality.Abrasive || p == Personality.Volatile;
    }

    public static Personality GeneratePersonality(IRng rng)
    {
        int total = 0;
        for (int i = 0; i < SpawnWeights.Length; i++) total += SpawnWeights[i];
        int roll = rng.Range(0, total);
        int cumulative = 0;
        for (int i = 0; i < SpawnWeights.Length; i++)
        {
            cumulative += SpawnWeights[i];
            if (roll < cumulative)
                return (Personality)i;
        }
        return Personality.Collaborative;
    }

    public static ReadOnlySpan<int> GetSpawnWeights()
    {
        return SpawnWeights;
    }

    public void PreTick(int tick) { }
    public void Tick(int tick) { }
    public void PostTick(int tick) { }
    public void ApplyCommand(ICommand command) { }
    public void Dispose() { }
}
