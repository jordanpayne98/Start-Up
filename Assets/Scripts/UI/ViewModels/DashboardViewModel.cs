using System.Collections.Generic;

public class DashboardViewModel : IViewModel
{
    public struct ProductEntry
    {
        public string Name;
        public int    ProgressPercent;
        public string ProgressLabel;
    }

    public struct ContractEntry
    {
        public string Name;
        public string RiskLevel;   // "warning" | "success" | "info"
        public string RiskLabel;
    }

    public struct ActivityEntry
    {
        public string Title;
        public string Subtitle;
        public string Severity;    // "info" | "warning" | "danger" | "success"
    }

    // ── KPI strip ────────────────────────────────────────────────────────────
    public string CompanyName    => "Quantum Pixel Ltd";
    public string CompanyType    => "Software Startup";
    public string CashDisplay    => "£312K";
    public string RunwayDisplay  => "17 months";
    public string EmployeeCount  => "24";
    public string CandidateCount => "128";

    // ── Products ─────────────────────────────────────────────────────────────
    public List<ProductEntry> Products { get; } = new List<ProductEntry>
    {
        new ProductEntry { Name = "TaskForge",    ProgressPercent = 64, ProgressLabel = "64%" },
        new ProductEntry { Name = "PixelDesk OS", ProgressPercent = 18, ProgressLabel = "18%" },
        new ProductEntry { Name = "BugShield",    ProgressPercent = 92, ProgressLabel = "92%" },
    };

    // ── Contracts ────────────────────────────────────────────────────────────
    public List<ContractEntry> Contracts { get; } = new List<ContractEntry>
    {
        new ContractEntry { Name = "Security Audit", RiskLevel = "warning", RiskLabel = "Medium Risk"  },
        new ContractEntry { Name = "UX Review",       RiskLevel = "success", RiskLabel = "Low Risk"     },
        new ContractEntry { Name = "Optimisation",    RiskLevel = "info",    RiskLabel = "High Reward"  },
    };

    // ── Recent activity ───────────────────────────────────────────────────────
    public List<ActivityEntry> RecentActivity { get; } = new List<ActivityEntry>
    {
        new ActivityEntry { Title = "Contract signed",    Subtitle = "Security Audit — 3 days ago",        Severity = "success" },
        new ActivityEntry { Title = "Runway warning",     Subtitle = "Burn rate increased this month",      Severity = "warning" },
        new ActivityEntry { Title = "New candidate",      Subtitle = "Senior Engineer applied — yesterday", Severity = "info"    },
        new ActivityEntry { Title = "BugShield nearing",  Subtitle = "Completion at 92% — review due",      Severity = "info"    },
    };

    // ── IViewModel ───────────────────────────────────────────────────────────
    public bool IsDirty => false;

    public void Refresh(GameStateSnapshot snapshot) { /* static mock — no-op */ }

    public void ClearDirty() { /* no-op */ }
}
