using UnityEngine;
using UnityEngine.UIElements;

public class AssignmentModalView : IGameView
{
    private readonly IModalPresenter _modal;
    private readonly ICommandDispatcher _dispatcher;

    private VisualElement _root;
    private AssignmentModalViewModel _vm;

    // Header
    private Button _dismissButton;
    private Label _titleLabel;

    // Role dropdown
    private DropdownField _roleDropdown;

    // Ability slider
    private SliderInt _abilitySlider;
    private Label _abilityValueLabel;

    // Potential slider (0–5 stars)
    private SliderInt _potentialSlider;
    private Label _potentialValueLabel;

    // Batch size buttons
    private readonly Button[] _batchButtons = new Button[5];

    // Preview section
    private Label _costLabel;
    private Label _durationLabel;
    private Label _noHRWarning;

    // Footer
    private Button _cancelButton;
    private Button _confirmButton;

    public AssignmentModalView(IModalPresenter modal, ICommandDispatcher dispatcher) {
        _modal = modal;
        _dispatcher = dispatcher;
    }

    public void Initialize(VisualElement root) {
        _root = root;
        _root.AddToClassList("modal-content--compact");

        // ── Header ───────────────────────────────────────────────────────────────
        var header = new VisualElement();
        header.AddToClassList("modal-header");
        header.AddToClassList("flex-row");
        header.AddToClassList("justify-between");
        header.style.alignItems = Align.Center;

        _titleLabel = new Label("New Search Assignment");
        _titleLabel.AddToClassList("metric-secondary");
        _titleLabel.AddToClassList("text-bold");
        header.Add(_titleLabel);

        _dismissButton = new Button { text = "X" };
        _dismissButton.AddToClassList("btn-sm");
        _dismissButton.style.minWidth = 30;
        header.Add(_dismissButton);
        _root.Add(header);

        // ── Scrollable body ───────────────────────────────────────────────────────
        var bodyScroll = new ScrollView();
        bodyScroll.AddToClassList("modal-body");
        bodyScroll.style.flexGrow = 1;
        bodyScroll.style.flexShrink = 1;
        var body = bodyScroll.contentContainer;
        _root.Add(bodyScroll);

        // No HR warning
        _noHRWarning = new Label("No HR Team \u2014 Assign employees to the HR role before starting a search.");
        _noHRWarning.AddToClassList("badge");
        _noHRWarning.AddToClassList("badge--danger");
        _noHRWarning.style.marginBottom = 12;
        _noHRWarning.style.display = DisplayStyle.None;
        body.Add(_noHRWarning);

        // Role dropdown
        BuildSectionHeader(body, "Target Role");
        _roleDropdown = new DropdownField();
        _roleDropdown.style.marginBottom = 16;
        body.Add(_roleDropdown);

        // Ability slider
        BuildSectionHeader(body, "Min. Current Ability (0 = Any)");
        var abilityRow = new VisualElement();
        abilityRow.AddToClassList("flex-row");
        abilityRow.style.alignItems = Align.Center;
        abilityRow.style.marginBottom = 16;

        _abilitySlider = new SliderInt(0, 200, SliderDirection.Horizontal, 10);
        _abilitySlider.style.flexGrow = 1;
        abilityRow.Add(_abilitySlider);

        _abilityValueLabel = new Label("Any");
        _abilityValueLabel.AddToClassList("metric-secondary");
        _abilityValueLabel.style.minWidth = 40;
        _abilityValueLabel.style.unityTextAlign = TextAnchor.MiddleRight;
        abilityRow.Add(_abilityValueLabel);
        body.Add(abilityRow);

        // Potential slider
        BuildSectionHeader(body, "Min. Potential (Stars)");
        var potentialRow = new VisualElement();
        potentialRow.AddToClassList("flex-row");
        potentialRow.style.alignItems = Align.Center;
        potentialRow.style.marginBottom = 16;

        _potentialSlider = new SliderInt(0, 5, SliderDirection.Horizontal, 1);
        _potentialSlider.style.flexGrow = 1;
        potentialRow.Add(_potentialSlider);

        _potentialValueLabel = new Label("Any");
        _potentialValueLabel.AddToClassList("metric-secondary");
        _potentialValueLabel.style.minWidth = 40;
        _potentialValueLabel.style.unityTextAlign = TextAnchor.MiddleRight;
        potentialRow.Add(_potentialValueLabel);
        body.Add(potentialRow);

        // Batch size
        BuildSectionHeader(body, "Batch Size (candidates)");
        var batchRow = new VisualElement();
        batchRow.AddToClassList("flex-row");
        batchRow.style.marginBottom = 16;

        for (int i = 0; i < 5; i++) {
            int batchVal = i + 1;
            var btn = new Button { text = batchVal.ToString() };
            btn.AddToClassList("btn-secondary");
            btn.AddToClassList("btn-sm");
            btn.AddToClassList("assignment-batch-btn");
            btn.userData = batchVal;
            batchRow.Add(btn);
            _batchButtons[i] = btn;
            btn.RegisterCallback<ClickEvent>(OnBatchButtonClickEvent);
        }
        body.Add(batchRow);

        // Cost / duration preview
        var previewCard = new VisualElement();
        previewCard.AddToClassList("card");
        previewCard.style.marginBottom = 16;

        _costLabel = CreateInfoRow(previewCard, "Search cost");
        _durationLabel = CreateInfoRow(previewCard, "Duration");
        body.Add(previewCard);

        // ── Footer ───────────────────────────────────────────────────────────────
        var footer = new VisualElement();
        footer.AddToClassList("modal-footer");
        footer.AddToClassList("flex-row");
        footer.AddToClassList("justify-end");
        footer.style.marginTop = 8;
        _root.Add(footer);

        _cancelButton = new Button { text = "Cancel" };
        _cancelButton.AddToClassList("btn-secondary");
        _cancelButton.style.marginRight = 8;
        footer.Add(_cancelButton);

        _confirmButton = new Button { text = "Start Search" };
        _confirmButton.AddToClassList("btn-primary");
        footer.Add(_confirmButton);

        // Wire handlers
        _dismissButton.clicked += OnDismissClicked;
        _cancelButton.clicked  += OnCancelClicked;
        _confirmButton.clicked += OnConfirmClicked;

        _abilitySlider.RegisterValueChangedCallback(OnAbilitySliderChanged);
        _potentialSlider.RegisterValueChangedCallback(OnPotentialSliderChanged);
        _roleDropdown.RegisterValueChangedCallback(OnRoleDropdownChanged);
    }

    public void Bind(IViewModel viewModel) {
        _vm = viewModel as AssignmentModalViewModel;
        if (_vm == null) return;

        _titleLabel.text = _vm.IsEditMode ? "Edit Search Assignment" : "New Search Assignment";
        _confirmButton.text = _vm.IsEditMode ? "Update Search" : "Start Search";

        // Roles
        if (_roleDropdown.choices == null || _roleDropdown.choices.Count == 0) {
            _roleDropdown.choices = _vm.AvailableRoles;
        }
        if (_vm.SelectedRoleIndex >= 0 && _vm.SelectedRoleIndex < _vm.AvailableRoles.Count) {
            _roleDropdown.index = _vm.SelectedRoleIndex;
        }

        // Sliders
        _abilitySlider.SetValueWithoutNotify(_vm.MinAbility);
        _abilityValueLabel.text = _vm.MinAbility == 0 ? "Any" : _vm.MinAbility.ToString();

        _potentialSlider.SetValueWithoutNotify(_vm.MinPotential);
        _potentialValueLabel.text = _vm.MinPotential == 0 ? "Any" : AbilityCalculator.PotentialStarsDisplay(_vm.MinPotential);

        // Batch
        UpdateBatchButtonClasses();

        // Preview
        _costLabel.text = UIFormatting.FormatMoney(_vm.EstimatedCost);
        _durationLabel.text = _vm.EstimatedDurationDays + "d";

        // Validation
        bool noHR = !_vm.HasHRTeam;
        _noHRWarning.style.display = noHR ? DisplayStyle.Flex : DisplayStyle.None;
        _confirmButton.SetEnabled(_vm.CanConfirm);
    }

    public void Dispose() {
        if (_dismissButton != null) _dismissButton.clicked -= OnDismissClicked;
        if (_cancelButton != null)  _cancelButton.clicked  -= OnCancelClicked;
        if (_confirmButton != null) _confirmButton.clicked -= OnConfirmClicked;

        if (_abilitySlider != null)   _abilitySlider.UnregisterValueChangedCallback(OnAbilitySliderChanged);
        if (_potentialSlider != null) _potentialSlider.UnregisterValueChangedCallback(OnPotentialSliderChanged);
        if (_roleDropdown != null)    _roleDropdown.UnregisterValueChangedCallback(OnRoleDropdownChanged);

        for (int i = 0; i < 5; i++) {
            if (_batchButtons[i] != null) _batchButtons[i].UnregisterCallback<ClickEvent>(OnBatchButtonClickEvent);
        }

        _vm = null;
        _root = null;
    }

    // --- Handlers ---

    private void OnDismissClicked() => _modal.DismissModal();
    private void OnCancelClicked()  => _modal.DismissModal();

    private void OnConfirmClicked() {
        if (_vm == null || !_vm.CanConfirm) return;

        var cmd = new StartHRSearchCommand {
            TeamId       = _vm.HRTeamId,
            TargetRole   = _vm.SelectedRole,
            MinCA        = _vm.MinAbility,
            MaxCA        = 0,
            MinPAStars   = _vm.MinPotential,
            MaxPAStars   = 0,
            DesiredSkills = null,
            SearchCount  = _vm.BatchSize
        };
        _dispatcher.Dispatch(cmd);
        _modal.DismissModal();
    }

    private void OnRoleDropdownChanged(ChangeEvent<string> evt) {
        if (_vm == null) return;
        _vm.SelectedRoleIndex = _roleDropdown.index;
        _confirmButton.SetEnabled(_vm.CanConfirm);
    }

    private void OnAbilitySliderChanged(ChangeEvent<int> evt) {
        if (_vm == null) return;
        _vm.MinAbility = evt.newValue;
        _abilityValueLabel.text = evt.newValue == 0 ? "Any" : evt.newValue.ToString();
        RefreshPreview();
    }

    private void OnPotentialSliderChanged(ChangeEvent<int> evt) {
        if (_vm == null) return;
        _vm.MinPotential = evt.newValue;
        _potentialValueLabel.text = evt.newValue == 0 ? "Any" : AbilityCalculator.PotentialStarsDisplay(evt.newValue);
        RefreshPreview();
    }

    private void OnBatchButtonClickEvent(ClickEvent evt) {
        if (_vm == null) return;
        var el = evt.currentTarget as VisualElement;
        if (el == null || !(el.userData is int batchVal)) return;
        _vm.BatchSize = batchVal;
        UpdateBatchButtonClasses();
        RefreshPreview();
    }

    private void RefreshPreview() {
        if (_vm == null) return;
        _costLabel.text = UIFormatting.FormatMoney(_vm.EstimatedCost);
        _durationLabel.text = _vm.EstimatedDurationDays + "d";
        _confirmButton.SetEnabled(_vm.CanConfirm);
    }

    private void UpdateBatchButtonClasses() {
        for (int i = 0; i < 5; i++) {
            var btn = _batchButtons[i];
            if (btn == null) continue;
            bool isSelected = (i + 1) == _vm.BatchSize;
            if (isSelected) {
                btn.RemoveFromClassList("btn-secondary");
                btn.AddToClassList("btn-primary");
            } else {
                btn.RemoveFromClassList("btn-primary");
                btn.AddToClassList("btn-secondary");
            }
        }
    }

    // --- Helpers ---

    private static Label CreateInfoRow(VisualElement parent, string label) {
        var row = new VisualElement();
        row.AddToClassList("flex-row");
        row.AddToClassList("justify-between");
        row.style.marginBottom = 4;

        var labelEl = new Label(label);
        labelEl.AddToClassList("metric-tertiary");
        row.Add(labelEl);

        var valueEl = new Label("--");
        valueEl.AddToClassList("metric-secondary");
        row.Add(valueEl);

        parent.Add(row);
        return valueEl;
    }

    private static Label BuildSectionHeader(VisualElement parent, string text) {
        var label = new Label(text);
        label.AddToClassList("metric-tertiary");
        label.AddToClassList("text-bold");
        label.style.marginBottom = 4;
        parent.Add(label);
        return label;
    }
}
