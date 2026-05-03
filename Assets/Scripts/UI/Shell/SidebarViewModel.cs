using System.Collections.Generic;

/// <summary>
/// ViewModel for the sidebar. Provides the navigation category tree with active
/// states and badge counts derived from GameStateSnapshot.
/// Pure C#: no UnityEngine, no UIElements references.
/// </summary>
public class SidebarViewModel : IViewModel
{
    // ── Public display state ──────────────────────────────────────────────

    public IReadOnlyList<NavCategoryData> Categories  => _categories;
    public ScreenId                        ActiveScreenId { get; private set; }
    public bool                            IsCollapsed    { get; private set; }

    // ── IViewModel ────────────────────────────────────────────────────────

    public bool IsDirty    { get; private set; }
    public void ClearDirty() => IsDirty = false;

    // ── Private data ──────────────────────────────────────────────────────

    private readonly List<NavCategoryData> _categories = new List<NavCategoryData>();

    // ── Construction ──────────────────────────────────────────────────────

    public SidebarViewModel()
    {
        BuildCategories();
        ActiveScreenId = ScreenId.DashboardInbox;
    }

    // ── IViewModel.Refresh ────────────────────────────────────────────────

    public void Refresh(GameStateSnapshot snapshot)
    {
        if (snapshot == null) return;

        // Inbox badge — use critical unread count
        int inboxBadge = 0;
        if (snapshot.InboxItems != null)
        {
            int count = snapshot.InboxItems.Count;
            for (int i = 0; i < count; i++)
            {
                if (snapshot.InboxItems[i].IsRead == false)
                    inboxBadge++;
            }
        }

        // Update badge and IsActive for all nodes
        int catCount = _categories.Count;
        for (int c = 0; c < catCount; c++)
        {
            var cat = _categories[c];
            int nodeCount = cat.Children.Count;
            for (int n = 0; n < nodeCount; n++)
            {
                var node = cat.Children[n];
                node.IsActive   = node.ScreenId == ActiveScreenId;
                node.BadgeCount = node.ScreenId == ScreenId.DashboardInbox ? inboxBadge : 0;
            }
        }

        IsDirty = true;
    }

    // ── Navigation state mutation ─────────────────────────────────────────

    public void SetActiveScreen(ScreenId id)
    {
        if (ActiveScreenId == id) return;
        ActiveScreenId = id;

        int catCount = _categories.Count;
        for (int c = 0; c < catCount; c++)
        {
            var cat = _categories[c];
            int nodeCount = cat.Children.Count;
            for (int n = 0; n < nodeCount; n++)
            {
                cat.Children[n].IsActive = cat.Children[n].ScreenId == id;
            }
        }

        IsDirty = true;
    }

    public void SetCollapsed(bool collapsed)
    {
        IsCollapsed = collapsed;
        IsDirty     = true;
    }

    // ── Build category tree ───────────────────────────────────────────────

    private void BuildCategories()
    {
        _categories.Clear();

        _categories.Add(new NavCategoryData
        {
            Id    = NavCategoryId.Overview,
            Label = "OVERVIEW",
            Children =
            {
                new NavNodeData { ScreenId = ScreenId.Dashboard,           Label = "Overview",   IconClass = "icon-dashboard"  },
                new NavNodeData { ScreenId = ScreenId.DashboardInbox,      Label = "Inbox",      IconClass = "icon-inbox"      },
                new NavNodeData { ScreenId = ScreenId.DashboardCalendar,   Label = "Calendar",   IconClass = "icon-calendar"   },
                new NavNodeData { ScreenId = ScreenId.DashboardReputation, Label = "Reputation", IconClass = "icon-reputation" },
            }
        });

        _categories.Add(new NavCategoryData
        {
            Id    = NavCategoryId.HRPortal,
            Label = "HR PORTAL",
            Children =
            {
                new NavNodeData { ScreenId = ScreenId.HRCandidates,  Label = "Candidates",  IconClass = "icon-candidates"  },
                new NavNodeData { ScreenId = ScreenId.HREmployees,   Label = "Employees",   IconClass = "icon-employees"   },
                new NavNodeData { ScreenId = ScreenId.HRTeams,       Label = "Teams",       IconClass = "icon-teams"       },
                new NavNodeData { ScreenId = ScreenId.HRAssignments, Label = "Assignments", IconClass = "icon-assignments" },
            }
        });

        _categories.Add(new NavCategoryData
        {
            Id    = NavCategoryId.Finance,
            Label = "FINANCE",
            Children =
            {
                new NavNodeData { ScreenId = ScreenId.FinanceOverview,         Label = "Overview",     IconClass = "icon-finance"      },
                new NavNodeData { ScreenId = ScreenId.FinanceStockInvestments, Label = "Investments",  IconClass = "icon-investments"  },
                new NavNodeData { ScreenId = ScreenId.FinanceMyInvestments,    Label = "My Portfolio", IconClass = "icon-portfolio"    },
            }
        });

        _categories.Add(new NavCategoryData
        {
            Id    = NavCategoryId.Production,
            Label = "PRODUCTION",
            Children =
            {
                new NavNodeData { ScreenId = ScreenId.ProductionContracts,     Label = "Contracts",     IconClass = "icon-contracts"  },
                new NavNodeData { ScreenId = ScreenId.ProductionProductsInDev, Label = "In Dev",        IconClass = "icon-in-dev"     },
                new NavNodeData { ScreenId = ScreenId.ProductionProductsLive,  Label = "Live Products", IconClass = "icon-live"       },
            }
        });

        _categories.Add(new NavCategoryData
        {
            Id    = NavCategoryId.Market,
            Label = "MARKET",
            Children =
            {
                new NavNodeData { ScreenId = ScreenId.MarketOverview,        Label = "Overview",  IconClass = "icon-market"   },
                new NavNodeData { ScreenId = ScreenId.MarketProductsBrowser, Label = "Products",  IconClass = "icon-products" },
            }
        });

        _categories.Add(new NavCategoryData
        {
            Id    = NavCategoryId.Competitors,
            Label = "COMPETITORS",
            Children =
            {
                new NavNodeData { ScreenId = ScreenId.CompetitorsList,             Label = "Competitor List",  IconClass = "icon-competitors" },
                new NavNodeData { ScreenId = ScreenId.CompetitorsIndustryOverview, Label = "Industry Trends",  IconClass = "icon-industry"    },
            }
        });
    }
}
