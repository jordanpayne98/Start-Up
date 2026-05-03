using UnityEngine.UIElements;

/// <summary>
/// View for the top status bar shell region.
/// Follows UI_Architecture_v3: Initialize caches elements and wires handlers.
/// Bind only updates text, classes, and values.
/// Dispose unregisters all handlers.
/// </summary>
public class TopBarView : IGameView
{
    private readonly GameController _gameController;

    // Cached elements — company block
    private Label _companyName;
    private Label _companyTier;

    // Cash block
    private Label _cashValue;
    private Label _cashLabel;

    // Net/month block
    private Label _netValue;
    private Label _netLabel;

    // Runway block
    private Label         _runwayValue;
    private Label         _runwayLabel;
    private VisualElement _runwayInfoIcon;

    // Timeline
    private Label _timelineLabel;

    // Speed group
    private Button _speed1x;
    private Button _speed2x;
    private Button _speed3x;

    // Action buttons
    private Button _continueButton;
    private Button _settingsButton;

    // Services
    private ICommandDispatcher _commands;
    private IModalPresenter    _modals;
    private ITooltipProvider   _tooltips;

    // ViewModel reference for Bind
    private TopBarViewModel _vm;

    // Tooltip state
    private bool _tooltipRegistered;

    public TopBarView(GameController gameController)
    {
        _gameController = gameController;
    }

    // ── Initialize ─────────────────────────────────────────────────────────────

    public void Initialize(VisualElement root, UIServices services)
    {
        _commands = services.Commands;
        _modals   = services.Modals;
        _tooltips = services.Tooltips;

        // Company block
        _companyName = root.Q<Label>("company-name");
        _companyTier = root.Q<Label>("company-tier");

        // Metric blocks
        _cashValue  = root.Q<Label>("cash-value");
        _cashLabel  = root.Q<Label>("cash-label");
        _netValue   = root.Q<Label>("net-value");
        _netLabel   = root.Q<Label>("net-label");
        _runwayValue    = root.Q<Label>("runway-value");
        _runwayLabel    = root.Q<Label>("runway-label");
        _runwayInfoIcon = root.Q<VisualElement>("runway-info-icon");

        // Timeline
        _timelineLabel = root.Q<Label>("timeline-label");

        // Speed buttons
        _speed1x = root.Q<Button>("speed-1x");
        _speed2x = root.Q<Button>("speed-2x");
        _speed3x = root.Q<Button>("speed-3x");

        // Action buttons
        _continueButton = root.Q<Button>("continue-button");
        _settingsButton = root.Q<Button>("settings-button");

        // Wire handlers (named methods — never lambdas)
        if (_speed1x      != null) _speed1x.clicked      += OnSpeed1xClicked;
        if (_speed2x      != null) _speed2x.clicked      += OnSpeed2xClicked;
        if (_speed3x      != null) _speed3x.clicked      += OnSpeed3xClicked;
        if (_continueButton != null) _continueButton.clicked += OnContinueClicked;
        if (_settingsButton != null) _settingsButton.clicked += OnSettingsClicked;

        // Tooltip on runway info icon
        RegisterRunwayTooltip();
    }

    // ── Bind ───────────────────────────────────────────────────────────────────

    public void Bind(IViewModel viewModel)
    {
        if (viewModel is not TopBarViewModel vm) return;
        _vm = vm;

        // Company block
        if (_companyName != null) _companyName.text = vm.CompanyName;
        if (_companyTier != null) _companyTier.text = vm.CompanyTier;

        // Cash
        if (_cashValue != null) _cashValue.text = vm.CashDisplay;
        if (_cashLabel != null) _cashLabel.text = vm.CashLabel;

        // Net / month — severity classes
        if (_netValue != null)
        {
            _netValue.text = vm.NetMonthDisplay;
            ApplySeverityClass(_netValue, vm.NetMonthSeverity);
        }
        if (_netLabel != null) _netLabel.text = vm.NetMonthLabel;

        // Runway — severity classes
        if (_runwayValue != null)
        {
            _runwayValue.text = vm.RunwayDisplay;
            ApplySeverityClass(_runwayValue, vm.RunwaySeverity);
        }
        if (_runwayLabel != null) _runwayLabel.text = vm.RunwayLabel;

        // Timeline
        if (_timelineLabel != null) _timelineLabel.text = vm.TimelineDisplay;

        // Speed toggle group
        UpdateSpeedActiveClass(_speed1x, vm.CurrentSpeed == 1);
        UpdateSpeedActiveClass(_speed2x, vm.CurrentSpeed == 2);
        UpdateSpeedActiveClass(_speed3x, vm.CurrentSpeed == 3);

        // Continue button — label and primary/secondary class
        if (_continueButton != null)
        {
            _continueButton.text = vm.ContinueLabel;
            _continueButton.EnableInClassList("btn-primary",   vm.IsPaused);
            _continueButton.EnableInClassList("btn-secondary", !vm.IsPaused);
        }
    }

    // ── Dispose ────────────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_speed1x      != null) _speed1x.clicked      -= OnSpeed1xClicked;
        if (_speed2x      != null) _speed2x.clicked      -= OnSpeed2xClicked;
        if (_speed3x      != null) _speed3x.clicked      -= OnSpeed3xClicked;
        if (_continueButton != null) _continueButton.clicked -= OnContinueClicked;
        if (_settingsButton != null) _settingsButton.clicked -= OnSettingsClicked;

        UnregisterRunwayTooltip();

        _vm      = null;
        _commands = null;
        _modals   = null;
        _tooltips = null;
    }

    // ── Click Handlers ─────────────────────────────────────────────────────────

    private void OnSpeed1xClicked() => SetSpeed(1);
    private void OnSpeed2xClicked() => SetSpeed(2);
    private void OnSpeed3xClicked() => SetSpeed(3);

    private void SetSpeed(int speed)
    {
        if (_gameController == null) return;
        _gameController.SetGameSpeed(speed);
        _vm?.SetCurrentSpeed(speed);
        // Immediately reflect new speed without waiting for coalesced refresh
        UpdateSpeedActiveClass(_speed1x, speed == 1);
        UpdateSpeedActiveClass(_speed2x, speed == 2);
        UpdateSpeedActiveClass(_speed3x, speed == 3);
    }

    private void OnContinueClicked()
    {
        if (_gameController == null) return;
        if (_gameController.IsAdvancing)
            _gameController.StopAdvance();
        else
            _gameController.StartAdvance();
        // Immediate label flip — coalesced refresh will follow
        if (_continueButton != null)
        {
            bool nowPaused = !_gameController.IsAdvancing;
            _continueButton.text = nowPaused ? "Continue" : "Pause";
            _continueButton.EnableInClassList("btn-primary",   nowPaused);
            _continueButton.EnableInClassList("btn-secondary", !nowPaused);
        }
    }

    private void OnSettingsClicked()
    {
        _modals?.DismissAll();   // close any open modal first
        // Settings / pause menu is opened via ESC or PauseMenuView; raise event via
        // dedicated modal path once PauseMenuView is wired into the new shell layer.
        // For now, trigger ESC simulation is not available — no-op until PauseMenuView
        // is attached to the new modal-layer in a follow-up plan.
    }

    // ── Tooltip Helpers ────────────────────────────────────────────────────────

    private void RegisterRunwayTooltip()
    {
        if (_runwayInfoIcon == null || _tooltips == null) return;
        _runwayInfoIcon.RegisterCallback<MouseEnterEvent>(OnRunwayIconMouseEnter);
        _runwayInfoIcon.RegisterCallback<MouseLeaveEvent>(OnRunwayIconMouseLeave);
        _tooltipRegistered = true;
    }

    private void UnregisterRunwayTooltip()
    {
        if (!_tooltipRegistered || _runwayInfoIcon == null) return;
        _runwayInfoIcon.UnregisterCallback<MouseEnterEvent>(OnRunwayIconMouseEnter);
        _runwayInfoIcon.UnregisterCallback<MouseLeaveEvent>(OnRunwayIconMouseLeave);
        _tooltipRegistered = false;
    }

    private void OnRunwayIconMouseEnter(MouseEnterEvent _)
    {
        if (_vm == null || _tooltips == null || _runwayInfoIcon == null) return;
        _tooltips.Show(_vm.RunwayTooltip, _runwayInfoIcon);
    }

    private void OnRunwayIconMouseLeave(MouseLeaveEvent _)
    {
        _tooltips?.Hide();
    }

    // ── Private USS Helpers ────────────────────────────────────────────────────

    private static void ApplySeverityClass(VisualElement element, SeverityState severity)
    {
        element.EnableInClassList("severity-warning", severity == SeverityState.Warning);
        element.EnableInClassList("severity-danger",  severity == SeverityState.Danger);
    }

    private static void UpdateSpeedActiveClass(Button btn, bool isActive)
    {
        if (btn == null) return;
        btn.EnableInClassList("speed-active", isActive);
    }
}
