using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ViewModel managing the new game creation wizard state.
/// Owns temporary NewGameSetupState that only becomes persistent on confirmation.
/// </summary>
public class NewGameFounderCreationViewModel : IViewModel
{
    // ── Step definitions ──
    private static readonly string[] StepNamesOneFounder =
    {
        "Company", "Background", "Founders",
        "Archetype", "Identity & Compensation", "Personality & Weakness",
        "Review"
    };

    private static readonly string[] StepNamesTwoFounders =
    {
        "Company", "Background", "Founders",
        "Archetype", "Identity & Compensation", "Personality & Weakness",
        "Archetype", "Identity & Compensation", "Personality & Weakness",
        "Review"
    };

    private static readonly string[] StepperGroupLabelsOneFounder =
    {
        "Company", "Background", "Founders", "Founder 1", "Review"
    };

    private static readonly string[] StepperGroupLabelsTwoFounders =
    {
        "Company", "Background", "Founders", "Founder 1", "Founder 2", "Review"
    };

    public string[] StepperGroupLabels =>
        SelectedFounderCount == 2 ? StepperGroupLabelsTwoFounders : StepperGroupLabelsOneFounder;

    public int GetStepperGroupCount() => SelectedFounderCount == 2 ? 6 : 5;

    public int GetStepperGroupIndexForStep(int internalStep)
    {
        if (internalStep <= 2) return internalStep;
        if (internalStep <= 5) return 3;
        if (SelectedFounderCount == 2)
        {
            if (internalStep <= 8) return 4;
            return 5;
        }
        return 4;
    }

    public int GetFounderIndexForStep(int step)
    {
        if (step >= 3 && step <= 5) return 0;
        if (SelectedFounderCount == 2 && step >= 6 && step <= 8) return 1;
        return -1;
    }

    public int GetFounderSubStepForStep(int step)
    {
        int fi = GetFounderIndexForStep(step);
        if (fi < 0) return -1;
        return (step - 3) % 3;
    }

    // ── Events ──
    public event Action OnStateChanged;

    // ── Wizard Navigation ──
    public int CurrentStep { get; private set; }

    public int TotalSteps => StepNames.Length;

    public string[] StepNames { get; private set; } = StepNamesOneFounder;

    public string StepLabel => $"Step {CurrentStep + 1} of {TotalSteps}";

    public bool CanGoBack => CurrentStep > 0;

    public bool CanContinue => IsCurrentStepValid();

    public bool IsOnFinalStep => CurrentStep == TotalSteps - 1;

    // ── Setup State ──
    public NewGameSetupState SetupState { get; private set; }

    // ── Company Background Options ──
    public CompanyBackgroundOption[] BackgroundOptions { get; private set; } = Array.Empty<CompanyBackgroundOption>();
    public int SelectedBackgroundIndex { get; private set; } = -1;

    // ── Founder Count Options ──
    public FounderCountOption[] CountOptions { get; private set; }
    public int SelectedFounderCount { get; private set; }

    // ── Validation ──
    public string[] ValidationErrors { get; private set; } = Array.Empty<string>();

    // ── Dependencies ──
    private CompanyBackgroundDefinition[] _backgroundDefinitions;
    private FounderArchetypeDefinition[] _archetypeDefinitions;

    // ── Founder Config Data (per founder index) ──
    public ArchetypeOption[] ArchetypeOptions { get; private set; } = Array.Empty<ArchetypeOption>();

    // ── Salary Options (Page 13 §12) ──
    public static readonly SalaryOptionData[] SalaryOptions =
    {
        new SalaryOptionData { Id = 0, DisplayName = "No Salary",       Description = "Maximum runway. Founder ambition/stress pressure builds over time.", MonthlyAmount = 0,    RunwayImpact = "+3 to +5 months", FuturePressure = "High" },
        new SalaryOptionData { Id = 1, DisplayName = "Low Salary",      Description = "Balanced default. Mild future pressure if company grows.",            MonthlyAmount = 1500, RunwayImpact = "+1 to +2 months", FuturePressure = "Mild" },
        new SalaryOptionData { Id = 2, DisplayName = "Market Salary",   Description = "Lower runway but reduces founder dissatisfaction risk.",              MonthlyAmount = 4000, RunwayImpact = "Baseline",        FuturePressure = "Low" },
        new SalaryOptionData { Id = 3, DisplayName = "Deferred Salary", Description = "Better runway now. Creates future obligation and salary pressure.",    MonthlyAmount = 0,    RunwayImpact = "+2 to +4 months", FuturePressure = "Very High" },
    };

    /// <summary>Returns the monthly cash cost for a given salary option index.</summary>
    public static int GetSalaryAmount(int optionId)
    {
        if (optionId >= 0 && optionId < SalaryOptions.Length)
            return SalaryOptions[optionId].MonthlyAmount;
        return SalaryOptions[2].MonthlyAmount;
    }

    // ── Personality Style Options (Page 13 §10) ──
    public static readonly PersonalityStyleOption[] PersonalityStyleOptions =
    {
        new PersonalityStyleOption { Id = 0, DisplayName = "Driven",        Description = "High ambition and work ethic. Pushes hard but risks burnout.", AttributeModifiers = "+Work Ethic, +Ambition, +Initiative" },
        new PersonalityStyleOption { Id = 1, DisplayName = "Calm",          Description = "Consistent and composed under pressure. Lower initiative.", AttributeModifiers = "+Composure, +Consistency, +Pressure Tolerance" },
        new PersonalityStyleOption { Id = 2, DisplayName = "Collaborative", Description = "Team-first mindset. Strong leadership but weaker solo focus.", AttributeModifiers = "+Communication, +Leadership, +Mentoring" },
        new PersonalityStyleOption { Id = 3, DisplayName = "Inventive",     Description = "Creative and adaptable. Risk of scope creep.", AttributeModifiers = "+Creativity, +Adaptability, +Learning Rate" },
        new PersonalityStyleOption { Id = 4, DisplayName = "Disciplined",   Description = "Reliable and focused. Less creative but very consistent.", AttributeModifiers = "+Focus, +Consistency, +Release Reliability" },
        new PersonalityStyleOption { Id = 5, DisplayName = "Commercial",    Description = "Sales and marketing mindset. Higher ego risk.", AttributeModifiers = "+Communication, +Ambition" },
        new PersonalityStyleOption { Id = 6, DisplayName = "Supportive",    Description = "Strong mentor and team stabiliser. Lower ambition.", AttributeModifiers = "+Mentoring, +Loyalty, +Communication" },
        new PersonalityStyleOption { Id = 7, DisplayName = "Independent",   Description = "High solo output. Weaker in collaborative roles.", AttributeModifiers = "+Focus, +Initiative" },
    };

    // ── Weakness Options (Page 13 §11) ──
    public static readonly WeaknessOption[] WeaknessOptions =
    {
        new WeaknessOption { Id = 0, DisplayName = "Poor Communicator", Description = "Lower Communication, higher coordination risk.",       Risk = "Team misalignment" },
        new WeaknessOption { Id = 1, DisplayName = "Easily Distracted", Description = "Lower Focus, higher output variance.",                  Risk = "Inconsistent delivery" },
        new WeaknessOption { Id = 2, DisplayName = "Stubborn",          Description = "Lower Adaptability, pivot penalties.",                  Risk = "Slow to change direction" },
        new WeaknessOption { Id = 3, DisplayName = "Stress-Prone",      Description = "Lower Composure, weaker under pressure.",               Risk = "Performance drops under crunch" },
        new WeaknessOption { Id = 4, DisplayName = "Perfectionist",     Description = "Higher polish potential, slower delivery.",             Risk = "Scope creep, delayed releases" },
        new WeaknessOption { Id = 5, DisplayName = "Overworker",        Description = "Higher Work Ethic short term, burnout long term.",      Risk = "Burnout, health pressure" },
        new WeaknessOption { Id = 6, DisplayName = "Low Presence",      Description = "Lower Leadership, weaker team stabilisation.",          Risk = "Reduced morale anchoring" },
        new WeaknessOption { Id = 7, DisplayName = "Lone Wolf",         Description = "Stronger solo work, weaker team communication.",        Risk = "Coordination penalties" },
        new WeaknessOption { Id = 8, DisplayName = "Big Ego",           Description = "Higher confidence, but conflict and salary pressure.",  Risk = "Team conflict, salary demands" },
        new WeaknessOption { Id = 9, DisplayName = "Cautious",          Description = "Lower Initiative, avoids reckless actions.",            Risk = "Missed early opportunities" },
    };

    // ── Per-Founder Preview Data ──
    private readonly FounderPreviewData[] _founderPreviews = new FounderPreviewData[2];

    public FounderPreviewData GetFounderPreview(int founderIndex)
    {
        if (founderIndex < 0 || founderIndex >= _founderPreviews.Length) return new FounderPreviewData();
        return _founderPreviews[founderIndex];
    }

    public NewGameFounderCreationViewModel()
    {
        SetupState = new NewGameSetupState
        {
            Seed = Environment.TickCount | 1
        };

        CountOptions = new[]
        {
            new FounderCountOption
            {
                Count = 1,
                Label = "Solo Founder",
                Pros = new[]
                {
                    "Full creative control",
                    "Lower initial salary costs",
                    "Simpler decision-making",
                    "Faster early pivots"
                },
                Cons = new[]
                {
                    "Narrower skill coverage",
                    "Higher burnout risk",
                    "Slower early development",
                    "Single point of failure"
                }
            },
            new FounderCountOption
            {
                Count = 2,
                Label = "Co-Founders",
                Pros = new[]
                {
                    "Broader skill coverage",
                    "Shared workload",
                    "Complementary strengths",
                    "Better team morale start"
                },
                Cons = new[]
                {
                    "Higher initial salary costs",
                    "Potential disagreements",
                    "Slower early decisions",
                    "Equity split complexity"
                }
            }
        };

        LoadBackgroundDefinitions();
        LoadArchetypeDefinitions();
    }

    /// <summary>
    /// No-op for wizard — state doesn't come from game state.
    /// </summary>
    public void Refresh(GameStateSnapshot snapshot) { }
    public bool IsDirty => false;
    public void ClearDirty() { }

    // ── Navigation ──

    public void GoBack()
    {
        if (!CanGoBack) return;
        CurrentStep--;
        NotifyChanged();
    }

    public void GoForward()
    {
        if (!IsCurrentStepValid()) return;
        if (CurrentStep >= TotalSteps - 1) return;
        CurrentStep++;
        NotifyChanged();
    }

    // ── Company Data (Step 1) ──

    public string CompanyName
    {
        get => SetupState.CompanyName;
        set
        {
            SetupState.CompanyName = value ?? "";
            NotifyChanged();
        }
    }

    public string Industry
    {
        get => SetupState.Industry;
        set
        {
            SetupState.Industry = value ?? "";
            NotifyChanged();
        }
    }

    public string Location
    {
        get => SetupState.Location;
        set
        {
            SetupState.Location = value ?? "";
            NotifyChanged();
        }
    }

    // ── Background Selection (Step 2) ──

    public void SelectBackground(int index)
    {
        if (index < 0 || index >= BackgroundOptions.Length) return;
        SelectedBackgroundIndex = index;
        SetupState.CompanyBackgroundId = BackgroundOptions[index].Id;
        NotifyChanged();
    }

    // ── Founder Count (Step 3) ──

    public void SelectFounderCount(int count)
    {
        if (count < 1 || count > 2) return;
        SelectedFounderCount = count;
        SetupState.InitializeFounderSetups(count);
        StepNames = count == 2 ? StepNamesTwoFounders : StepNamesOneFounder;
        NotifyChanged();
    }

    // ── Randomise ──

    public string GetRandomiseLabel()
    {
        switch (CurrentStep)
        {
            case 0: return "Randomise";
            case 1: return "Randomise Background";
            case 2: return "Randomise";
            default:
                int subStep = GetFounderSubStepForStep(CurrentStep);
                switch (subStep)
                {
                    case 0: return "Randomise Archetype";
                    case 1: return "Randomise Identity";
                    case 2: return "Randomise Personality";
                    default: return IsOnFinalStep ? "Randomise Setup" : "Randomise";
                }
        }
    }

    public void RandomiseCurrentStep()
    {
        var rng = new RngStream(Environment.TickCount);

        switch (CurrentStep)
        {
            case 0:
                string[] prefixes = { "Nova", "Apex", "Zenith", "Pulse", "Vertex", "Helix", "Cipher", "Nexus", "Orbit", "Flux" };
                string[] suffixes = { " Labs", " Studios", " Tech", " Digital", " Systems", " Works", " Software", " Interactive", " Co", " HQ" };
                CompanyName = prefixes[rng.Range(0, prefixes.Length)] + suffixes[rng.Range(0, suffixes.Length)];
                break;
            case 1:
                if (BackgroundOptions.Length > 0)
                    SelectBackground(rng.Range(0, BackgroundOptions.Length));
                break;
            case 2:
                SelectFounderCount(rng.Range(1, 3));
                break;
            default:
                int fi = GetFounderIndexForStep(CurrentStep);
                if (fi < 0) break;
                int subStep = GetFounderSubStepForStep(CurrentStep);
                switch (subStep)
                {
                    case 0:
                        if (ArchetypeOptions.Length > 0)
                            SelectArchetype(fi, rng.Range(0, ArchetypeOptions.Length));
                        break;
                    case 1:
                        SetFounderName(fi, GetRandomFounderName());
                        SelectSalary(fi, rng.Range(0, SalaryOptions.Length));
                        break;
                    case 2:
                        SelectPersonalityStyle(fi, rng.Range(0, PersonalityStyleOptions.Length));
                        SelectWeakness(fi, rng.Range(0, WeaknessOptions.Length));
                        break;
                }
                break;
        }
    }

    // ── Founder Config (Steps 4/5) ──

    public void SelectArchetype(int founderIndex, int archetypeIndex)
    {
        var setup = GetFounderSetup(founderIndex);
        if (setup == null) return;
        var s = setup.Value;
        s.ArchetypeId = archetypeIndex < ArchetypeOptions.Length ? ArchetypeOptions[archetypeIndex].Id : -1;
        SetFounderSetup(founderIndex, s);
        RecalculateFounderPreview(founderIndex);
        NotifyChanged();
    }

    public int GetSelectedArchetypeIndex(int founderIndex)
    {
        var setup = GetFounderSetup(founderIndex);
        if (setup == null) return -1;
        int archetypeId = setup.Value.ArchetypeId;
        for (int i = 0; i < ArchetypeOptions.Length; i++)
            if (ArchetypeOptions[i].Id == archetypeId) return i;
        return -1;
    }

    public void SetFounderName(int founderIndex, string name)
    {
        var setup = GetFounderSetup(founderIndex);
        if (setup == null) return;
        var s = setup.Value;
        s.Name = name ?? "";
        SetFounderSetup(founderIndex, s);
        RecalculateFounderPreview(founderIndex);
        NotifyChanged();
    }

    public string GetFounderName(int founderIndex)
    {
        var setup = GetFounderSetup(founderIndex);
        return setup?.Name ?? "";
    }

    public void SelectPersonalityStyle(int founderIndex, int styleIndex)
    {
        var setup = GetFounderSetup(founderIndex);
        if (setup == null) return;
        var s = setup.Value;
        s.PersonalityStyleId = (styleIndex >= 0 && styleIndex < PersonalityStyleOptions.Length)
            ? PersonalityStyleOptions[styleIndex].Id : -1;
        SetFounderSetup(founderIndex, s);
        RecalculateFounderPreview(founderIndex);
        NotifyChanged();
    }

    public int GetSelectedPersonalityStyleIndex(int founderIndex)
    {
        var setup = GetFounderSetup(founderIndex);
        if (setup == null) return -1;
        int styleId = setup.Value.PersonalityStyleId;
        for (int i = 0; i < PersonalityStyleOptions.Length; i++)
            if (PersonalityStyleOptions[i].Id == styleId) return i;
        return -1;
    }

    public void SelectWeakness(int founderIndex, int weaknessIndex)
    {
        var setup = GetFounderSetup(founderIndex);
        if (setup == null) return;
        var s = setup.Value;
        s.WeaknessId = (weaknessIndex >= 0 && weaknessIndex < WeaknessOptions.Length)
            ? WeaknessOptions[weaknessIndex].Id : -1;
        SetFounderSetup(founderIndex, s);
        RecalculateFounderPreview(founderIndex);
        NotifyChanged();
    }

    public int GetSelectedWeaknessIndex(int founderIndex)
    {
        var setup = GetFounderSetup(founderIndex);
        if (setup == null) return -1;
        int weaknessId = setup.Value.WeaknessId;
        for (int i = 0; i < WeaknessOptions.Length; i++)
            if (WeaknessOptions[i].Id == weaknessId) return i;
        return -1;
    }

    public void SelectSalary(int founderIndex, int salaryOptionIndex)
    {
        var setup = GetFounderSetup(founderIndex);
        if (setup == null) return;
        var s = setup.Value;
        s.SalaryOptionId = salaryOptionIndex;
        SetFounderSetup(founderIndex, s);
        RecalculateFounderPreview(founderIndex);
        NotifyChanged();
    }

    public int GetSelectedSalaryIndex(int founderIndex)
    {
        var setup = GetFounderSetup(founderIndex);
        return setup?.SalaryOptionId ?? 2;
    }

    public string GetEquityExpectation(int founderIndex)
    {
        int salaryIdx = GetSelectedSalaryIndex(founderIndex);
        switch (salaryIdx)
        {
            case 0: return "Equity Expectation 22% – 35%"; // No Salary — highest equity pressure
            case 1: return "Equity Expectation 14% – 22%"; // Low Salary
            case 2: return "Equity Expectation 8% – 14%";  // Market Salary
            case 3: return "Equity Expectation 18% – 28%"; // Deferred Salary — future obligation
            default: return "Equity Expectation —";
        }
    }

    public string GetSalaryRunwayImpact(int founderIndex)
    {
        int salaryIdx = GetSelectedSalaryIndex(founderIndex);
        if (salaryIdx >= 0 && salaryIdx < SalaryOptions.Length)
            return SalaryOptions[salaryIdx].RunwayImpact;
        return "—";
    }

    public string GetSalaryFuturePressure(int founderIndex)
    {
        int salaryIdx = GetSelectedSalaryIndex(founderIndex);
        if (salaryIdx >= 0 && salaryIdx < SalaryOptions.Length)
            return SalaryOptions[salaryIdx].FuturePressure;
        return "—";
    }

    public string GetRandomFounderName()
    {
        var rng = new RngStream(Environment.TickCount);
        string[] firstNames = { "Alex", "Jordan", "Morgan", "Taylor", "Riley", "Casey", "Avery", "Quinn", "Drew", "Blake",
                                 "Jamie", "Sage", "Skyler", "Reece", "Parker", "Harley", "Phoenix", "Finley", "Charlie", "Dana" };
        string[] lastNames = { "Chen", "Patel", "Kim", "Okafor", "Santos", "Nakamura", "Mensah", "Kovač", "Reyes", "Müller",
                                "Singh", "Torres", "Ivanova", "Andersen", "Osei", "Yamamoto", "Ferreira", "Johansson", "Park", "Adeyemi" };
        return firstNames[rng.Range(0, firstNames.Length)] + " " + lastNames[rng.Range(0, lastNames.Length)];
    }

    // ── Stat Preview Generation ──

    private void RecalculateFounderPreview(int founderIndex)
    {
        var setup = GetFounderSetup(founderIndex);
        if (setup == null) return;
        var s = setup.Value;

        var preview = new FounderPreviewData();
        preview.Name = string.IsNullOrWhiteSpace(s.Name) ? "Unnamed Founder" : s.Name;

        int archetypeIdx = -1;
        for (int i = 0; i < ArchetypeOptions.Length; i++)
            if (ArchetypeOptions[i].Id == s.ArchetypeId) { archetypeIdx = i; break; }

        if (archetypeIdx >= 0)
        {
            var arch = ArchetypeOptions[archetypeIdx];
            preview.ArchetypeName = arch.DisplayName;
            preview.ArchetypeBadgeClass = "archetype-" + arch.DisplayName.ToLower().Replace(" ", "-");
            preview.RoleName = RoleIdHelper.GetName(arch.Role);
            preview.Strengths = arch.Strengths;
            preview.Risks = arch.Risks;

            int skillCount = SkillIdHelper.SkillCount;
            var skills = new int[skillCount];
            var rng = new RngStream(SetupState.Seed + founderIndex * 997);

            var biasProfile = _archetypeDefinitions != null && archetypeIdx < _archetypeDefinitions.Length
                ? _archetypeDefinitions[archetypeIdx].SkillBiasProfile
                : null;

            for (int i = 0; i < skillCount; i++)
                skills[i] = rng.Range(3, 8);

            if (biasProfile != null)
            {
                for (int b = 0; b < biasProfile.Length; b++)
                {
                    int si = (int)biasProfile[b];
                    if (si >= 0 && si < skillCount)
                        skills[si] = Mathf.Clamp(skills[si] + rng.Range(4, 8), 0, 25);
                }
            }

            // Apply personality style modifier (skill bias heuristic)
            if (s.PersonalityStyleId >= 0)
                ApplyPersonalityStyleModifiers(skills, s.PersonalityStyleId, skillCount);

            int[] topIndices = GetTopThreeSkillIndices(skills);
            preview.TopSkills = new SkillPreviewEntry[topIndices.Length];
            for (int i = 0; i < topIndices.Length; i++)
            {
                preview.TopSkills[i] = new SkillPreviewEntry
                {
                    SkillName = SkillIdHelper.GetName((SkillId)topIndices[i]),
                    Value = skills[topIndices[i]]
                };
            }

            var archDef = _archetypeDefinitions != null && archetypeIdx < _archetypeDefinitions.Length
                ? _archetypeDefinitions[archetypeIdx] : null;
            if (archDef != null)
            {
                float caBase = Mathf.Lerp(archDef.CAMin, archDef.CAMax, 0.5f);
                float paBase = Mathf.Lerp(archDef.PAMin, archDef.PAMax, 0.5f);
                preview.CAStars = Mathf.Clamp(Mathf.RoundToInt(caBase), 1, 5);
                preview.PAStars = Mathf.Clamp(Mathf.RoundToInt(paBase), 1, 5);
            }
            else
            {
                preview.CAStars = 3;
                preview.PAStars = 3;
            }
        }
        else
        {
            preview.ArchetypeName = "—";
            preview.RoleName = "Select an archetype";
            preview.Strengths = Array.Empty<string>();
            preview.Risks = Array.Empty<string>();
            preview.TopSkills = Array.Empty<SkillPreviewEntry>();
            preview.CAStars = 0;
            preview.PAStars = 0;
        }

        preview.Location = string.IsNullOrWhiteSpace(SetupState.Location) ? "—" : SetupState.Location;

        preview.DepartmentBars = new TeamSnapshotEntry[]
        {
            new TeamSnapshotEntry { Department = "Engineering", Current = founderIndex + 1, Max = 5 },
            new TeamSnapshotEntry { Department = "Product", Current = 0, Max = 5 },
            new TeamSnapshotEntry { Department = "Design", Current = 0, Max = 5 },
            new TeamSnapshotEntry { Department = "Other", Current = 0, Max = 5 }
        };
        preview.TotalTeamSize = founderIndex + 1;

        preview.Company = new CompanyPreviewData
        {
            CompanyName = string.IsNullOrWhiteSpace(SetupState.CompanyName) ? "—" : SetupState.CompanyName,
            Industry = string.IsNullOrWhiteSpace(SetupState.Industry) ? "—" : SetupState.Industry,
            BusinessModel = "B2B",
            Headquarters = string.IsNullOrWhiteSpace(SetupState.Location) ? "—" : SetupState.Location,
            StartingBudget = "$500K",
            Runway = "~18 months"
        };

        _founderPreviews[founderIndex] = preview;
    }

    private static void ApplyPersonalityStyleModifiers(int[] skills, int styleId, int skillCount)
    {
        // Heuristic: boost skill indices based on style to influence preview
        switch (styleId)
        {
            case 0: // Driven
                if ((int)SkillId.Negotiation < skillCount) skills[(int)SkillId.Negotiation] = Mathf.Clamp(skills[(int)SkillId.Negotiation] + 2, 0, 25);
                break;
            case 1: // Calm
                if ((int)SkillId.ReleaseManagement < skillCount) skills[(int)SkillId.ReleaseManagement] = Mathf.Clamp(skills[(int)SkillId.ReleaseManagement] + 2, 0, 25);
                break;
            case 2: // Collaborative
                if ((int)SkillId.HrRecruitment < skillCount) skills[(int)SkillId.HrRecruitment] = Mathf.Clamp(skills[(int)SkillId.HrRecruitment] + 2, 0, 25);
                break;
            case 3: // Inventive
                if ((int)SkillId.UserResearch < skillCount) skills[(int)SkillId.UserResearch] = Mathf.Clamp(skills[(int)SkillId.UserResearch] + 2, 0, 25);
                break;
            case 4: // Disciplined
                if ((int)SkillId.QaTesting < skillCount) skills[(int)SkillId.QaTesting] = Mathf.Clamp(skills[(int)SkillId.QaTesting] + 2, 0, 25);
                break;
            case 5: // Commercial
                if ((int)SkillId.Marketing < skillCount) skills[(int)SkillId.Marketing] = Mathf.Clamp(skills[(int)SkillId.Marketing] + 2, 0, 25);
                if ((int)SkillId.Sales < skillCount) skills[(int)SkillId.Sales] = Mathf.Clamp(skills[(int)SkillId.Sales] + 2, 0, 25);
                break;
            case 6: // Supportive
                if ((int)SkillId.HrRecruitment < skillCount) skills[(int)SkillId.HrRecruitment] = Mathf.Clamp(skills[(int)SkillId.HrRecruitment] + 3, 0, 25);
                break;
            case 7: // Independent
                if ((int)SkillId.ProductDesign < skillCount) skills[(int)SkillId.ProductDesign] = Mathf.Clamp(skills[(int)SkillId.ProductDesign] + 2, 0, 25);
                break;
        }
    }

    private static int[] GetTopThreeSkillIndices(int[] skills)
    {
        int count = Mathf.Min(3, skills.Length);
        int[] indices = new int[count];
        bool[] used = new bool[skills.Length];

        for (int t = 0; t < count; t++)
        {
            int best = -1;
            for (int i = 0; i < skills.Length; i++)
            {
                if (!used[i] && (best < 0 || skills[i] > skills[best]))
                    best = i;
            }
            if (best >= 0) { indices[t] = best; used[best] = true; }
        }
        return indices;
    }

    private NewGameSetupState.FounderSetupData? GetFounderSetup(int founderIndex)
    {
        if (SetupState.FounderSetups == null || founderIndex < 0 || founderIndex >= SetupState.FounderSetups.Length)
            return null;
        return SetupState.FounderSetups[founderIndex];
    }

    private void SetFounderSetup(int founderIndex, NewGameSetupState.FounderSetupData data)
    {
        if (SetupState.FounderSetups == null || founderIndex < 0 || founderIndex >= SetupState.FounderSetups.Length)
            return;
        SetupState.FounderSetups[founderIndex] = data;
    }

    // ── Validation ──

    public bool IsCurrentStepValid()
    {
        switch (CurrentStep)
        {
            case 0:
                return !string.IsNullOrWhiteSpace(SetupState.CompanyName);
            case 1:
                return SelectedBackgroundIndex >= 0;
            case 2:
                return SelectedFounderCount > 0;
            default:
                int fi = GetFounderIndexForStep(CurrentStep);
                if (fi < 0) return true;
                int subStep = GetFounderSubStepForStep(CurrentStep);
                var setup = GetFounderSetup(fi);
                if (setup == null) return false;
                var s = setup.Value;
                switch (subStep)
                {
                    case 0: return s.ArchetypeId >= 0;
                    case 1: return !string.IsNullOrWhiteSpace(s.Name);
                    case 2: return s.PersonalityStyleId >= 0 && s.WeaknessId >= 0;
                    default: return true;
                }
        }
    }

    public string[] GetValidationErrors()
    {
        switch (CurrentStep)
        {
            case 0:
                if (string.IsNullOrWhiteSpace(SetupState.CompanyName))
                    return new[] { "No company name entered — type a name to continue" };
                break;
            case 1:
                if (SelectedBackgroundIndex < 0)
                    return new[] { "No background selected — choose a company background to continue" };
                break;
            case 2:
                if (SelectedFounderCount == 0)
                    return new[] { "No founder count selected — choose 1 or 2 founders to continue" };
                break;
            default:
                int fi = GetFounderIndexForStep(CurrentStep);
                if (fi >= 0)
                {
                    int subStep = GetFounderSubStepForStep(CurrentStep);
                    var setup = GetFounderSetup(fi);
                    if (setup != null)
                    {
                        var s = setup.Value;
                        switch (subStep)
                        {
                            case 0:
                                if (s.ArchetypeId < 0)
                                    return new[] { "Select an archetype for this founder" };
                                break;
                            case 1:
                                if (string.IsNullOrWhiteSpace(s.Name))
                                    return new[] { "Enter a name for this founder" };
                                break;
                            case 2:
                                var errs = new List<string>(2);
                                if (s.PersonalityStyleId < 0)
                                    errs.Add("No personality style selected — choose how this founder operates.");
                                if (s.WeaknessId < 0)
                                    errs.Add("No weakness selected — choose one founder weakness.");
                                if (errs.Count > 0)
                                    return errs.ToArray();
                                break;
                        }
                    }
                }
                break;
        }
        return Array.Empty<string>();
    }

    // ── Continue Button Label ──

    public string GetContinueLabel()
    {
        if (IsOnFinalStep) return "Start Game";
        return "Continue";
    }

    // ── Final Step Data Computation ──

    private static readonly int StartingCashBase = 500000;
    private static readonly string[] MeterNames = { "Creativity", "Coordination", "Reliability", "Technical", "Commercial" };

    public ReviewStepData GetReviewData()
    {
        int monthlyBurn = ComputeMonthlyBurn();
        int runway = monthlyBurn > 0 ? StartingCashBase / monthlyBurn : 99;
        var meters = ComputeTeamMeters();
        float avgMeter = 0f;
        if (meters != null && meters.Length > 0)
        {
            for (int i = 0; i < meters.Length; i++) avgMeter += meters[i].NormalizedValue;
            avgMeter /= meters.Length;
        }

        string teamStrength = avgMeter < 0.3f ? "Weak" : avgMeter < 0.5f ? "Functional" : avgMeter < 0.75f ? "Strong" : "Excellent";
        var errors = ComputeBlockingErrors();
        var warnings = ComputeReviewWarnings(monthlyBurn, runway);
        string foundersSummary = BuildFoundersSummary();
        string bgName = SelectedBackgroundIndex >= 0 && SelectedBackgroundIndex < BackgroundOptions.Length
            ? BackgroundOptions[SelectedBackgroundIndex].Name : "—";

        return new ReviewStepData
        {
            CompanyName = string.IsNullOrWhiteSpace(SetupState.CompanyName) ? "—" : SetupState.CompanyName,
            Industry = string.IsNullOrWhiteSpace(SetupState.Industry) ? "—" : SetupState.Industry,
            BackgroundName = bgName,
            FoundersSummary = foundersSummary,
            MonthlySalaryCost = monthlyBurn > 0 ? $"${monthlyBurn:N0} / mo" : "$0 / mo",
            StartingCash = $"${StartingCashBase:N0}",
            RunwayEstimate = runway >= 99 ? "Unlimited" : $"~{runway} months",
            TeamStrengthLabel = teamStrength,
            CanStartGame = errors.Length == 0,
            BlockingErrors = errors,
            Warnings = warnings
        };
    }

    public FoundingEmployeeData[] GenerateFounderData()
    {
        if (SetupState.FounderSetups == null || SetupState.FounderSetups.Length == 0)
            return Array.Empty<FoundingEmployeeData>();

        var result = new FoundingEmployeeData[SetupState.FounderSetups.Length];
        for (int i = 0; i < SetupState.FounderSetups.Length; i++)
        {
            var setup = SetupState.FounderSetups[i];

            RoleId role = RoleId.SoftwareEngineer;
            string archetypeName = "";
            for (int a = 0; a < ArchetypeOptions.Length; a++)
            {
                if (ArchetypeOptions[a].Id == setup.ArchetypeId)
                {
                    role = ArchetypeOptions[a].Role;
                    archetypeName = ArchetypeOptions[a].DisplayName ?? "";
                    break;
                }
            }

            int salaryChoice = setup.SalaryOptionId;
            int salaryAmount = GetSalaryAmount(salaryChoice);

            result[i] = new FoundingEmployeeData
            {
                Name             = string.IsNullOrWhiteSpace(setup.Name) ? $"Founder {i + 1}" : setup.Name,
                Age              = setup.Age > 0 ? setup.Age : 30,
                Role             = role,
                ArchetypeId      = setup.ArchetypeId,
                PersonalityStyleId = setup.PersonalityStyleId,
                WeaknessId       = setup.WeaknessId,
                SalaryChoice     = salaryChoice,
                SalaryAmount     = salaryAmount,
                TraitId          = -1,
                ArchetypeName    = archetypeName,
                IsFounder        = true
            };
        }
        return result;
    }

    // ── Private computation helpers ──

    private int ComputeMonthlyBurn()
    {
        if (SetupState.FounderSetups == null) return 0;
        int total = 0;
        for (int i = 0; i < SetupState.FounderSetups.Length; i++)
        {
            total += GetSalaryAmount(SetupState.FounderSetups[i].SalaryOptionId);
        }
        return total;
    }

    private MeterPreview[] ComputeTeamMeters()
    {
        var meters = new MeterPreview[5];

        // Compute per-founder skill contributions then average
        float[] meterValues = new float[5];
        int founderCount = SetupState.FounderSetups != null ? SetupState.FounderSetups.Length : 0;

        for (int fi = 0; fi < founderCount; fi++)
        {
            var preview = GetFounderPreview(fi);
            if (preview.TopSkills == null || preview.TopSkills.Length == 0) continue;

            // Map preview skills to meter slots using top skill values
            int topVal = preview.TopSkills.Length > 0 ? preview.TopSkills[0].Value : 5;
            float contribution = Mathf.Clamp01(topVal / 20f);

            // Simple heuristic: archetype drives primary meter
            int archetypeIdx = -1;
            var setup = GetFounderSetup(fi);
            if (setup != null)
            {
                for (int a = 0; a < ArchetypeOptions.Length; a++)
                    if (ArchetypeOptions[a].Id == setup.Value.ArchetypeId) { archetypeIdx = a; break; }
            }

            if (archetypeIdx >= 0)
            {
                var role = ArchetypeOptions[archetypeIdx].Role;
                int meterIndex = RoleToMeterIndex(role);
                meterValues[meterIndex] = Mathf.Max(meterValues[meterIndex], contribution);

                // Spread some secondary value to adjacent meters
                for (int m = 0; m < 5; m++)
                {
                    if (m != meterIndex)
                        meterValues[m] = Mathf.Max(meterValues[m], contribution * 0.35f);
                }
            }
        }

        // Boost meters from personality style
        for (int fi = 0; fi < founderCount; fi++)
        {
            var setup = GetFounderSetup(fi);
            if (setup == null || setup.Value.PersonalityStyleId < 0) continue;
            int styleId = setup.Value.PersonalityStyleId;
            // Map style to primary meter boost
            int m = PersonalityStyleToMeterIndex(styleId);
            meterValues[m] = Mathf.Clamp01(meterValues[m] + 0.08f);
        }

        for (int m = 0; m < 5; m++)
        {
            float v = meterValues[m];
            string quality = v < 0.25f ? "Weak" : v < 0.5f ? "Functional" : v < 0.75f ? "Strong" : "Excellent";
            meters[m] = new MeterPreview { Label = MeterNames[m], NormalizedValue = v, QualityLabel = quality };
        }
        return meters;
    }

    private static int RoleToMeterIndex(RoleId role)
    {
        switch (role)
        {
            case RoleId.ProductDesigner:
            case RoleId.GameDesigner:
            case RoleId.TechnicalArtist:
            case RoleId.AudioDesigner:
                return 0; // Creativity
            case RoleId.HrSpecialist:
            case RoleId.Accountant:
                return 1; // Coordination
            case RoleId.QaEngineer:
            case RoleId.TechnicalSupportSpecialist:
                return 2; // Reliability
            case RoleId.SoftwareEngineer:
            case RoleId.SystemsEngineer:
            case RoleId.SecurityEngineer:
            case RoleId.PerformanceEngineer:
            case RoleId.HardwareEngineer:
            case RoleId.ManufacturingEngineer:
                return 3; // Technical
            case RoleId.Marketer:
            case RoleId.SalesExecutive:
                return 4; // Commercial
            default:
                return 3; // Technical as fallback
        }
    }

    private static int PersonalityStyleToMeterIndex(int styleId)
    {
        switch (styleId)
        {
            case 0: return 3; // Driven → Technical
            case 1: return 2; // Calm → Reliability
            case 2: return 1; // Collaborative → Coordination
            case 3: return 0; // Inventive → Creativity
            case 4: return 2; // Disciplined → Reliability
            case 5: return 4; // Commercial → Commercial
            case 6: return 1; // Supportive → Coordination
            case 7: return 3; // Independent → Technical
            default: return 3;
        }
    }

    private string[] ComputeBlockingErrors()
    {
        var errors = new List<string>(8);
        if (string.IsNullOrWhiteSpace(SetupState.CompanyName)) errors.Add("No company name entered — go back to Step 1 and enter a company name");
        if (SelectedBackgroundIndex < 0) errors.Add("No background selected — go back to Step 2 and choose a company background");
        if (SelectedFounderCount == 0) errors.Add("No founder count selected — go back to Step 3 and choose 1 or 2 founders");
        if (SetupState.FounderSetups != null)
        {
            for (int i = 0; i < SetupState.FounderSetups.Length; i++)
            {
                var s = SetupState.FounderSetups[i];
                int archetypeStep = 3 + i * 3 + 1;
                int identityStep = 3 + i * 3 + 2;
                int personalityStep = 3 + i * 3 + 3;
                if (string.IsNullOrWhiteSpace(s.Name))
                    errors.Add($"Founder {i + 1} has no name — go back to Step {identityStep} and enter a name");
                if (s.ArchetypeId < 0)
                    errors.Add($"Founder {i + 1} has no archetype selected — go back to Step {archetypeStep} and choose an archetype");
                if (s.PersonalityStyleId < 0)
                    errors.Add($"Founder {i + 1} has no personality style selected — go back to Step {personalityStep} and choose a personality style");
                if (s.WeaknessId < 0)
                    errors.Add($"Founder {i + 1} has no weakness selected — go back to Step {personalityStep} and choose a weakness");
            }
        }
        return errors.ToArray();
    }

    private string[] ComputeReviewWarnings(int monthlyBurn, int runway)
    {
        var list = new List<string>(4);
        if (runway < 12) list.Add("Runway is under 12 months — secure contracts early to avoid running out of cash");
        if (monthlyBurn > 24000) list.Add("High combined salary — leaves little margin for unexpected costs");
        return list.ToArray();
    }

    private string BuildFoundersSummary()
    {
        if (SetupState.FounderSetups == null || SetupState.FounderSetups.Length == 0) return "—";
        var parts = new string[SetupState.FounderSetups.Length];
        for (int i = 0; i < SetupState.FounderSetups.Length; i++)
        {
            string name = string.IsNullOrWhiteSpace(SetupState.FounderSetups[i].Name)
                ? $"Founder {i + 1}" : SetupState.FounderSetups[i].Name;
            parts[i] = name;
        }
        return string.Join(", ", parts);
    }

    // ── Internal ──

    private void LoadBackgroundDefinitions()
    {
        _backgroundDefinitions = Resources.LoadAll<CompanyBackgroundDefinition>("CompanyBackgrounds");
        if (_backgroundDefinitions == null || _backgroundDefinitions.Length == 0)
        {
            Debug.LogWarning("[NewGameFounderCreationViewModel] No CompanyBackgroundDefinition assets found in Resources/CompanyBackgrounds/");
            BackgroundOptions = Array.Empty<CompanyBackgroundOption>();
            return;
        }

        BackgroundOptions = new CompanyBackgroundOption[_backgroundDefinitions.Length];
        for (int i = 0; i < _backgroundDefinitions.Length; i++)
        {
            var def = _backgroundDefinitions[i];
            var roleNames = new string[def.RecommendedFounderRoles != null ? def.RecommendedFounderRoles.Length : 0];
            for (int r = 0; r < roleNames.Length; r++)
            {
                roleNames[r] = RoleIdHelper.GetName(def.RecommendedFounderRoles[r]);
            }

            BackgroundOptions[i] = new CompanyBackgroundOption
            {
                Id = def.BackgroundId,
                Name = def.DisplayName,
                Description = def.Description,
                RecommendedRoleNames = roleNames,
                Strengths = def.StartingStrengths ?? Array.Empty<string>(),
                Risks = def.StartingRisks ?? Array.Empty<string>(),
                SuggestedFirstActions = def.SuggestedFirstActions ?? Array.Empty<string>(),
                DifficultyLabel = def.DifficultyLabel ?? "Standard"
            };
        }
    }

    private void NotifyChanged()
    {
        ValidationErrors = GetValidationErrors();
        OnStateChanged?.Invoke();
    }

    private void LoadArchetypeDefinitions()
    {
        _archetypeDefinitions = Resources.LoadAll<FounderArchetypeDefinition>("FounderArchetypes");
        if (_archetypeDefinitions == null || _archetypeDefinitions.Length == 0)
        {
            Debug.LogWarning("[NewGameFounderCreationViewModel] No FounderArchetypeDefinition assets found in Resources/FounderArchetypes/");
            ArchetypeOptions = Array.Empty<ArchetypeOption>();
            return;
        }

        System.Array.Sort(_archetypeDefinitions, (a, b) => a.SortOrder.CompareTo(b.SortOrder));

        ArchetypeOptions = new ArchetypeOption[_archetypeDefinitions.Length];
        for (int i = 0; i < _archetypeDefinitions.Length; i++)
        {
            var def = _archetypeDefinitions[i];
            ArchetypeOptions[i] = new ArchetypeOption
            {
                Id = def.ArchetypeId,
                DisplayName = def.DisplayName,
                Description = def.Description,
                Role = def.Role,
                TopSkills = def.TopSkills ?? Array.Empty<SkillId>(),
                BestEarlyUse = def.BestEarlyUse ?? "",
                Strengths = def.Strengths ?? Array.Empty<string>(),
                Risks = def.Risks ?? Array.Empty<string>(),
                RecommendedPairing = def.RecommendedPairing ?? "",
                IsGated = def.IsGatedArchetype,
                GateCondition = def.GateCondition ?? ""
            };
        }
    }
}

/// <summary>
/// Display data for a company background option card.
/// </summary>
public struct CompanyBackgroundOption
{
    public int Id;
    public string Name;
    public string Description;
    public string[] RecommendedRoleNames;
    public string[] Strengths;
    public string[] Risks;
    public string[] SuggestedFirstActions;
    public string DifficultyLabel;
}

/// <summary>
/// Display data for a founder count option card.
/// </summary>
public struct FounderCountOption
{
    public int Count;
    public string Label;
    public string[] Pros;
    public string[] Cons;
}

/// <summary>Display data for a founder archetype option card.</summary>
public struct ArchetypeOption
{
    public int Id;
    public string DisplayName;
    public string Description;
    public RoleId Role;
    public SkillId[] TopSkills;
    public string BestEarlyUse;
    public string[] Strengths;
    public string[] Risks;
    public string RecommendedPairing;
    public bool IsGated;
    public string GateCondition;
}

/// <summary>Display data for a personality style card (Page 13 §10).</summary>
public struct PersonalityStyleOption
{
    public int Id;
    public string DisplayName;
    public string Description;
    public string AttributeModifiers;
}

/// <summary>Display data for a founder weakness card (Page 13 §11).</summary>
public struct WeaknessOption
{
    public int Id;
    public string DisplayName;
    public string Description;
    public string Risk;
}

/// <summary>Live preview data for a configured founder.</summary>
public struct FounderPreviewData
{
    public string Name;
    public string ArchetypeName;
    public string ArchetypeBadgeClass;
    public string RoleName;
    public string Location;
    public SkillPreviewEntry[] TopSkills;
    public int CAStars;
    public int PAStars;
    public string[] Strengths;
    public string[] Risks;
    public TeamSnapshotEntry[] DepartmentBars;
    public int TotalTeamSize;
    public CompanyPreviewData Company;
}

/// <summary>A single skill name + value for preview display.</summary>
public struct SkillPreviewEntry
{
    public string SkillName;
    public int Value;
}

/// <summary>A single department bar entry for team snapshot.</summary>
public struct TeamSnapshotEntry
{
    public string Department;
    public int Current;
    public int Max;
}

/// <summary>Company preview data block shown during founder steps.</summary>
public struct CompanyPreviewData
{
    public string CompanyName;
    public string Industry;
    public string BusinessModel;
    public string Headquarters;
    public string StartingBudget;
    public string Runway;
}

// ─────────────────────────────────────────────
// Final step data types — Plan 2I
// ─────────────────────────────────────────────

/// <summary>A single team meter entry for the review step.</summary>
public struct MeterPreview
{
    public string Label;
    public float NormalizedValue;
    public string QualityLabel;
}

/// <summary>Aggregated data for the Review step.</summary>
public struct ReviewStepData
{
    public string CompanyName;
    public string Industry;
    public string BackgroundName;
    public string FoundersSummary;
    public string MonthlySalaryCost;
    public string StartingCash;
    public string RunwayEstimate;
    public string TeamStrengthLabel;
    public bool CanStartGame;
    public string[] BlockingErrors;
    public string[] Warnings;
}

/// <summary>Named salary option for Page 13 §12 — 4 options instead of 6 raw amounts.</summary>
public struct SalaryOptionData
{
    public int Id;
    public string DisplayName;
    public string Description;
    public int MonthlyAmount;
    public string RunwayImpact;
    public string FuturePressure;
}
