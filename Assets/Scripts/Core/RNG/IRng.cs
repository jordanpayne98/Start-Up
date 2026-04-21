using System.Collections.Generic;

public interface IRng
{
    int InvocationCount { get; }
    int Range(int minInclusive, int maxExclusive);
    float NextFloat01();
    bool Chance(float probability);
    T Pick<T>(IReadOnlyList<T> items);
}
