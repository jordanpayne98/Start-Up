public interface IWorkContributor
{
    int GetProgrammingWorkContribution(int tick);
    int GetDesignWorkContribution(int tick);
    int GetQAWorkContribution(int tick);
}
