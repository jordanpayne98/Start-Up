using System.Collections.Generic;

public struct CompanyRankVM
{
    public CompetitorId? Id;
    public string CompanyName;
    public string MetricDisplay;
    public bool IsPlayer;
}

public struct SalaryBenchmarkVM
{
    public string TierName;
    public string BenchmarkSalary;
    public string PlayerAvgSalary;
    public string Delta;
}

public struct DisruptionVM
{
    public string EventName;
    public string Description;
    public bool IsMajor;
    public string TicksRemaining;
}

public class IndustryOverviewViewModel : IViewModel
{
    private readonly List<CompanyRankVM> _revenueRanking = new List<CompanyRankVM>();
    private readonly List<CompanyRankVM> _marketShareRanking = new List<CompanyRankVM>();
    private readonly List<CompanyRankVM> _productCountRanking = new List<CompanyRankVM>();
    private readonly List<CompanyRankVM> _reputationRanking = new List<CompanyRankVM>();
    private readonly List<SalaryBenchmarkVM> _salaryBenchmarks = new List<SalaryBenchmarkVM>();
    private readonly List<DisruptionVM> _activeDisruptions = new List<DisruptionVM>();

    public List<CompanyRankVM> RevenueRanking => _revenueRanking;
    public List<CompanyRankVM> MarketShareRanking => _marketShareRanking;
    public List<CompanyRankVM> ProductCountRanking => _productCountRanking;
    public List<CompanyRankVM> ReputationRanking => _reputationRanking;
    public List<SalaryBenchmarkVM> SalaryBenchmarks => _salaryBenchmarks;
    public List<DisruptionVM> ActiveDisruptions => _activeDisruptions;

    public string TotalMarketSize { get; private set; }
    public int ActiveCompanyCount { get; private set; }
    public int TotalProductsOnMarket { get; private set; }

    public void Refresh(IReadOnlyGameState state) {
        if (state == null) return;
        var snapshot = state as GameStateSnapshot;
        if (snapshot == null) return;
        Refresh(snapshot.CompetitorState, snapshot.MarketStateRef, snapshot.ProductStateRef,
                snapshot.StockState, snapshot.DisruptionStateRef, snapshot.CurrentTick, snapshot.CompanyName,
                snapshot.EmployeeStateRef);
    }

    public void Refresh(CompetitorState compState, MarketState marketState, ProductState productState,
        StockState stockState, DisruptionState disruptionState, int currentTick, string playerCompanyName,
        EmployeeState employeeState = null) {
        if (compState == null) return;

        _revenueRanking.Clear();
        _marketShareRanking.Clear();
        _productCountRanking.Clear();
        _reputationRanking.Clear();
        _salaryBenchmarks.Clear();
        _activeDisruptions.Clear();

        var allRevenue = new List<CompanyRankVM>();
        var allShare = new List<CompanyRankVM>();
        var allProducts = new List<CompanyRankVM>();
        var allReputation = new List<CompanyRankVM>();

        long totalMarketRevenue = 0L;
        int activeCount = 0;
        int totalProducts = 0;

        foreach (var kvp in compState.competitors) {
            var comp = kvp.Value;
            if (comp.IsBankrupt || comp.IsAbsorbed) continue;
            activeCount++;

            int productCount = comp.ActiveProductIds != null ? comp.ActiveProductIds.Count : 0;
            totalProducts += productCount;
            totalMarketRevenue += comp.Finance.MonthlyRevenue;

            float totalShare = 0f;
            if (comp.NicheMarketShare != null) {
                foreach (var ms in comp.NicheMarketShare) totalShare += ms.Value;
            }

            allRevenue.Add(new CompanyRankVM { Id = comp.Id, CompanyName = comp.CompanyName, MetricDisplay = UIFormatting.FormatMoney(comp.Finance.MonthlyRevenue) });
            allShare.Add(new CompanyRankVM { Id = comp.Id, CompanyName = comp.CompanyName, MetricDisplay = UIFormatting.FormatPercent(totalShare) });
            allProducts.Add(new CompanyRankVM { Id = comp.Id, CompanyName = comp.CompanyName, MetricDisplay = productCount.ToString() });
            allReputation.Add(new CompanyRankVM { Id = comp.Id, CompanyName = comp.CompanyName, MetricDisplay = comp.ReputationPoints.ToString() });
        }

        if (!string.IsNullOrEmpty(playerCompanyName)) {
            activeCount++;
        }

        if (productState != null) {
            int playerProducts = productState.shippedProducts != null ? productState.shippedProducts.Count : 0;
            totalProducts += playerProducts;
        }

        ActiveCompanyCount = activeCount;
        TotalProductsOnMarket = totalProducts;
        TotalMarketSize = UIFormatting.FormatMoney(totalMarketRevenue);

        SortDescendingByMetric(allRevenue);
        SortDescendingByMetric(allShare);
        SortDescendingIntByMetric(allProducts);
        SortDescendingIntByMetric(allReputation);

        CopyTop5(allRevenue, _revenueRanking);
        CopyTop5(allShare, _marketShareRanking);
        CopyTop5(allProducts, _productCountRanking);
        CopyTop5(allReputation, _reputationRanking);

        BuildSalaryBenchmarks(compState, employeeState);
        BuildDisruptions(disruptionState, currentTick);
    }

    private void BuildSalaryBenchmarks(CompetitorState compState, EmployeeState employeeState) {
        long[] industrySum = new long[4];
        int[] industryCount = new int[4];
        long[] playerSum = new long[4];
        int[] playerCount = new int[4];

        if (employeeState != null && employeeState.employees != null) {
            foreach (var kvp in employeeState.employees) {
                var emp = kvp.Value;
                if (!emp.isActive) continue;
                int avg = ComputeAvgSkill(emp);
                int tierIndex = (int)GetTierFromAvgSkill(avg);
                if (emp.ownerCompanyId == CompanyId.Player) {
                    playerSum[tierIndex] += emp.salary;
                    playerCount[tierIndex]++;
                } else {
                    industrySum[tierIndex] += emp.salary;
                    industryCount[tierIndex]++;
                }
            }
        }

        var tiers = System.Enum.GetValues(typeof(SkillTier));
        for (int t = 0; t < tiers.Length; t++) {
            var tier = (SkillTier)tiers.GetValue(t);
            int idx = (int)tier;
            long indAvg = industryCount[idx] > 0 ? industrySum[idx] / industryCount[idx] : 0;
            long plAvg = playerCount[idx] > 0 ? playerSum[idx] / playerCount[idx] : 0;
            long delta = plAvg - indAvg;
            string benchmarkStr = industryCount[idx] > 0 ? UIFormatting.FormatMoney(indAvg) : "--";
            string playerStr = playerCount[idx] > 0 ? UIFormatting.FormatMoney(plAvg) : "--";
            string deltaStr = "--";
            if (industryCount[idx] > 0 && playerCount[idx] > 0) {
                deltaStr = (delta >= 0 ? "+" : "") + UIFormatting.FormatMoney(delta);
            }
            _salaryBenchmarks.Add(new SalaryBenchmarkVM {
                TierName = tier.ToString(),
                BenchmarkSalary = benchmarkStr,
                PlayerAvgSalary = playerStr,
                Delta = deltaStr
            });
        }
    }

    private static int ComputeAvgSkill(Employee emp) {
        if (emp.Stats.Skills == null || emp.Stats.Skills.Length == 0) return 0;
        int sum = 0;
        int len = emp.Stats.Skills.Length;
        for (int i = 0; i < len; i++) sum += emp.Stats.Skills[i];
        return sum / len;
    }

    private static SkillTier GetTierFromAvgSkill(int avg) {
        if (avg >= 14) return SkillTier.Master;
        if (avg >= 10) return SkillTier.Expert;
        if (avg >= 6) return SkillTier.Competent;
        return SkillTier.Apprentice;
    }

    private void BuildDisruptions(DisruptionState disruptionState, int currentTick) {
        if (disruptionState == null || disruptionState.activeDisruptions == null) return;
        int count = disruptionState.activeDisruptions.Count;
        for (int i = 0; i < count; i++) {
            var d = disruptionState.activeDisruptions[i];
            int ticksLeft = (d.StartTick + d.DurationTicks) - currentTick;
            int daysLeft = ticksLeft / TimeState.TicksPerDay;
            _activeDisruptions.Add(new DisruptionVM {
                EventName = d.EventType.ToString(),
                Description = d.Description,
                IsMajor = d.IsMajor,
                TicksRemaining = daysLeft > 0 ? daysLeft + "d" : "Ending"
            });
        }
    }

    private static void SortDescendingByMetric(List<CompanyRankVM> list) {
        int count = list.Count;
        for (int i = 1; i < count; i++) {
            var key = list[i];
            int j = i - 1;
            while (j >= 0 && string.Compare(list[j].MetricDisplay, key.MetricDisplay, System.StringComparison.Ordinal) < 0) {
                list[j + 1] = list[j];
                j--;
            }
            list[j + 1] = key;
        }
    }

    private static void SortDescendingIntByMetric(List<CompanyRankVM> list) {
        int count = list.Count;
        for (int i = 1; i < count; i++) {
            var key = list[i];
            int j = i - 1;
            int keyVal = 0;
            int.TryParse(key.MetricDisplay, out keyVal);
            while (j >= 0) {
                int jVal = 0;
                int.TryParse(list[j].MetricDisplay, out jVal);
                if (jVal >= keyVal) break;
                list[j + 1] = list[j];
                j--;
            }
            list[j + 1] = key;
        }
    }

    private static void CopyTop5(List<CompanyRankVM> source, List<CompanyRankVM> dest) {
        int count = source.Count < 5 ? source.Count : 5;
        for (int i = 0; i < count; i++)
            dest.Add(source[i]);
    }
}
