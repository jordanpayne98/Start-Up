using System.Collections.Generic;

public class RngStream : IRng
{
    private System.Random _random;
    private int _invocationCount = 0;

    public int InvocationCount => _invocationCount;

    public RngStream(int seed)
    {
        _random = new System.Random(seed);
    }

    public int Range(int minInclusive, int maxExclusive)
    {
        _invocationCount++;
        return _random.Next(minInclusive, maxExclusive);
    }

    public float NextFloat01()
    {
        _invocationCount++;
        return (float)_random.NextDouble();
    }

    public bool Chance(float probability)
    {
        return NextFloat01() < probability;
    }

    public T Pick<T>(IReadOnlyList<T> items)
    {
        return items[Range(0, items.Count)];
    }

    public void AdvanceTo(int targetCount)
    {
        while (_invocationCount < targetCount)
        {
            _invocationCount++;
            _random.Next();
        }
    }
}
