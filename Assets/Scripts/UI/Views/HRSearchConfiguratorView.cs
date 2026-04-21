using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class HRSearchConfiguratorView : IGameView
{
    private VisualElement _root;
    private HRSearchConfiguratorViewModel _viewModel;

    private Label _teamNameLabel;
    private DropdownField _roleDropdown;
    private DropdownField _minCADropdown;
    private DropdownField _maxCADropdown;
    private DropdownField _minPADropdown;
    private DropdownField _maxPADropdown;
    private VisualElement _skillTogglesContainer;
    private DropdownField _searchCountDropdown;
    private Label _costLabel;
    private Label _durationLabel;
    private Label _successLabel;
    private Button _launchBtn;
    private Button _cancelBtn;

    private readonly List<Toggle> _skillToggles = new List<Toggle>();
    private readonly List<EventCallback<ChangeEvent<bool>>> _skillCallbacks = new List<EventCallback<ChangeEvent<bool>>>();

    private static readonly List<string> _caOptions = new List<string>
        { "Any", "20", "40", "60", "80", "100", "120", "140", "160" };
    private static readonly List<string> _paOptions = new List<string>
        { "★1", "★2", "★3", "★4" };
    private static readonly List<string> _countOptions = new List<string>
        { "1 Candidate", "2 Candidates", "3 Candidates" };

    public void Initialize(VisualElement root)
    {
        _root = root;

        // ── Header ───────────────────────────────────────────────────────────────
        var header = new VisualElement();
        header.AddToClassList("modal-header");
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.alignItems = Align.Center;

        var title = new Label("Assign Search Task");
        title.AddToClassList("text-xl");
        title.AddToClassList("text-bold");
        header.Add(title);

        _cancelBtn = new Button { text = "X" };
        _cancelBtn.AddToClassList("btn-sm");
        _cancelBtn.style.minWidth = 30;
        header.Add(_cancelBtn);
        _root.Add(header);

        // ── Scrollable body ───────────────────────────────────────────────────────
        var bodyScroll = new ScrollView();
        bodyScroll.AddToClassList("modal-body");
        bodyScroll.style.flexGrow = 1;
        bodyScroll.style.flexShrink = 1;
        var body = bodyScroll.contentContainer;
        _root.Add(bodyScroll);

        // Team name
        _teamNameLabel = new Label();
        _teamNameLabel.AddToClassList("text-muted");
        _teamNameLabel.style.marginBottom = 12;
        body.Add(_teamNameLabel);

        // Role dropdown
        AddFieldRow("Role", out var roleRow, body);
        _roleDropdown = new DropdownField(BuildRoleOptions(), 0);
        roleRow.Add(_roleDropdown);

        // CA range
        AddFieldRow("Min CA", out var minCARow, body);
        _minCADropdown = new DropdownField(_caOptions, 0);
        minCARow.Add(_minCADropdown);

        AddFieldRow("Max CA", out var maxCARow, body);
        _maxCADropdown = new DropdownField(_caOptions, 0);
        maxCARow.Add(_maxCADropdown);

        // PA range
        AddFieldRow("Min PA Stars", out var minPARow, body);
        _minPADropdown = new DropdownField(_paOptions, 0);
        minPARow.Add(_minPADropdown);

        AddFieldRow("Max PA Stars", out var maxPARow, body);
        _maxPADropdown = new DropdownField(_paOptions, 3);
        maxPARow.Add(_maxPADropdown);

        // Desired skills toggles
        var skillsLabel = new Label("Desired Skills");
        skillsLabel.AddToClassList("text-sm");
        skillsLabel.AddToClassList("text-muted");
        skillsLabel.style.marginTop = 8;
        skillsLabel.style.marginBottom = 4;
        body.Add(skillsLabel);

        _skillTogglesContainer = new VisualElement();
        _skillTogglesContainer.style.flexDirection = FlexDirection.Row;
        _skillTogglesContainer.style.flexWrap = Wrap.Wrap;
        _skillTogglesContainer.style.marginBottom = 8;
        body.Add(_skillTogglesContainer);

        _skillToggles.Clear();
        _skillCallbacks.Clear();
        for (int i = 0; i < SkillTypeHelper.SkillTypeCount; i++)
        {
            var toggle = new Toggle(SkillTypeHelper.GetName((SkillType)i));
            toggle.style.marginRight = 8;
            toggle.style.marginBottom = 4;
            _skillTogglesContainer.Add(toggle);
            _skillToggles.Add(toggle);

            int captured = i;
            EventCallback<ChangeEvent<bool>> cb = evt => _viewModel?.SetDesiredSkill(captured, evt.newValue);
            _skillCallbacks.Add(cb);
            toggle.RegisterValueChangedCallback(cb);
        }

        // Search count
        AddFieldRow("Candidates", out var countRow, body);
        _searchCountDropdown = new DropdownField(_countOptions, 0);
        countRow.Add(_searchCountDropdown);

        // Preview strip
        var previewCard = new VisualElement();
        previewCard.AddToClassList("card");
        previewCard.style.flexDirection = FlexDirection.Row;
        previewCard.style.justifyContent = Justify.SpaceBetween;
        previewCard.style.marginTop = 12;
        previewCard.style.paddingTop = 8;
        previewCard.style.paddingBottom = 8;
        previewCard.style.paddingLeft = 10;
        previewCard.style.paddingRight = 10;

        _costLabel = new Label();
        _costLabel.AddToClassList("text-sm");
        previewCard.Add(_costLabel);

        _durationLabel = new Label();
        _durationLabel.AddToClassList("text-sm");
        previewCard.Add(_durationLabel);

        _successLabel = new Label();
        _successLabel.AddToClassList("text-sm");
        previewCard.Add(_successLabel);
        body.Add(previewCard);

        // ── Footer ───────────────────────────────────────────────────────────────
        var footer = new VisualElement();
        footer.AddToClassList("modal-footer");
        footer.style.justifyContent = Justify.FlexEnd;
        _root.Add(footer);

        _launchBtn = new Button { text = "Launch Search" };
        _launchBtn.AddToClassList("btn-primary");
        footer.Add(_launchBtn);
    }

    public void Bind(IViewModel viewModel)
    {
        _viewModel = viewModel as HRSearchConfiguratorViewModel;
        if (_viewModel == null) return;

        // Populate static fields
        UpdatePreviewLabels();
        if (_teamNameLabel != null)
            _teamNameLabel.text = "Team: " + _viewModel.TeamName;

        // Wire dropdowns
        if (_roleDropdown != null)
        {
            _roleDropdown.UnregisterValueChangedCallback(OnRoleChanged);
            _roleDropdown.RegisterValueChangedCallback(OnRoleChanged);
        }
        if (_minCADropdown != null)
        {
            _minCADropdown.UnregisterValueChangedCallback(OnMinCAChanged);
            _minCADropdown.RegisterValueChangedCallback(OnMinCAChanged);
        }
        if (_maxCADropdown != null)
        {
            _maxCADropdown.UnregisterValueChangedCallback(OnMaxCAChanged);
            _maxCADropdown.RegisterValueChangedCallback(OnMaxCAChanged);
        }
        if (_minPADropdown != null)
        {
            _minPADropdown.UnregisterValueChangedCallback(OnMinPAChanged);
            _minPADropdown.RegisterValueChangedCallback(OnMinPAChanged);
        }
        if (_maxPADropdown != null)
        {
            _maxPADropdown.UnregisterValueChangedCallback(OnMaxPAChanged);
            _maxPADropdown.RegisterValueChangedCallback(OnMaxPAChanged);
        }
        if (_searchCountDropdown != null)
        {
            _searchCountDropdown.UnregisterValueChangedCallback(OnCountChanged);
            _searchCountDropdown.RegisterValueChangedCallback(OnCountChanged);
        }

        // Skill toggles are wired once in Initialize via stored callbacks — nothing to re-wire here

        if (_launchBtn != null)
        {
            _launchBtn.clicked -= OnLaunchClicked;
            _launchBtn.clicked += OnLaunchClicked;
            _launchBtn.SetEnabled(_viewModel.CanLaunch);
        }
        if (_cancelBtn != null)
        {
            _cancelBtn.clicked -= OnCancelClicked;
            _cancelBtn.clicked += OnCancelClicked;
        }
    }

    public void Dispose()
    {
        _viewModel = null;
    }

    private void OnRoleChanged(ChangeEvent<string> evt)
    {
        if (_viewModel == null) return;
        var role = ParseRole(evt.newValue);
        _viewModel.SetRole(role);
        UpdatePreviewLabels();
    }

    private void OnMinCAChanged(ChangeEvent<string> evt)
    {
        if (_viewModel == null) return;
        int min = ParseCAOption(evt.newValue);
        _viewModel.SetCARange(min, _viewModel.MaxAbility);
        UpdatePreviewLabels();
    }

    private void OnMaxCAChanged(ChangeEvent<string> evt)
    {
        if (_viewModel == null) return;
        int max = ParseCAOption(evt.newValue);
        _viewModel.SetCARange(_viewModel.MinAbility, max);
        UpdatePreviewLabels();
    }

    private void OnMinPAChanged(ChangeEvent<string> evt)
    {
        if (_viewModel == null) return;
        int min = ParsePAOption(evt.newValue);
        _viewModel.SetPARange(min, _viewModel.MaxPAStars);
        UpdatePreviewLabels();
    }

    private void OnMaxPAChanged(ChangeEvent<string> evt)
    {
        if (_viewModel == null) return;
        int max = ParsePAOption(evt.newValue);
        _viewModel.SetPARange(_viewModel.MinPAStars, max);
        UpdatePreviewLabels();
    }

    private void OnCountChanged(ChangeEvent<string> evt)
    {
        if (_viewModel == null) return;
        int count = _countOptions.IndexOf(evt.newValue) + 1;
        _viewModel.SetSearchCount(count);
    }

    private void OnLaunchClicked()
    {
        _viewModel?.LaunchSearch();
    }

    private void OnCancelClicked()
    {
        _viewModel?.Dismiss();
    }

    private void UpdatePreviewLabels()
    {
        if (_viewModel == null) return;
        if (_costLabel != null)    _costLabel.text    = "Cost: " + _viewModel.CostPreview;
        if (_durationLabel != null) _durationLabel.text = "Duration: " + _viewModel.DurationPreview;
        if (_successLabel != null) _successLabel.text = "Success: " + _viewModel.SuccessChancePreview;
        if (_launchBtn != null)    _launchBtn.SetEnabled(_viewModel.CanLaunch);
    }

    private void AddFieldRow(string labelText, out VisualElement valueContainer, VisualElement parent)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.alignItems = Align.Center;
        row.style.marginBottom = 6;

        var lbl = new Label(labelText);
        lbl.AddToClassList("text-sm");
        lbl.style.minWidth = 100;
        row.Add(lbl);

        valueContainer = new VisualElement();
        valueContainer.style.flexGrow = 1;
        row.Add(valueContainer);

        parent.Add(row);
    }

    private static List<string> BuildRoleOptions()
    {
        return new List<string>
        {
            "Developer",
            "Designer",
            "QA Engineer",
            "SFX Artist",
            "VFX Artist",
            "Accountant",
            "HR Specialist"
        };
    }

    private static EmployeeRole ParseRole(string label)
    {
        switch (label)
        {
            case "Designer":      return EmployeeRole.Designer;
            case "QA Engineer":   return EmployeeRole.QAEngineer;
            case "SFX Artist":    return EmployeeRole.SoundEngineer;
            case "VFX Artist":    return EmployeeRole.VFXArtist;
            case "Accountant":    return EmployeeRole.Accountant;
            case "HR Specialist": return EmployeeRole.HR;
            default:              return EmployeeRole.Developer;
        }
    }

    private static int ParseCAOption(string label)
    {
        if (label == "Any") return 0;
        int val;
        return int.TryParse(label, out val) ? val : 0;
    }

    private static int ParsePAOption(string label)
    {
        // "★1" → 1, "★2" → 2, etc.
        if (label.Length > 1 && label[0] == '★')
        {
            int val;
            return int.TryParse(label.Substring(1), out val) ? val : 1;
        }
        return 1;
    }
}
