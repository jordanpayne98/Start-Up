using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class EmployeeProfileView : IGameView
{
    private readonly ICommandDispatcher _dispatcher;
    private readonly IModalPresenter _modal;
    private VisualElement _root;
    private Label _nameLabel;
    private Label _roleLabel;
    private Label _teamLabel;
    private Label _salaryLabel;
    private Label _hireDateLabel;
    private Label _moraleLabel;
    private VisualElement _moraleFill;
    private VisualElement _abilityStars;
    private VisualElement _potentialStars;
    private RadarChartElement _radarChart;
    private List<RadarChartElement.AxisData> _radarCache;
    private Button _fireButton;
    private Button _closeButton;
    private Label _founderBadge;
    private EmployeeProfileViewModel _vm;

    public EmployeeProfileView(ICommandDispatcher dispatcher, IModalPresenter modal) {
        _dispatcher = dispatcher;
        _modal = modal;
        _radarCache = new List<RadarChartElement.AxisData>(9);
    }

    public void Initialize(VisualElement root) {
        _root = root;

        // Header
        var header = new VisualElement();
        header.AddToClassList("card");
        header.style.marginBottom = 16;

        _nameLabel = new Label("Employee");
        _nameLabel.AddToClassList("text-2xl");
        _nameLabel.AddToClassList("text-bold");
        header.Add(_nameLabel);

        var infoRow = new VisualElement();
        infoRow.AddToClassList("flex-row");
        infoRow.style.marginTop = 8;

        _founderBadge = new Label("Founder");
        _founderBadge.AddToClassList("badge");
        _founderBadge.AddToClassList("badge--founder");
        _founderBadge.style.marginRight = 8;
        infoRow.Add(_founderBadge);

        _roleLabel = new Label();
        _roleLabel.AddToClassList("badge");
        _roleLabel.AddToClassList("badge--primary");
        _roleLabel.style.marginRight = 8;
        infoRow.Add(_roleLabel);

        _teamLabel = new Label();
        _teamLabel.AddToClassList("text-muted");
        infoRow.Add(_teamLabel);

        header.Add(infoRow);

        // Stars
        var starsRow = new VisualElement();
        starsRow.AddToClassList("flex-row");
        starsRow.style.marginTop = 8;

        var caLabel = new Label("Ability: ");
        caLabel.AddToClassList("text-sm");
        caLabel.AddToClassList("text-muted");
        starsRow.Add(caLabel);
        _abilityStars = CreateStarRow();
        starsRow.Add(_abilityStars);

        var paLabel = new Label("  Potential: ");
        paLabel.AddToClassList("text-sm");
        paLabel.AddToClassList("text-muted");
        paLabel.style.marginLeft = 16;
        starsRow.Add(paLabel);
        _potentialStars = CreateStarRow();
        starsRow.Add(_potentialStars);

        header.Add(starsRow);
        _root.Add(header);

        // Info strip
        var infoStrip = new VisualElement();
        infoStrip.AddToClassList("card");
        infoStrip.AddToClassList("flex-row");
        infoStrip.AddToClassList("align-center");
        infoStrip.style.marginBottom = 0;

        _salaryLabel = new Label();
        infoStrip.Add(_salaryLabel);

        var sep1 = new Label("|");
        sep1.AddToClassList("text-muted");
        sep1.style.marginLeft = 10;
        sep1.style.marginRight = 10;
        infoStrip.Add(sep1);

        _hireDateLabel = new Label();
        _hireDateLabel.AddToClassList("text-sm");
        _hireDateLabel.AddToClassList("text-muted");
        infoStrip.Add(_hireDateLabel);

        var sep2 = new Label("|");
        sep2.AddToClassList("text-muted");
        sep2.style.marginLeft = 10;
        sep2.style.marginRight = 10;
        infoStrip.Add(sep2);

        var moraleBar = new VisualElement();
        moraleBar.AddToClassList("progress-bar");
        moraleBar.style.width = 80;
        moraleBar.style.height = 8;
        _moraleFill = new VisualElement();
        _moraleFill.AddToClassList("progress-bar__fill");
        moraleBar.Add(_moraleFill);
        infoStrip.Add(moraleBar);

        _moraleLabel = new Label();
        _moraleLabel.AddToClassList("text-sm");
        _moraleLabel.style.marginLeft = 8;
        infoStrip.Add(_moraleLabel);

        _root.Add(infoStrip);

        // Radar chart
        _radarChart = new RadarChartElement();
        _radarChart.style.width = 360;
        _radarChart.style.height = 360;
        _radarChart.style.alignSelf = UnityEngine.UIElements.Align.Center;
        _radarChart.style.marginTop = 12;
        _radarChart.style.marginBottom = 12;
        _root.Add(_radarChart);

        // Action buttons row
        var buttonRow = new VisualElement();
        buttonRow.AddToClassList("flex-row");
        buttonRow.AddToClassList("justify-between");
        buttonRow.style.marginTop = 16;

        _closeButton = new Button { text = "Close" };
        _closeButton.AddToClassList("btn-secondary");
        _closeButton.clicked += OnCloseClicked;
        buttonRow.Add(_closeButton);

        _fireButton = new Button { text = "Fire Employee" };
        _fireButton.AddToClassList("btn-danger");
        _fireButton.clicked += OnFireClicked;
        buttonRow.Add(_fireButton);

        _root.Add(buttonRow);
    }

    public void Bind(IViewModel viewModel) {
        _vm = viewModel as EmployeeProfileViewModel;
        if (_vm == null) return;

        _nameLabel.text = _vm.Name;
        if (_vm.IsFounder) _nameLabel.AddToClassList("text-founder");
        else _nameLabel.RemoveFromClassList("text-founder");

        if (_founderBadge != null)
            _founderBadge.style.display = _vm.IsFounder ? DisplayStyle.Flex : DisplayStyle.None;

        _roleLabel.text = _vm.Role;
        _teamLabel.text = _vm.TeamName;
        _salaryLabel.text = "Salary: " + _vm.SalaryDisplay;
        _hireDateLabel.text = _vm.HireDateDisplay;
        _moraleLabel.text = _vm.Morale + "% - " + _vm.MoraleLabel;
        _moraleFill.style.width = Length.Percent(_vm.Morale);

        UpdateStars(_abilityStars, _vm.AbilityStars);
        UpdateStars(_potentialStars, _vm.PotentialStars);

        _radarCache.Clear();
        int skillCount = _vm.Skills.Count;
        for (int i = 0; i < skillCount; i++) {
            var s = _vm.Skills[i];
            _radarCache.Add(new RadarChartElement.AxisData {
                Name = s.Name,
                NormalizedValue = s.Value / (float)s.MaxValue,
                DeltaDirection = s.DeltaDirection,
                RawValue = s.Value,
                LabelColor = UIFormatting.GetSkillColor((SkillType)i)
            });
        }
        _radarChart.SetData(_radarCache);

        if (_fireButton != null)
            _fireButton.style.display = _vm.CanFire ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void Dispose() {
        if (_closeButton != null) _closeButton.clicked -= OnCloseClicked;
        if (_fireButton != null) _fireButton.clicked -= OnFireClicked;
        _radarChart = null;
        _radarCache = null;
        _vm = null;
    }

    private void OnCloseClicked() {
        _modal?.DismissModal();
    }

    private void OnFireClicked() {
        if (_vm == null || !_vm.CanFire) return;
        _dispatcher?.Dispatch(new FireEmployeeCommand {
            Tick = _dispatcher.CurrentTick,
            EmployeeId = _vm.EmployeeId
        });
        _modal?.DismissModal();
    }

    private VisualElement CreateStarRow() {
        var container = new VisualElement();
        container.AddToClassList("star-rating");
        for (int i = 0; i < 5; i++) {
            var star = new VisualElement();
            star.AddToClassList("star");
            star.AddToClassList("star-empty");
            container.Add(star);
        }
        return container;
    }

    private void UpdateStars(VisualElement container, int filled) {
        int childCount = container.childCount;
        for (int i = 0; i < childCount && i < 5; i++) {
            var star = container[i];
            star.RemoveFromClassList("star-filled");
            star.RemoveFromClassList("star-empty");
            star.AddToClassList(i < filled ? "star-filled" : "star-empty");
        }
    }

}
