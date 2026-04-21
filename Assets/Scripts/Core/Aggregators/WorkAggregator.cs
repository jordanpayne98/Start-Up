// WorkAggregator Version: Clean v1
using System.Collections.Generic;

public sealed class WorkAggregator
{
    private readonly List<IWorkContributor> _contributors;
    
    public WorkAggregator()
    {
        _contributors = new List<IWorkContributor>();
    }
    
    public void RegisterContributor(IWorkContributor contributor)
    {
        if (!_contributors.Contains(contributor))
        {
            _contributors.Add(contributor);
        }
    }
    
    public void UnregisterContributor(IWorkContributor contributor)
    {
        _contributors.Remove(contributor);
    }
    
    public int GetTotalProgrammingWork(int tick)
    {
        int total = 0;
        for (int i = 0; i < _contributors.Count; i++)
        {
            total += _contributors[i].GetProgrammingWorkContribution(tick);
        }
        return total;
    }
    
    public int GetTotalDesignWork(int tick)
    {
        int total = 0;
        for (int i = 0; i < _contributors.Count; i++)
        {
            total += _contributors[i].GetDesignWorkContribution(tick);
        }
        return total;
    }
    
    public int GetTotalQAWork(int tick)
    {
        int total = 0;
        for (int i = 0; i < _contributors.Count; i++)
        {
            total += _contributors[i].GetQAWorkContribution(tick);
        }
        return total;
    }
}
