using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;

public class CommandHistoryInspector : MonoBehaviour
{
    // Kept for backward-compat — GameController still calls this
    public static void RecordCommand(ICommand cmd) { }

    private static CommandHistoryInspector _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        var go = new GameObject("__CommandHistoryInspector__");
        go.hideFlags = HideFlags.HideInHierarchy;
        DontDestroyOnLoad(go);

        var uiDoc = go.AddComponent<UIDocument>();

        PanelSettings panelSettings = null;
        var allDocs = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
        for (int i = 0; i < allDocs.Length; i++)
        {
            if (allDocs[i] != uiDoc && allDocs[i].panelSettings != null)
            {
                panelSettings = allDocs[i].panelSettings;
                break;
            }
        }
        if (panelSettings == null)
            panelSettings = Resources.Load<PanelSettings>("DebugPanelSettings");
        if (panelSettings != null)
            uiDoc.panelSettings = panelSettings;

        uiDoc.sortingOrder = 999;

        _instance = go.AddComponent<CommandHistoryInspector>();
        _instance._uiDocument = uiDoc;
    }

    // ─── Fields ──────────────────────────────────────────────────────────────────

    private UIDocument _uiDocument;
    private VisualElement _panelContainer;
    private bool _isVisible;
    private bool _initialized;

    private GameController _gc;

    // Tabs
    private int _activeTab = 0;
    private Button _tabState;
    private Button _tabTuning;
    private VisualElement _statePanel;
    private VisualElement _tuningPanel;

    // State tab
    private ScrollView _stateScroll;

    // Tuning tab
    private TextField _tuningSearchField;
    private ScrollView _tuningScroll;
    private string _tuningFilter = "";
    private Dictionary<string, object> _tuningDefaults;

    // Cheats tab
    private Button _tabCheats;
    private VisualElement _cheatsPanel;
    private ScrollView _cheatsScroll;
    private EmployeeId? _selectedEmployeeId;
    private bool _unlimitedFunds;
    private IRng _spawnRng;

    // Colors — dark panel, white labels, black text inside input fields
    private static readonly Color PanelBg        = new Color(0.08f, 0.08f, 0.10f, 0.97f);
    private static readonly Color TitleBg         = new Color(0.05f, 0.05f, 0.08f, 1f);
    private static readonly Color TabActiveBg     = new Color(0.12f, 0.20f, 0.35f, 1f);
    private static readonly Color TabInactiveBg   = new Color(0.06f, 0.06f, 0.09f, 1f);
    private static readonly Color AccentBlue      = new Color(0.5f, 0.72f, 1f);
    private static readonly Color SectionColor    = new Color(0.4f, 0.65f, 0.9f);
    private static readonly Color RowAlt          = new Color(0.05f, 0.05f, 0.08f, 0.8f);
    private static readonly Color ModifiedYellow  = new Color(0.9f, 0.75f, 0.0f);   // darker gold — readable on white
    private static readonly Color MutedGray       = new Color(0.55f, 0.55f, 0.55f);
    private static readonly Color FieldBg         = new Color(1f, 1f, 1f, 1f);       // white input background
    private static readonly Color FieldText       = new Color(0.07f, 0.07f, 0.07f); // near-black input text
    private static readonly Color FieldModified   = new Color(0.55f, 0.40f, 0.0f);  // dark amber on white

    // ─── Lifecycle ───────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!_initialized) { TryInitialize(); return; }

        if (_gc == null)
            _gc = FindAnyObjectByType<GameController>();

        if (Keyboard.current == null) return;

        if (Keyboard.current.f11Key.wasPressedThisFrame)
        {
            if (_isVisible) HidePanel(); else ShowPanel();
        }

        if (_unlimitedFunds && _gc != null)
        {
            if (_gc.FinanceSystem != null && _gc.FinanceSystem.Money < 999999)
            {
                _gc.QueueCommand(new AddMoneyCommand
                {
                    Tick = _gc.CurrentTick + 1,
                    Amount = 999999 - _gc.FinanceSystem.Money
                });
            }
        }
    }

    private bool TryInitialize()
    {
        if (_uiDocument == null) _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null || _uiDocument.rootVisualElement == null) return false;
        BuildUI();
        HidePanel();
        _initialized = true;
        return true;
    }

    // ─── Show / Hide ─────────────────────────────────────────────────────────────

    private void ShowPanel()
    {
        _isVisible = true;
        _panelContainer.style.display = DisplayStyle.Flex;
        RefreshActiveTab();
    }

    private void HidePanel()
    {
        _isVisible = false;
        _panelContainer.style.display = DisplayStyle.None;
    }

    // ─── UI Construction ─────────────────────────────────────────────────────────

    private void BuildUI()
    {
        var root = _uiDocument.rootVisualElement;
        root.Clear();
        root.pickingMode = PickingMode.Ignore;

        _panelContainer = new VisualElement();
        _panelContainer.pickingMode = PickingMode.Position;
        _panelContainer.style.position = Position.Absolute;
        _panelContainer.style.right = 0;
        _panelContainer.style.top = 0;
        _panelContainer.style.bottom = 0;
        _panelContainer.style.width = 440;
        _panelContainer.style.minWidth = 440;
        _panelContainer.style.maxWidth = 440;
        _panelContainer.style.overflow = Overflow.Hidden;
        _panelContainer.style.backgroundColor = PanelBg;
        _panelContainer.style.borderLeftWidth = 2;
        _panelContainer.style.borderLeftColor = new Color(0.3f, 0.5f, 0.8f, 0.7f);
        _panelContainer.style.flexDirection = FlexDirection.Column;
        root.Add(_panelContainer);

        BuildTitleBar();
        BuildTabBar();
        BuildStatePanel();
        BuildTuningPanel();
        BuildCheatsPanel();
        SwitchTab(0);
    }

    private void BuildTitleBar()
    {
        var bar = new VisualElement();
        bar.style.flexDirection = FlexDirection.Row;
        bar.style.justifyContent = Justify.SpaceBetween;
        bar.style.alignItems = Align.Center;
        bar.style.backgroundColor = TitleBg;
        bar.style.paddingLeft = 10; bar.style.paddingRight = 10;
        bar.style.paddingTop = 7; bar.style.paddingBottom = 7;
        bar.style.borderBottomWidth = 1;
        bar.style.borderBottomColor = new Color(0.25f, 0.4f, 0.7f, 0.5f);

        var title = new Label("Inspector");
        title.style.color = AccentBlue;
        title.style.fontSize = 13;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        bar.Add(title);

        var hint = new Label("F11");
        hint.style.color = MutedGray;
        hint.style.fontSize = 11;
        bar.Add(hint);

        _panelContainer.Add(bar);
    }

    private void BuildTabBar()
    {
        var bar = new VisualElement();
        bar.style.flexDirection = FlexDirection.Row;
        bar.style.backgroundColor = new Color(0.06f, 0.06f, 0.1f, 1f);
        bar.style.borderBottomWidth = 1;
        bar.style.borderBottomColor = new Color(0.2f, 0.3f, 0.5f, 0.4f);

        _tabState  = MakeTab("State",  0);
        _tabTuning = MakeTab("Tuning", 1);
        _tabCheats = MakeTab("Cheats", 2);
        bar.Add(_tabState);
        bar.Add(_tabTuning);
        bar.Add(_tabCheats);

        var spacer = new VisualElement();
        spacer.style.flexGrow = 1;
        bar.Add(spacer);

        var refresh = new Button(() => RefreshActiveTab()) { text = "Refresh" };
        refresh.style.fontSize = 11;
        refresh.style.color = AccentBlue;
        refresh.style.backgroundColor = Color.clear;
        refresh.style.borderTopWidth = 0; refresh.style.borderBottomWidth = 0;
        refresh.style.borderLeftWidth = 0; refresh.style.borderRightWidth = 0;
        refresh.style.marginTop = 4; refresh.style.marginBottom = 4; refresh.style.marginRight = 8;
        refresh.style.paddingLeft = 6; refresh.style.paddingRight = 6;
        bar.Add(refresh);

        _panelContainer.Add(bar);
    }

    private Button MakeTab(string label, int index)
    {
        var btn = new Button(() => SwitchTab(index)) { text = label };
        btn.style.fontSize = 12;
        btn.style.paddingLeft = 14; btn.style.paddingRight = 14;
        btn.style.paddingTop = 7; btn.style.paddingBottom = 7;
        btn.style.borderTopWidth = 0; btn.style.borderBottomWidth = 0;
        btn.style.borderLeftWidth = 0; btn.style.borderRightWidth = 1;
        btn.style.borderRightColor = new Color(0.2f, 0.3f, 0.5f, 0.3f);
        btn.style.marginTop = 0; btn.style.marginBottom = 0;
        btn.style.marginLeft = 0; btn.style.marginRight = 0;
        return btn;
    }

    private void SwitchTab(int index)
    {
        _activeTab = index;
        ApplyTabStyle(_tabState,  index == 0);
        ApplyTabStyle(_tabTuning, index == 1);
        ApplyTabStyle(_tabCheats, index == 2);
        _statePanel.style.display  = index == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        _tuningPanel.style.display = index == 1 ? DisplayStyle.Flex : DisplayStyle.None;
        _cheatsPanel.style.display = index == 2 ? DisplayStyle.Flex : DisplayStyle.None;
        if (_isVisible) RefreshActiveTab();
    }

    private void ApplyTabStyle(Button btn, bool active)
    {
        btn.style.backgroundColor = active ? TabActiveBg : TabInactiveBg;
        btn.style.color = active ? AccentBlue : MutedGray;
        btn.style.unityFontStyleAndWeight = active ? FontStyle.Bold : FontStyle.Normal;
        btn.style.borderBottomWidth = active ? 2 : 0;
        btn.style.borderBottomColor = AccentBlue;
    }

    // ─── State Panel ─────────────────────────────────────────────────────────────

    private void BuildStatePanel()
    {
        _statePanel = new VisualElement();
        _statePanel.style.flexGrow = 1;

        _stateScroll = new ScrollView(ScrollViewMode.Vertical);
        _stateScroll.style.flexGrow = 1;
        _stateScroll.style.paddingTop = 6;
        _stateScroll.style.paddingLeft = 6;
        _stateScroll.style.paddingRight = 6;
        _stateScroll.style.paddingBottom = 6;

        _statePanel.Add(_stateScroll);
        _panelContainer.Add(_statePanel);
    }

    private void RefreshStatePanel()
    {
        _stateScroll.Clear();

        if (_gc == null) { _stateScroll.Add(MakeInfoLabel("GameController not found")); return; }
        var state = _gc.GetGameState();
        if (state == null) { _stateScroll.Add(MakeInfoLabel("GameState is null")); return; }

        // Finance
        AddSectionHeader(_stateScroll, "Finance");
        if (state.financeState != null)
        {
            var fs = state.financeState;
            AddKV(_stateScroll, "Balance",        $"${fs.money:N0}");
            AddKV(_stateScroll, "Health",         fs.financialHealth.ToString());
            AddKV(_stateScroll, "Neg. Days",      fs.consecutiveDaysNegativeCash.ToString());
            AddKV(_stateScroll, "Missed Oblig.",  fs.missedObligationCount.ToString());
            int dailyExp = 0, dailyRev = 0;
            if (fs.recurringCosts != null)
                for (int i = 0; i < fs.recurringCosts.Count; i++)
                {
                    var e = fs.recurringCosts[i];
                    if (!e.isActive || e.interval != RecurringInterval.Monthly) continue;
                    if (e.amount > 0) dailyRev += e.amount; else dailyExp += -e.amount;
                }
            AddKV(_stateScroll, "Monthly Expenses", $"${dailyExp:N0}");
            AddKV(_stateScroll, "Monthly Revenue",  $"${dailyRev:N0}");
            AddKV(_stateScroll, "Net / Month",      $"${(dailyRev - dailyExp):N0}");

            var lr = _gc.LoanReadModel;
            if (lr != null && lr.HasActiveLoan)
            {
                var loan = lr.GetActiveLoan();
                if (loan.HasValue)
                {
                    AddKV(_stateScroll, "Loan Remaining",   $"${loan.Value.remainingOwed:N0}");
                    AddKV(_stateScroll, "Loan Monthly Pmnt", $"${loan.Value.monthlyPayment:N0}");
                    AddKV(_stateScroll, "Loan Months Left",  loan.Value.remainingMonths.ToString());
                }
            }
        }

        // Reputation
        AddSectionHeader(_stateScroll, "Reputation");
        if (state.reputationState?.reputationScores != null)
            foreach (var kvp in state.reputationState.reputationScores)
            {
                var tier = ReputationSystem.CalculateTier(kvp.Value, _gc.Tuning);
                AddKV(_stateScroll, kvp.Key.ToString(), $"{kvp.Value} pts  Tier {tier}");
            }
        if (state.recruitmentReputationState != null)
            AddKV(_stateScroll, "Recruit Score", $"{state.recruitmentReputationState.score}/100");

        // Time
        AddSectionHeader(_stateScroll, "Time");
        if (state.timeState != null)
        {
            AddKV(_stateScroll, "Tick",  state.currentTick.ToString());
            AddKV(_stateScroll, "Day",   state.timeState.currentDay.ToString());
            AddKV(_stateScroll, "Month", state.timeState.currentMonth.ToString());
            AddKV(_stateScroll, "Year",  state.timeState.currentYear.ToString());
        }

        // Employees
        AddSectionHeader(_stateScroll, "Employees");
        if (state.employeeState?.employees != null)
        {
            int empCount = 0;
            foreach (var emp in state.employeeState.employees.Values)
            {
                if (!emp.isActive) continue;
                int ca = _gc.AbilitySystem != null ? _gc.AbilitySystem.GetCA(emp.id, emp.role) : 0;
                float morale = 0f;
                if (state.moraleState?.employeeMorale != null &&
                    state.moraleState.employeeMorale.TryGetValue(emp.id, out var ms))
                    morale = ms.currentMorale;
                AddStateRow(_stateScroll,
                    $"[{emp.id.Value}] {emp.name}  {emp.role}  Ability:{ca}  Potential:{emp.Stats.PotentialAbility}  Morale:{morale:F0}  ${emp.salary:N0}");
                empCount++;
            }
            if (empCount == 0) AddStateRow(_stateScroll, "  (none)", MutedGray);
        }

        // Teams
        AddSectionHeader(_stateScroll, "Teams");
        if (state.teamState?.teams != null)
            foreach (var team in state.teamState.teams.Values)
            {
                string contractInfo = "No contract";
                if (state.contractState?.teamAssignments != null &&
                    state.contractState.teamAssignments.TryGetValue(team.id, out var cid))
                {
                    Contract c = null;
                    if (state.contractState.activeContracts?.TryGetValue(cid, out c) != true)
                        state.contractState.availableContracts?.TryGetValue(cid, out c);
                    if (c != null) contractInfo = $"{c.Name} ({c.Status})";
                }
                AddStateRow(_stateScroll,
                    $"[{team.id.Value}] {team.name}  Members:{team.members?.Count ?? 0}");
                AddStateRow(_stateScroll, $"    {contractInfo}", MutedGray);
            }

        // Contracts
        AddSectionHeader(_stateScroll, "Contracts");
        if (state.contractState != null)
        {
            var cs = state.contractState;
            AddKV(_stateScroll, "Available", (cs.availableContracts?.Count ?? 0).ToString());
            bool anyActive = false;
            if (cs.activeContracts != null)
                foreach (var c in cs.activeContracts.Values)
                {
                    float pct = c.TotalWorkRequired > 0 ? c.WorkCompleted / c.TotalWorkRequired * 100f : 0f;
                    AddStateRow(_stateScroll, $"  [{c.Id.Value}] {c.Name}  {c.Status}  {pct:F0}%");
                    anyActive = true;
                }
            if (!anyActive) AddStateRow(_stateScroll, "  (no active contracts)", MutedGray);
        }

        // Candidates
        AddSectionHeader(_stateScroll, "Candidates");
        if (state.employeeState?.availableCandidates != null)
        {
            var pool = state.employeeState.availableCandidates;
            AddKV(_stateScroll, "Pool size", pool.Count.ToString());
            for (int i = 0; i < pool.Count && i < 6; i++)
            {
                var c = pool[i];
                float pct = CandidateExpiryHelper.GetTimeRemainingPercent(
                    state.employeeState, c.CandidateId, state.currentTick, _gc.Tuning);
                string exp = pct >= 0f ? $"{pct * 100f:F0}%" : "N/A";
                AddStateRow(_stateScroll,
                    $"  [{c.CandidateId}] {c.Name}  {c.Role}  {(c.IsTargeted ? "HR" : "Auto")}  Stg:{c.InterviewStage}  Exp:{exp}",
                    MutedGray);
            }
            if (pool.Count > 6)
                AddStateRow(_stateScroll, $"  ... and {pool.Count - 6} more", MutedGray);
        }
    }

    private void AddSectionHeader(VisualElement parent, string text)
    {
        var sp = new VisualElement(); sp.style.height = 6; parent.Add(sp);

        var lbl = new Label(text.ToUpperInvariant());
        lbl.style.color = SectionColor;
        lbl.style.fontSize = 10;
        lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        lbl.style.paddingLeft = 4; lbl.style.paddingTop = 4; lbl.style.paddingBottom = 2;
        lbl.style.borderBottomWidth = 1;
        lbl.style.borderBottomColor = new Color(0.3f, 0.5f, 0.8f, 0.25f);
        parent.Add(lbl);
    }

    private void AddKV(VisualElement parent, string key, string value)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.paddingLeft = 6; row.style.paddingRight = 6;
        row.style.paddingTop = 2; row.style.paddingBottom = 2;

        var k = new Label(key);
        k.style.color = new Color(0.7f, 0.7f, 0.7f); k.style.fontSize = 11;

        var v = new Label(value);
        v.style.color = Color.white; v.style.fontSize = 11;
        v.style.unityFontStyleAndWeight = FontStyle.Bold;

        row.Add(k); row.Add(v);
        parent.Add(row);
    }

    private void AddStateRow(VisualElement parent, string text, Color? color = null)
    {
        var lbl = new Label(text);
        lbl.style.color = color ?? new Color(0.85f, 0.85f, 0.85f);
        lbl.style.fontSize = 11; lbl.style.whiteSpace = WhiteSpace.Normal;
        lbl.style.paddingLeft = 6; lbl.style.paddingRight = 6;
        lbl.style.paddingTop = 1; lbl.style.paddingBottom = 1;
        parent.Add(lbl);
    }

    // ─── Tuning Panel ─────────────────────────────────────────────────────────────

    private void BuildTuningPanel()
    {
        _tuningPanel = new VisualElement();
        _tuningPanel.style.flexGrow = 1;
        _tuningPanel.style.flexDirection = FlexDirection.Column;
        _tuningPanel.style.overflow = Overflow.Hidden;

        // Search bar
        var searchRow = new VisualElement();
        searchRow.style.flexDirection = FlexDirection.Row;
        searchRow.style.alignItems = Align.Center;
        searchRow.style.paddingLeft = 8; searchRow.style.paddingRight = 8;
        searchRow.style.paddingTop = 6; searchRow.style.paddingBottom = 6;
        searchRow.style.borderBottomWidth = 1;
        searchRow.style.borderBottomColor = new Color(0.2f, 0.3f, 0.5f, 0.3f);

        var searchHint = new Label("Filter:");
        searchHint.style.color = MutedGray;
        searchHint.style.fontSize = 11;
        searchHint.style.marginRight = 6;
        searchRow.Add(searchHint);

        _tuningSearchField = new TextField();
        _tuningSearchField.style.flexGrow = 1;
        _tuningSearchField.style.fontSize = 11;
        _tuningSearchField.style.backgroundColor = FieldBg;
        _tuningSearchField.value = "";
        _tuningSearchField.RegisterValueChangedCallback(evt =>
        {
            _tuningFilter = evt.newValue;
            RebuildTuningList();
        });
        // Set inner text element colour after field is constructed
        var searchTextEl = _tuningSearchField.Q<TextElement>();
        if (searchTextEl != null) searchTextEl.style.color = FieldText;
        searchRow.Add(_tuningSearchField);
        _tuningPanel.Add(searchRow);

        // Column headers
        var headers = new VisualElement();
        headers.style.flexDirection = FlexDirection.Row;
        headers.style.paddingLeft = 8; headers.style.paddingRight = 8;
        headers.style.paddingTop = 4; headers.style.paddingBottom = 4;
        headers.style.backgroundColor = new Color(0.07f, 0.07f, 0.11f, 1f);
        headers.style.borderBottomWidth = 1;
        headers.style.borderBottomColor = new Color(0.2f, 0.3f, 0.5f, 0.25f);

        var hParam = new Label("PARAMETER");
        hParam.style.color = MutedGray; hParam.style.fontSize = 10; hParam.style.flexGrow = 1;

        var hVal = new Label("VALUE");
        hVal.style.color = MutedGray; hVal.style.fontSize = 10;
        hVal.style.width = 120; hVal.style.unityTextAlign = TextAnchor.MiddleRight;

        headers.Add(hParam); headers.Add(hVal);
        _tuningPanel.Add(headers);

        _tuningScroll = new ScrollView(ScrollViewMode.Vertical);
        _tuningScroll.style.flexGrow = 1;
        _tuningScroll.style.overflow = Overflow.Hidden;
        _tuningPanel.Add(_tuningScroll);

        _panelContainer.Add(_tuningPanel);
    }

    private void RebuildTuningList()
    {
        _tuningScroll.Clear();

        if (_gc == null) { _tuningScroll.Add(MakeInfoLabel("GameController not found")); return; }
        var tuning = _gc.Tuning;
        if (tuning == null) { _tuningScroll.Add(MakeInfoLabel("TuningConfig not available")); return; }

        if (_tuningDefaults == null)
            _tuningDefaults = TuningConfig.Defaults().GetAllParameters();

        var allParams = tuning.GetAllParameters();
        string filter = _tuningFilter?.ToLowerInvariant() ?? "";

        int rowIndex = 0;
        foreach (var kvp in allParams)
        {
            if (!string.IsNullOrEmpty(filter) && !kvp.Key.ToLowerInvariant().Contains(filter))
                continue;
            bool modified = IsModifiedFromDefault(kvp.Key, kvp.Value);
            AddTuningRow(kvp.Key, FormatValue(kvp.Value), modified, rowIndex % 2 == 1, tuning);
            rowIndex++;
        }

        if (rowIndex == 0)
            _tuningScroll.Add(MakeInfoLabel("No parameters match filter"));
    }

    private void AddTuningRow(string paramName, string currentValue, bool isModified, bool altRow, TuningConfig tuning)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.paddingLeft = isModified ? 6 : 8;
        row.style.paddingRight = 8;
        row.style.paddingTop = 3; row.style.paddingBottom = 3;
        row.style.minHeight = 26;
        if (altRow) row.style.backgroundColor = RowAlt;
        if (isModified) { row.style.borderLeftWidth = 2; row.style.borderLeftColor = ModifiedYellow; }

        var nameLabel = new Label(paramName);
        nameLabel.style.color = isModified ? ModifiedYellow : new Color(0.78f, 0.78f, 0.78f);
        nameLabel.style.fontSize = 11;
        nameLabel.style.flexGrow = 1;
        nameLabel.style.overflow = Overflow.Hidden;
        row.Add(nameLabel);

        var field = new TextField();
        field.value = currentValue;
        field.style.width = 118;
        field.style.fontSize = 11;
        field.style.backgroundColor = FieldBg;
        field.style.borderTopWidth = 0; field.style.borderLeftWidth = 0; field.style.borderRightWidth = 0;
        field.style.borderBottomWidth = 1;
        field.style.borderBottomColor = new Color(0.25f, 0.4f, 0.65f, 0.5f);
        field.style.unityTextAlign = TextAnchor.MiddleRight;
        field.style.marginLeft = 6;

        // Colour the inner TextElement directly — TextField.style.color does not reach it
        var textEl = field.Q<TextElement>();
        if (textEl != null) textEl.style.color = isModified ? FieldModified : FieldText;

        field.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            { CommitTuning(paramName, field.value, tuning); field.Blur(); }
        });
        field.RegisterCallback<FocusOutEvent>(evt =>
        {
            CommitTuning(paramName, field.value, tuning);
        });

        row.Add(field);
        _tuningScroll.Add(row);
    }

    private void CommitTuning(string paramName, string rawValue, TuningConfig tuning)
    {
        try
        {
            tuning.SetParameter(paramName, rawValue);
            RebuildTuningList();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[TuningPanel] {paramName} = {rawValue} failed: {ex.Message}");
        }
    }

    private bool IsModifiedFromDefault(string key, object currentValue)
    {
        if (_tuningDefaults == null || !_tuningDefaults.TryGetValue(key, out var defVal)) return false;
        return FormatValue(currentValue) != FormatValue(defVal);
    }

    // ─── Cheats Panel ─────────────────────────────────────────────────────────────

    private void BuildCheatsPanel()
    {
        _cheatsPanel = new VisualElement();
        _cheatsPanel.style.flexGrow = 1;
        _cheatsPanel.style.flexDirection = FlexDirection.Column;
        _cheatsPanel.style.overflow = Overflow.Hidden;

        _cheatsScroll = new ScrollView(ScrollViewMode.Vertical);
        _cheatsScroll.style.flexGrow = 1;
        _cheatsScroll.style.overflow = Overflow.Hidden;
        _cheatsScroll.style.paddingTop = 4;
        _cheatsScroll.style.paddingLeft = 6;
        _cheatsScroll.style.paddingRight = 6;
        _cheatsScroll.style.paddingBottom = 6;
        _cheatsPanel.Add(_cheatsScroll);

        _panelContainer.Add(_cheatsPanel);
    }

    private void RefreshCheatsPanel()
    {
        _cheatsScroll.Clear();

        if (_gc == null) { _cheatsScroll.Add(MakeInfoLabel("GameController not found")); return; }
        var state = _gc.GetGameState();
        if (state == null) { _cheatsScroll.Add(MakeInfoLabel("GameState is null")); return; }

        BuildCheatSection_Finance(state);
        BuildCheatSection_Time(state);
        BuildCheatSection_EmployeeActions(state);
        BuildCheatSection_ContractActions(state);
        BuildCheatSection_Reputation();
        BuildCheatSection_Products(state);
        BuildCheatSection_EmployeeEditor(state);
    }

    private void BuildCheatSection_Finance(GameState state)
    {
        AddSectionHeader(_cheatsScroll, "Finance");
        AddKV(_cheatsScroll, "Balance", $"${_gc.FinanceSystem.Money:N0}");

        // Preset buttons
        var presetRow = MakeActionRow();
        presetRow.Add(MakeActionButton("+$1K",   () => { _gc.QueueCommand(new AddMoneyCommand { Tick = _gc.CurrentTick + 1, Amount = 1000 }); RefreshCheatsPanel(); }));
        presetRow.Add(MakeActionButton("+$10K",  () => { _gc.QueueCommand(new AddMoneyCommand { Tick = _gc.CurrentTick + 1, Amount = 10000 }); RefreshCheatsPanel(); }));
        presetRow.Add(MakeActionButton("+$100K", () => { _gc.QueueCommand(new AddMoneyCommand { Tick = _gc.CurrentTick + 1, Amount = 100000 }); RefreshCheatsPanel(); }));
        presetRow.Add(MakeActionButton("+$1M",   () => { _gc.QueueCommand(new AddMoneyCommand { Tick = _gc.CurrentTick + 1, Amount = 1000000 }); RefreshCheatsPanel(); }));
        _cheatsScroll.Add(presetRow);

        // Custom add
        var addRow = MakeActionRow();
        var addField = MakeTextField("10000", 80);
        addRow.Add(addField);
        addRow.Add(MakeActionButton("Add Money", () =>
        {
            if (int.TryParse(addField.value, out int amt))
                _gc.QueueCommand(new AddMoneyCommand { Tick = _gc.CurrentTick + 1, Amount = amt });
            RefreshCheatsPanel();
        }));
        _cheatsScroll.Add(addRow);

        // Set money
        var setRow = MakeActionRow();
        var setField = MakeTextField(_gc.FinanceSystem.Money.ToString(), 80);
        setRow.Add(setField);
        setRow.Add(MakeActionButton("Set Money", () =>
        {
            if (int.TryParse(setField.value, out int target))
            {
                int delta = target - _gc.FinanceSystem.Money;
                _gc.QueueCommand(new AddMoneyCommand { Tick = _gc.CurrentTick + 1, Amount = delta });
            }
            RefreshCheatsPanel();
        }));
        _cheatsScroll.Add(setRow);

        // Unlimited funds toggle
        var unlimitedBtn = MakeActionButton(
            _unlimitedFunds ? "Unlimited: ON" : "Unlimited: OFF",
            () =>
            {
                _unlimitedFunds = !_unlimitedFunds;
                RefreshCheatsPanel();
            });
        if (_unlimitedFunds)
        {
            unlimitedBtn.style.color = new Color(1f, 0.85f, 0f);
            unlimitedBtn.style.borderBottomColor = new Color(1f, 0.85f, 0f);
            unlimitedBtn.style.borderTopColor    = new Color(1f, 0.85f, 0f);
            unlimitedBtn.style.borderLeftColor   = new Color(1f, 0.85f, 0f);
            unlimitedBtn.style.borderRightColor  = new Color(1f, 0.85f, 0f);
        }
        var unlimitedRow = MakeActionRow();
        unlimitedRow.Add(unlimitedBtn);
        _cheatsScroll.Add(unlimitedRow);

        // Loan repay (conditional)
        var lr = _gc.LoanReadModel;
        if (lr != null && lr.HasActiveLoan)
        {
            int debt = lr.GetTotalRemainingDebt();
            var loanRow = MakeActionRow();
            loanRow.Add(MakeActionButton($"Repay Loan (${debt:N0})", () =>
            {
                _gc.QueueCommand(new RepayLoanEarlyCommand(_gc.CurrentTick, debt));
                RefreshCheatsPanel();
            }));
            _cheatsScroll.Add(loanRow);
        }
    }

    private void BuildCheatSection_Time(GameState state)
    {
        AddSectionHeader(_cheatsScroll, "Time");
        var ts = _gc.TimeSystem;
        AddKV(_cheatsScroll, "Date", $"Day {ts.DayOfMonth} / Month {ts.CurrentMonth} / Year {ts.CurrentYear}");
        AddKV(_cheatsScroll, "Tick", _gc.CurrentTick.ToString());
        AddKV(_cheatsScroll, "Status", _gc.IsAdvancing ? "ADVANCING" : "PAUSED");

        // Pause/Resume
        var pauseRow = MakeActionRow();
        pauseRow.Add(MakeActionButton(_gc.IsAdvancing ? "Pause" : "Resume", () =>
        {
            _gc.TogglePause();
            RefreshCheatsPanel();
        }));
        _cheatsScroll.Add(pauseRow);

        // Skip day presets (only when paused)
        var skipRow = MakeActionRow();
        bool canSkip = !_gc.IsAdvancing;
        foreach (var days in new[] { 1, 7, 30, 90 })
        {
            int d = days;
            var btn = MakeActionButton($"+{d}d", () =>
            {
                if (!_gc.IsAdvancing)
                    _gc.SkipTicks(d * TimeState.TicksPerDay);
                RefreshCheatsPanel();
            });
            btn.SetEnabled(canSkip);
            skipRow.Add(btn);
        }
        _cheatsScroll.Add(skipRow);

        // Skip to day
        var skipToRow = MakeActionRow();
        var skipDayField = MakeTextField("1", 60);
        skipToRow.Add(skipDayField);
        skipToRow.Add(MakeActionButton("Skip Days", () =>
        {
            if (!_gc.IsAdvancing && int.TryParse(skipDayField.value, out int d) && d > 0)
                _gc.SkipTicks(d * TimeState.TicksPerDay);
            RefreshCheatsPanel();
        }));
        _cheatsScroll.Add(skipToRow);


    }

    private void BuildCheatSection_EmployeeActions(GameState state)
    {
        AddSectionHeader(_cheatsScroll, "Employee Actions");
        int empCount = _gc.EmployeeSystem.EmployeeCount;
        int candCount = state.employeeState?.availableCandidates?.Count ?? 0;
        AddKV(_cheatsScroll, "Employees", empCount.ToString());
        AddKV(_cheatsScroll, "Candidates", candCount.ToString());

        // Quick hire
        var hireRow = MakeActionRow();
        var hireCountField = MakeTextField("1", 50);
        hireRow.Add(hireCountField);
        hireRow.Add(MakeActionButton("Quick Hire", () =>
        {
            var s2 = _gc.GetGameState();
            if (s2?.employeeState?.availableCandidates == null) { RefreshCheatsPanel(); return; }
            var pool = s2.employeeState.availableCandidates;
            if (!int.TryParse(hireCountField.value, out int count)) count = 1;
            for (int i = 0; i < count && i < pool.Count; i++)
            {
                var cand = pool[i];
                _gc.QueueCommand(new HireEmployeeCommand
                {
                    Tick = _gc.CurrentTick,
                    CandidateId = cand.CandidateId,
                    Name = cand.Name,
                    Gender = cand.Gender,
                    Age = cand.Age,
                    Stats = cand.Stats,
                    Salary = cand.Salary,
                    Role = cand.Role,
                    Mode = HiringMode.Manual
                });
            }
            RefreshCheatsPanel();
        }));
        _cheatsScroll.Add(hireRow);

        // Spawn random
        var spawnRow = MakeActionRow();
        spawnRow.Add(MakeActionButton("Spawn Random", () =>
        {
            EnsureSpawnRng();
            Gender gender = (Gender)_spawnRng.Range(0, 2);
            string name = NameGenerator.GenerateRandomName(_spawnRng, gender);
            RoleId role = (RoleId)_spawnRng.Range(0, SkillIdHelper.SkillCount);
            var stats = EmployeeStatBlock.Create();
            for (int i = 0; i < SkillIdHelper.SkillCount; i++) stats.SetSkill((SkillId)i, _spawnRng.Range(5, 26));
            stats.PotentialAbility = _spawnRng.Range(80, 181);
            _gc.QueueCommand(new HireEmployeeCommand
            {
                Tick = _gc.CurrentTick,
                CandidateId = 0,
                Name = name,
                Gender = gender,
                Age = _spawnRng.Range(22, 46),
                Stats = stats,
                Salary = _spawnRng.Range(3000, 9000),
                Role = role,
                Mode = HiringMode.Manual
            });
            RefreshCheatsPanel();
        }));
        _cheatsScroll.Add(spawnRow);

        // Spawn by role
        var allRoles = (RoleId[])System.Enum.GetValues(typeof(RoleId));
        var roleNames = new List<string>(allRoles.Length);
        for (int r = 0; r < allRoles.Length; r++) roleNames.Add(RoleIdHelper.GetName(allRoles[r]));
        var roleValues = new List<RoleId>(allRoles);
        var roleDropdown = new PopupField<string>(roleNames, 0);
        roleDropdown.style.width = 100;
        roleDropdown.style.fontSize = 11;
        roleDropdown.style.backgroundColor = FieldBg;
        roleDropdown.style.marginRight = 4;
        var roleTextEl = roleDropdown.Q<TextElement>();
        if (roleTextEl != null) roleTextEl.style.color = FieldText;

        var spawnCountField = MakeTextField("1", 40);

        var roleSpawnRow = MakeActionRow();
        roleSpawnRow.Add(roleDropdown);
        roleSpawnRow.Add(spawnCountField);
        roleSpawnRow.Add(MakeActionButton("Spawn", () =>
        {
            EnsureSpawnRng();
            int roleIdx = roleNames.IndexOf(roleDropdown.value);
            if (roleIdx < 0) roleIdx = 0;
            RoleId chosenRole = roleValues[roleIdx];
            if (!int.TryParse(spawnCountField.value, out int spawnCount)) spawnCount = 1;
            if (spawnCount < 1) spawnCount = 1;
            if (spawnCount > 20) spawnCount = 20;

            for (int i = 0; i < spawnCount; i++)
            {
                var candidate = CandidateData.GenerateCandidate(_spawnRng, 1.5f, chosenRole);
                candidate.CandidateId = 0;
                _gc.QueueCommand(new HireEmployeeCommand
                {
                    Tick = _gc.CurrentTick,
                    CandidateId = candidate.CandidateId,
                    Name = candidate.Name,
                    Gender = candidate.Gender,
                    Age = candidate.Age,
                    Stats = candidate.Stats,
                    Salary = candidate.Salary,
                    Role = candidate.Role,
                    Mode = HiringMode.Manual
                });
            }
            RefreshCheatsPanel();
        }));
        _cheatsScroll.Add(roleSpawnRow);

        // Max all stats
        var maxRow = MakeActionRow();
        maxRow.Add(MakeActionButton("Max All Stats", () =>
        {
            var s2 = _gc.GetGameState();
            if (s2?.employeeState?.employees == null) { RefreshCheatsPanel(); return; }
            foreach (var emp in s2.employeeState.employees.Values)
            {
                if (!emp.isActive) continue;
                for (int i = 0; i < SkillIdHelper.SkillCount; i++)
                    emp.Stats.SetSkill((SkillId)i, 20);
                emp.morale = 100;
                emp.Stats.PotentialAbility = 200;
            }
            RefreshCheatsPanel();
        }));
        _cheatsScroll.Add(maxRow);
    }

    private void BuildCheatSection_ContractActions(GameState state)
    {
        AddSectionHeader(_cheatsScroll, "Contract Actions");
        AddKV(_cheatsScroll, "Available", _gc.ContractSystem.AvailableContractCount.ToString());
        AddKV(_cheatsScroll, "Active",    _gc.ContractSystem.ActiveContractCount.ToString());

        // Reroll pool
        var topRow = MakeActionRow();
        topRow.Add(MakeActionButton("Reroll Pool", () =>
        {
            _gc.QueueCommand(new RerollContractPoolCommand { Tick = _gc.CurrentTick });
            RefreshCheatsPanel();
        }));

        // Accept first
        topRow.Add(MakeActionButton("Accept First", () =>
        {
            foreach (var c in _gc.ContractSystem.GetAvailableContracts())
            {
                _gc.QueueCommand(new AcceptContractCommand { Tick = _gc.CurrentTick, ContractId = c.Id });
                break;
            }
            RefreshCheatsPanel();
        }));
        _cheatsScroll.Add(topRow);

        // Per-contract rows
        if (state.contractState?.activeContracts == null) return;
        foreach (var contract in state.contractState.activeContracts.Values)
        {
            float pct = contract.ProgressPercent * 100f;
            var contractHeader = new Label($"[{contract.Id.Value}] {contract.Name}  {pct:F0}%  Q:{contract.QualityScore:F1}");
            contractHeader.style.color = new Color(0.75f, 0.75f, 0.75f);
            contractHeader.style.fontSize = 10;
            contractHeader.style.paddingLeft = 4;
            contractHeader.style.paddingTop = 3;
            contractHeader.style.whiteSpace = WhiteSpace.Normal;
            _cheatsScroll.Add(contractHeader);

            var contractRow = MakeActionRow();
            ContractId cid = contract.Id;

            contractRow.Add(MakeActionButton("Complete", () =>
            {
                _gc.QueueCommand(new CompleteContractCommand { Tick = _gc.CurrentTick, ContractId = cid });
                RefreshCheatsPanel();
            }));

            var failBtn = MakeActionButton("Fail", () =>
            {
                var s2 = _gc.GetGameState();
                if (s2?.contractState?.activeContracts != null &&
                    s2.contractState.activeContracts.TryGetValue(cid, out var c2))
                    c2.Status = ContractStatus.Failed;
                RefreshCheatsPanel();
            });
            failBtn.style.color = new Color(1f, 0.35f, 0.35f);
            contractRow.Add(failBtn);

            var qField = MakeTextField(contract.QualityScore.ToString("F1"), 50);
            contractRow.Add(qField);
            contractRow.Add(MakeActionButton("Set Q", () =>
            {
                var s2 = _gc.GetGameState();
                if (s2?.contractState?.activeContracts != null &&
                    s2.contractState.activeContracts.TryGetValue(cid, out var c2) &&
                    float.TryParse(qField.value, out float q))
                    c2.QualityScore = q;
                RefreshCheatsPanel();
            }));
            _cheatsScroll.Add(contractRow);
        }
    }

    private void BuildCheatSection_Reputation()
    {
        AddSectionHeader(_cheatsScroll, "Reputation");

        var rep = _gc.ReputationSystem;
        if (rep == null) { _cheatsScroll.Add(MakeInfoLabel("ReputationSystem not available")); return; }

        AddKV(_cheatsScroll, "Reputation", $"{rep.GlobalReputation}  Tier {rep.CurrentTier}");

        var deltaRow = MakeActionRow();
        foreach (var delta in new[] { -50, -10, 10, 50, 100 })
        {
            int d = delta;
            string label = d > 0 ? $"+{d}" : d.ToString();
            deltaRow.Add(MakeActionButton(label, () =>
            {
                if (d > 0) rep.AddReputation(d);
                else rep.RemoveReputation(-d);
                RefreshCheatsPanel();
            }));
        }
        _cheatsScroll.Add(deltaRow);
    }

    private void BuildCheatSection_Products(GameState state)
    {
        AddSectionHeader(_cheatsScroll, "Products");

        int devCount = state.productState?.developmentProducts?.Count ?? 0;
        int shippedCount = state.productState?.shippedProducts?.Count ?? 0;
        AddKV(_cheatsScroll, "In Development", devCount.ToString());
        AddKV(_cheatsScroll, "Shipped", shippedCount.ToString());

        var templates = _gc.ProductTemplates;
        if (templates == null || templates.Length == 0)
        {
            _cheatsScroll.Add(MakeInfoLabel("No ProductTemplates registered"));
            return;
        }

        var spawnRow = MakeActionRow();
        spawnRow.Add(MakeActionButton("Spawn Random Product", () =>
        {
            EnsureSpawnRng();
            var template = templates[_spawnRng.Range(0, templates.Length)];

            // Pick 2-4 random features via partial Fisher-Yates
            int featureCount = 0;
            string[] selectedFeatureIds = null;
            if (template.availableFeatures != null && template.availableFeatures.Length > 0)
            {
                int available = template.availableFeatures.Length;
                int pick = Math.Min(_spawnRng.Range(2, 5), available);
                var indices = new List<int>(available);
                for (int i = 0; i < available; i++) indices.Add(i);
                selectedFeatureIds = new string[pick];
                for (int i = 0; i < pick; i++)
                {
                    int idx = _spawnRng.Range(0, indices.Count);
                    selectedFeatureIds[i] = template.availableFeatures[indices[idx]].featureId;
                    indices.RemoveAt(idx);
                }
                featureCount = pick;
            }
            else
            {
                selectedFeatureIds = Array.Empty<string>();
            }

            string productName = $"Debug {template.displayName} #{_spawnRng.Range(100, 999)}";
            float price = template.economyConfig?.pricePerUnit > 0f ? template.economyConfig.pricePerUnit : 9.99f;
            bool isSub = template.economyConfig != null && template.economyConfig.isSubscriptionBased;

            float quality = _spawnRng.Range(50, 96);

            _gc.QueueCommand(new SpawnShippedProductCheatCommand
            {
                Tick = _gc.CurrentTick,
                TemplateId = template.templateId,
                ProductName = productName,
                SelectedFeatureIds = selectedFeatureIds,
                IsSubscriptionBased = isSub,
                Price = price,
                OverallQuality = quality
            });
            RefreshCheatsPanel();
        }));
        _cheatsScroll.Add(spawnRow);
    }

    private void BuildCheatSection_EmployeeEditor(GameState state)
    {
        AddSectionHeader(_cheatsScroll, "Employee Editor");

        if (state.employeeState?.employees == null) return;

        foreach (var emp in state.employeeState.employees.Values)
        {
            if (!emp.isActive) continue;

            int ca = _gc.AbilitySystem != null ? _gc.AbilitySystem.GetCA(emp.id, emp.role) : 0;
            bool isSelected = _selectedEmployeeId.HasValue && _selectedEmployeeId.Value.Value == emp.id.Value;
            EmployeeId capturedId = emp.id;

            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.alignItems = Align.Center;
            headerRow.style.paddingLeft = 4;
            headerRow.style.paddingTop = 3;
            headerRow.style.paddingBottom = 3;
            if (isSelected) headerRow.style.backgroundColor = new Color(0.1f, 0.18f, 0.3f, 0.8f);

            var empLabel = new Label($"[{emp.id.Value}] {emp.name}  {emp.role}  Ability:{ca}  Potential:{emp.Stats.PotentialAbility}  M:{emp.morale}");
            empLabel.style.color = isSelected ? AccentBlue : new Color(0.78f, 0.78f, 0.78f);
            empLabel.style.fontSize = 10;
            empLabel.style.flexGrow = 1;
            empLabel.style.overflow = Overflow.Hidden;
            empLabel.style.whiteSpace = WhiteSpace.Normal;
            headerRow.Add(empLabel);

            headerRow.RegisterCallback<PointerDownEvent>(evt =>
            {
                _selectedEmployeeId = isSelected ? (EmployeeId?)null : capturedId;
                RefreshCheatsPanel();
            });
            headerRow.pickingMode = PickingMode.Position;
            _cheatsScroll.Add(headerRow);

            if (isSelected)
            {
                Employee empRef = emp;

                AddEditableField(_cheatsScroll, "Salary",   emp.salary.ToString(),       v => { if (int.TryParse(v, out int n)) empRef.salary = n; });
                AddEditableField(_cheatsScroll, "Morale",   emp.morale.ToString(),        v => { if (int.TryParse(v, out int n)) empRef.morale = n; });
                AddEditableField(_cheatsScroll, "Potential", emp.Stats.PotentialAbility.ToString(), v => { if (int.TryParse(v, out int n)) empRef.Stats.PotentialAbility = n; });
                AddEditableField(_cheatsScroll, "Age",      emp.age.ToString(),           v => { if (int.TryParse(v, out int n)) empRef.age = n; });

                for (int i = 0; i < SkillIdHelper.SkillCount; i++)
                {
                    int skillIndex = i;
                    Employee empRef2 = emp;
                    string skillName = SkillIdHelper.GetName((SkillId)i);
                    AddEditableField(_cheatsScroll, skillName, emp.Stats.Skills[i].ToString(),
                        v => { if (int.TryParse(v, out int n)) empRef2.Stats.SetSkill((SkillId)skillIndex, n); });
                }

                // Quick actions
                var quickRow = MakeActionRow();
                quickRow.Add(MakeActionButton("Max Stats", () =>
                {
                    var s2 = _gc.GetGameState();
                    if (s2?.employeeState?.employees != null &&
                        s2.employeeState.employees.TryGetValue(capturedId, out var e2))
                    {
                        for (int i = 0; i < SkillIdHelper.SkillCount; i++) e2.Stats.SetSkill((SkillId)i, 20);
                        e2.morale = 100;
                        e2.Stats.PotentialAbility = 200;
                    }
                    RefreshCheatsPanel();
                }));
                quickRow.Add(MakeActionButton("Morale 100", () =>
                {
                    var s2 = _gc.GetGameState();
                    if (s2?.employeeState?.employees != null &&
                        s2.employeeState.employees.TryGetValue(capturedId, out var e2))
                        e2.morale = 100;
                    RefreshCheatsPanel();
                }));

                var fireBtn = MakeActionButton("Fire", () =>
                {
                    _gc.QueueCommand(new FireEmployeeCommand { Tick = _gc.CurrentTick, EmployeeId = capturedId });
                    _selectedEmployeeId = null;
                    RefreshCheatsPanel();
                });
                fireBtn.style.color = new Color(1f, 0.35f, 0.35f);
                quickRow.Add(fireBtn);
                _cheatsScroll.Add(quickRow);
            }
        }
    }

    private void EnsureSpawnRng()
    {
        if (_spawnRng == null && _gc != null)
        {
            int seed = _gc.GetDeterministicSeed("cheatpanel");
            _spawnRng = RngFactory.CreateStream(seed, "cheatspawn");
        }
    }

    // ─── Cheat Helpers ────────────────────────────────────────────────────────────

    private VisualElement MakeActionRow()
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.paddingLeft = 4; row.style.paddingRight = 4;
        row.style.paddingTop = 3; row.style.paddingBottom = 3;
        row.style.flexWrap = Wrap.Wrap;
        return row;
    }

    private TextField MakeTextField(string defaultValue, int width)
    {
        var tf = new TextField();
        tf.value = defaultValue;
        tf.style.width = width;
        tf.style.fontSize = 11;
        tf.style.backgroundColor = FieldBg;
        tf.style.borderTopWidth = 0; tf.style.borderLeftWidth = 0; tf.style.borderRightWidth = 0;
        tf.style.borderBottomWidth = 1;
        tf.style.borderBottomColor = new Color(0.25f, 0.4f, 0.65f, 0.5f);
        tf.style.marginRight = 4;
        var textEl = tf.Q<TextElement>();
        if (textEl != null) textEl.style.color = FieldText;
        return tf;
    }

    private Button MakeActionButton(string label, Action onClick)
    {
        var btn = new Button(onClick) { text = label };
        btn.style.fontSize = 11;
        btn.style.color = AccentBlue;
        btn.style.backgroundColor = new Color(0.06f, 0.10f, 0.20f, 1f);
        btn.style.borderTopWidth = 1; btn.style.borderBottomWidth = 1;
        btn.style.borderLeftWidth = 1; btn.style.borderRightWidth = 1;
        btn.style.borderTopColor    = new Color(0.2f, 0.35f, 0.6f, 0.6f);
        btn.style.borderBottomColor = new Color(0.2f, 0.35f, 0.6f, 0.6f);
        btn.style.borderLeftColor   = new Color(0.2f, 0.35f, 0.6f, 0.6f);
        btn.style.borderRightColor  = new Color(0.2f, 0.35f, 0.6f, 0.6f);
        btn.style.paddingLeft = 6; btn.style.paddingRight = 6;
        btn.style.paddingTop = 3; btn.style.paddingBottom = 3;
        btn.style.marginRight = 4; btn.style.marginBottom = 2;
        return btn;
    }

    private void AddEditableField(VisualElement parent, string label, string value, Action<string> onCommit)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.paddingLeft = 4; row.style.paddingRight = 4;
        row.style.paddingTop = 2; row.style.paddingBottom = 2;

        var lbl = new Label(label);
        lbl.style.color = new Color(0.65f, 0.65f, 0.65f);
        lbl.style.fontSize = 10;
        lbl.style.width = 100;
        row.Add(lbl);

        var tf = MakeTextField(value, 70);
        tf.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            { onCommit(tf.value); tf.Blur(); }
        });
        tf.RegisterCallback<FocusOutEvent>(evt => onCommit(tf.value));
        row.Add(tf);

        parent.Add(row);
    }

    // ─── Refresh ─────────────────────────────────────────────────────────────────

    private void RefreshActiveTab()
    {
        if (_activeTab == 0) RefreshStatePanel();
        else if (_activeTab == 1) RebuildTuningList();
        else RefreshCheatsPanel();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────────

    private static string FormatValue(object val)
    {
        if (val is float f)  return f.ToString("G5");
        if (val is double d) return d.ToString("G5");
        return val?.ToString() ?? "null";
    }

    private Label MakeInfoLabel(string text)
    {
        var lbl = new Label(text);
        lbl.style.color = MutedGray; lbl.style.fontSize = 11;
        lbl.style.paddingLeft = 10; lbl.style.paddingTop = 8;
        return lbl;
    }
}
