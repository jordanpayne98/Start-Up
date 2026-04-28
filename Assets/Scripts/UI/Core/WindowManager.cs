using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class WindowManager : MonoBehaviour, ICommandDispatcher, IModalPresenter, INavigationService, ITooltipProvider
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private Font customFont;
    [SerializeField] private VisualTreeAsset candidateDetailAsset;
    [SerializeField] private VisualTreeAsset employeeDetailAsset;

    private GameController _gameController;
    public GameController GameController => _gameController;
    private GameEventBus _eventBus;
    private ScreenRegistry _registry;
    private TopBarViewModel _topBarViewModel;

    // Cached root elements
    private VisualElement _root;
    private VisualElement _topBar;
    private VisualElement _sidebar;
    private Button _sidebarCollapseBtn;
    private AccordionSidebar _accordion;
    private VisualElement _contentArea;
    private VisualElement _modalOverlay;
    private VisualElement _modalBackdrop;
    private VisualElement _modalContent;
    private VisualElement _toastContainer;

    // Top bar elements
    private Label _companyNameLabel;
    private Label _moneyLabel;
    private Label _dateLabel;
    private Label _reputationSubtitle;
    private Label _netIncomeLabel;
    private Button _continueButton;
    private VisualElement _moduleControls;
    private VisualElement _moduleFinance;

    // Delta floater
    private VisualElement _deltaContainer;

    // Money animation state
    private float _lastMoneyValue;
    private Color _moneyBaseColor = new Color(0.32f, 0.72f, 0.53f, 1f); // accent-success default
    private Tweener _moneyTweener;
    private Tweener _screenTweener;
    private string _lastReputationClass = "";
    private string _lastMoneyColourClass = "";

    // Navigation state
    private ScreenId _currentScreenId;
    private IGameView _currentView;
    private IViewModel _currentViewModel;

    // Modal state
    private IGameView _currentModalView;
    private IViewModel _currentModalViewModel;
    private Action _modalCleanup;

    // Pause menu
    private PauseMenuView _pauseMenuView;
    private PauseMenuViewModel _pauseMenuViewModel;
    private Button _settingsButton;
    private InputAction _escapeAction;

    // Toast state
    private readonly List<VisualElement> _activeToasts = new List<VisualElement>();
    private ToastNotificationController _toastController;

    // Tooltip state
    [SerializeField] private TooltipRegistry _tooltipRegistry;
    private TooltipService _tooltipService;
    public TooltipService TooltipService => _tooltipService;

    // Dirty flag coalescing
    private bool _viewDirty;
    private bool _topBarDirty;
    private bool _modalDirty;

    // View cache — keeps views alive across navigation
    private readonly Dictionary<ScreenId, (IGameView view, IViewModel vm, VisualElement container)> _viewCache
        = new Dictionary<ScreenId, (IGameView, IViewModel, VisualElement)>();

    // Reusable snapshot instance
    private GameStateSnapshot _cachedSnapshot;

    // Event-to-screen relevance map
    private static readonly Dictionary<Type, HashSet<ScreenId>> _eventRelevance = BuildEventRelevanceMap();

    public event Action<ScreenId> OnScreenChanged;

    private void Awake() {
        if (uiDocument == null) {
            uiDocument = GetComponent<UIDocument>();
        }
    }

    private void Start() {
        _gameController = FindFirstObjectByType<GameController>();
        if (_gameController == null) {
            Debug.LogError("[WindowManager] GameController not found in scene!");
            return;
        }

        _eventBus = _gameController.EventBus;
        _registry = new ScreenRegistry(this, this, this, this, candidateDetailAsset);
        _topBarViewModel = new TopBarViewModel();

        Initialize(uiDocument);
        SubscribeToEvents();
        RefreshTopBar();
        NavigateTo(ScreenId.DashboardInbox);

        // Wire ToastNotificationController
        _toastController = GetComponent<ToastNotificationController>();
        if (_toastController != null && _toastContainer != null)
        {
            _toastController.Initialize(
                _eventBus,
                (INavigationService)this,
                _toastContainer,
                () => _currentScreenId == ScreenId.DashboardInbox
            );
        }
        else if (_toastController == null)
        {
            Debug.LogWarning("[WindowManager] ToastNotificationController component not found. Toasts will not be shown.");
        }

        InitializePauseMenu();
        SetupEscapeKey();
    }

    private void OnDestroy() {
        DeltaLabel.Dispose();
        DOTween.Kill(_screenTweener);
        DOTween.Kill(_moneyTweener);
        foreach (var kvp in _viewCache) {
            kvp.Value.view?.Dispose();
        }
        _viewCache.Clear();
        DisposeCurrentModal();
        _pauseMenuView?.Dispose();
        _pauseMenuView = null;
        if (_escapeAction != null) {
            _escapeAction.performed -= OnEscapePerformed;
            _escapeAction.Disable();
            _escapeAction = null;
        }
        UnsubscribeFromEvents();
        _tooltipService?.Dispose();
    }

    public void Initialize(UIDocument doc) {
        _root = doc.rootVisualElement;
        if (_root == null) {
            Debug.LogError("[WindowManager] Root VisualElement is null!");
            return;
        }

        // Apply custom font to root element so it cascades to all children
        if (customFont != null) {
            _root.style.unityFontDefinition = new StyleFontDefinition(customFont);
        }

        _topBar = _root.Q<VisualElement>("top-bar");
        _sidebar = _root.Q<VisualElement>("sidebar");
        _sidebarCollapseBtn = _sidebar?.Q<Button>("sidebar-collapse");
        _contentArea = _root.Q<VisualElement>("content-area");
        _modalOverlay = _root.Q<VisualElement>("modal-overlay");
        _modalBackdrop = _root.Q<VisualElement>("modal-backdrop");
        _modalContent = _root.Q<VisualElement>("modal-content");
        _toastContainer = _root.Q<VisualElement>("toast-container");

        // Top bar
        _companyNameLabel = _topBar?.Q<Label>("company-name");
        _moneyLabel = _topBar?.Q<Label>("money-display");
        _dateLabel = _topBar?.Q<Label>("date-display");
        _reputationSubtitle = _topBar?.Q<Label>("reputation-subtitle");
        _netIncomeLabel = _topBar?.Q<Label>("net-income-display");
        _continueButton = _topBar?.Q<Button>("btn-continue");
        _moduleControls = _topBar?.Q<VisualElement>("module-controls");
        _moduleFinance = _topBar?.Q<VisualElement>("module-finance");
        _deltaContainer = _topBar?.Q<VisualElement>("delta-container");

        if (_continueButton != null) {
            _continueButton.clicked += OnContinueClicked;
        }

        // Cache money base color once the element resolves its style
        if (_moneyLabel != null) {
            _moneyLabel.RegisterCallbackOnce<GeometryChangedEvent>(_ => {
                _moneyBaseColor = _moneyLabel.resolvedStyle.color;
            });
        }

        // Modal backdrop click to dismiss
        if (_modalBackdrop != null) {
            _modalBackdrop.RegisterCallback<ClickEvent>(evt => DismissModal());
        }

        if (_sidebarCollapseBtn != null) {
            _sidebarCollapseBtn.clicked += ToggleSidebarCollapse;
        }

        SetupAccordion();

        // Wire TooltipService
        var tooltipContainer = _root.Q<VisualElement>("tooltip-container");
        _tooltipService = new TooltipService();
        _tooltipService.Initialize(_root, tooltipContainer, _tooltipRegistry);
        OnScreenChanged += _ => {
            _tooltipService?.Hide();
            _tooltipService?.ReregisterScrollViews();
        };
    }

    public void NavigateTo(ScreenId screen) {
        var config = _registry.GetConfig(screen);
        if (config.ViewFactory == null || config.ViewModelFactory == null) {
            Debug.LogWarning("[WindowManager] No factory registered for screen: " + screen);
            return;
        }

        // Hide current view's container instead of disposing
        if (_currentView != null && _viewCache.TryGetValue(_currentScreenId, out var currentCached)) {
            currentCached.container.style.display = DisplayStyle.None;
        }

        _currentScreenId = screen;
        _accordion?.SetActiveScreen(screen);

        if (_contentArea == null) return;

        if (_viewCache.TryGetValue(screen, out var cached)) {
            // Cache hit — show existing container, refresh and bind
            cached.container.style.display = DisplayStyle.Flex;
            _currentView = cached.view;
            _currentViewModel = cached.vm;
            RefreshCurrentViewModel();
            _currentView.Bind(_currentViewModel);
        } else {
            // Cache miss — create new view + container, initialize, store
            _currentViewModel = config.ViewModelFactory();
            _currentView = config.ViewFactory();

            var screenContainer = new VisualElement();
            screenContainer.name = "screen-container";
            screenContainer.AddToClassList("screen-container");
            _contentArea.Add(screenContainer);

            _currentView.Initialize(screenContainer);
            RefreshCurrentViewModel();
            _currentView.Bind(_currentViewModel);

            _viewCache[screen] = (_currentView, _currentViewModel, screenContainer);

            UIAnimator.FadeSlideIn(screenContainer);
        }

        OnScreenChanged?.Invoke(screen);
        _tooltipService?.Hide();
    }

    private void SetupAccordion() {
        if (_sidebar == null) return;
        var navTree = _registry.GetNavigationTree();
        _accordion = new AccordionSidebar();
        _accordion.Initialize(_sidebar, navTree);
        _accordion.OnScreenSelected += NavigateTo;
        _accordion.RegisterKeyboardEvents(_root);
    }

    private void ToggleSidebarCollapse() {
        if (_accordion == null) return;
        _accordion.SetCollapsed(!_accordion.IsCollapsed);
        if (_sidebarCollapseBtn != null) {
            _sidebarCollapseBtn.text = _accordion.IsCollapsed ? "»" : "«";
        }
    }

    private void UpdateInboxBadge(int count) {
        _accordion?.SetBadge("dashboard-inbox", count);
    }

    public void ShowModal(IGameView modalView, IViewModel modalVM) {
        if (_modalOverlay == null || _modalContent == null) return;

        DisposeCurrentModal();

        _currentModalView = modalView;
        _currentModalViewModel = modalVM;

        // Subscribe to action events on known modal view models
        if (modalVM is LoanApplicationViewModel loanVM) {
            Action<int, int> onTakeLoan = (amount, durationMonths) => {
                QueueCommand(new TakeLoanCommand(CurrentTick, amount, durationMonths));
                DismissModal();
                RefreshAll();
            };
            loanVM.OnTakeLoan += onTakeLoan;
            loanVM.SetLoanReadModel(GameController?.LoanReadModel);
            loanVM.OnDismiss += DismissModal;
            _modalCleanup = () => {
                loanVM.OnTakeLoan -= onTakeLoan;
                loanVM.OnDismiss -= DismissModal;
            };
        }
        bool isInspector = modalVM is EmployeeDetailModalViewModel
                        || modalVM is CandidateDetailModalViewModel;
        bool isLarge = !isInspector && modalVM is CompetitorProfileViewModel;
        _modalContent.EnableInClassList("modal-content--inspector", isInspector);
        _modalContent.EnableInClassList("modal-content--large", isLarge);

        // Inject UXML asset into views that require it
        if (_currentModalView is EmployeeDetailModalView empDetailView && employeeDetailAsset != null)
            empDetailView.SetAsset(employeeDetailAsset);
        if (_currentModalView is CandidateDetailModalView candDetailView && candidateDetailAsset != null)
            candDetailView.SetAsset(candidateDetailAsset);

        _modalContent.Clear();
        _currentModalView.Initialize(_modalContent);

        if (_currentModalViewModel != null) {
            var snapshot = BuildSnapshot();
            if (snapshot != null) {
                _currentModalViewModel.Refresh(snapshot);
            }
            _currentModalView.Bind(_currentModalViewModel);
        }

        _modalOverlay.RemoveFromClassList("hidden");
        UIAnimator.ModalOpen(_modalBackdrop, _modalContent);
    }

    public void DismissModal() {
        UIAnimator.ModalClose(_modalBackdrop, _modalContent, () => {
            DisposeCurrentModal();
            if (_modalOverlay != null) {
                _modalOverlay.AddToClassList("hidden");
            }
            if (_modalContent != null) {
                _modalContent.Clear();
            }
        });
    }

    public void ShowToast(string message, ToastType type) {
        if (_toastContainer == null) return;

        var toast = new VisualElement();
        toast.AddToClassList("toast");
        toast.AddToClassList("toast--" + type.ToString().ToLowerInvariant());

        var label = new Label(message);
        label.AddToClassList("toast__message");
        toast.Add(label);

        _toastContainer.Add(toast);
        _activeToasts.Add(toast);

        UIAnimator.ToastIn(toast);

        toast.schedule.Execute(() => {
            UIAnimator.ToastOut(toast, () => {
                if (_toastContainer != null && toast.parent == _toastContainer) {
                    _toastContainer.Remove(toast);
                }
                _activeToasts.Remove(toast);
            });
        }).ExecuteLater(3000);
    }

    public void QueueCommand(ICommand command) {
        if (_gameController != null) {
            _gameController.QueueCommand(command);
        }
    }

    // ICommandDispatcher
    void ICommandDispatcher.Dispatch(ICommand command) => QueueCommand(command);
    int ICommandDispatcher.CurrentTick => CurrentTick;

    // IModalPresenter
    void IModalPresenter.ShowModal(IGameView view, IViewModel viewModel) => ShowModal(view, viewModel);
    void IModalPresenter.DismissModal() => DismissModal();

    void IModalPresenter.OpenCompetitorProfile(CompetitorId competitorId) {
        var snapshot = BuildSnapshot();
        var vm = new CompetitorProfileViewModel();
        vm.SetId(competitorId);
        if (snapshot != null) vm.Refresh(competitorId, snapshot);
        ShowModal(new CompetitorProfileView(this, this), vm);
    }

    void IModalPresenter.OpenProductDetail(ProductId productId) {
        var snapshot = BuildSnapshot();
        var vm = new ProductDetailViewModel();
        if (snapshot != null) vm.Refresh(productId, snapshot.ProductStateRef, snapshot.CompetitorState, snapshot.MarketStateRef);
        ShowModal(new ProductDetailView(this, this), vm);
    }

    void IModalPresenter.OpenRenewalModal(EmployeeId? autoExpandId) {
        var vm = new RenewalViewModel();
        if (autoExpandId.HasValue) vm.SetAutoExpand(autoExpandId.Value);
        var snapshot = BuildSnapshot();
        if (snapshot != null) vm.Refresh(snapshot);
        ShowModal(new RenewalView(this, this), vm);
    }

    void IModalPresenter.ShowCandidateDetailModal(int candidateId, bool showCounterOffer) {
        var vm = new CandidateDetailModalViewModel();
        vm.SetCandidateId(candidateId);
        var view = new CandidateDetailModalView(this, this);
        ShowModal(view, vm);
        if (showCounterOffer && vm.HasPendingCounter) {
            view.ShowCounterOfferView();
        }
    }

    public int CurrentTick => _gameController != null ? _gameController.CurrentTick : 0;

    // INavigationService
    void INavigationService.NavigateTo(ScreenId screenId) => NavigateTo(screenId);

    void INavigationService.NavigateTo(ScreenId screenId, int tabHint) {
        NavigateTo(screenId);
        if (tabHint >= 0 && _currentView is ITabNavigable tabView) {
            tabView.SwitchToTab(tabHint);
        }
    }

    // --- Private helpers ---

    private void RefreshTopBar() {
        var snapshot = BuildSnapshot();
        if (snapshot == null) return;

        float previousMoney = _lastMoneyValue;
        _topBarViewModel.Refresh(snapshot);

        if (_companyNameLabel != null) _companyNameLabel.text = _topBarViewModel.CompanyName;
        if (_dateLabel != null) _dateLabel.text = _topBarViewModel.DateDisplay;
        if (_reputationSubtitle != null) {
            _reputationSubtitle.text = "★ " + _topBarViewModel.ReputationTier;
            string newRepClass = _topBarViewModel.ReputationColourClass;
            if (newRepClass != _lastReputationClass) {
                if (!string.IsNullOrEmpty(_lastReputationClass))
                    _reputationSubtitle.RemoveFromClassList(_lastReputationClass);
                _reputationSubtitle.AddToClassList(newRepClass);
                _lastReputationClass = newRepClass;
            }
        }

        // Money: extract numeric value for animation
        float newMoneyValue = snapshot.Money;
        if (_moneyLabel != null && !Mathf.Approximately(newMoneyValue, previousMoney)) {
            // Delta floater
            float delta = newMoneyValue - previousMoney;
            bool isPositive = delta > 0;
            string deltaText = isPositive
                ? "+" + UIFormatting.FormatMoney((long)delta)
                : UIFormatting.FormatMoney((long)delta);
            DeltaLabel.Show(_deltaContainer, deltaText, isPositive);

            // Counter roll-up
            UIAnimator.CounterRollUp(
                _moneyLabel, previousMoney, newMoneyValue,
                "{0:N0}", 0.4f);

            // Color flash
            Color flashColor = isPositive
                ? new Color(0.204f, 0.827f, 0.600f, 1f)  // accent-success #34D399
                : new Color(0.984f, 0.443f, 0.522f, 1f);  // accent-danger  #FB7185
            UIAnimator.MoneyFlash(_moneyLabel, flashColor, _moneyBaseColor);

            _lastMoneyValue = newMoneyValue;
        } else if (_moneyLabel != null) {
            _moneyLabel.text = _topBarViewModel.MoneyDisplay;
            _lastMoneyValue = newMoneyValue;
        }

        // Continue button + advancing module class
        bool isAdvancing = _topBarViewModel.IsAdvancing;
        if (_continueButton != null) {
            _continueButton.text = isAdvancing ? "Pause" : "Continue";
        }
        if (_moduleControls != null) {
            if (isAdvancing) {
                _moduleControls.AddToClassList("top-bar__module--advancing");
            } else {
                _moduleControls.RemoveFromClassList("top-bar__module--advancing");
            }
        }
        if (_moduleFinance != null) {
            _moduleFinance.RemoveFromClassList("finance--stable");
            _moduleFinance.RemoveFromClassList("finance--tight");
            _moduleFinance.RemoveFromClassList("finance--critical");
            _moduleFinance.AddToClassList(_topBarViewModel.FinanceHealthClass);
        }
        if (_netIncomeLabel != null) {
            _netIncomeLabel.text = _topBarViewModel.NetIncomeDisplay;
            string incomeClass = _topBarViewModel.IsNetPositive ? "text-success" : "text-danger";
            _netIncomeLabel.RemoveFromClassList("text-success");
            _netIncomeLabel.RemoveFromClassList("text-danger");
            _netIncomeLabel.AddToClassList(incomeClass);
        }
        if (_moneyLabel != null) {
            string moneyClass = _topBarViewModel.FinanceHealthClass switch {
                "finance--stable"   => "text-success",
                "finance--tight"    => "text-warning",
                "finance--critical" => "text-danger",
                _                   => "text-success"
            };
            if (moneyClass != _lastMoneyColourClass) {
                if (!string.IsNullOrEmpty(_lastMoneyColourClass))
                    _moneyLabel.RemoveFromClassList(_lastMoneyColourClass);
                _moneyLabel.AddToClassList(moneyClass);
                _lastMoneyColourClass = moneyClass;
            }
        }

        // Inbox badge
        if (_gameController != null) {
            UpdateInboxBadge(_gameController.InboxSystem?.CriticalUnreadCount ?? 0);
        }
    }

    private void RefreshCurrentViewModel() {
        if (_currentViewModel == null) return;
        var snapshot = BuildSnapshot();
        if (snapshot != null) {
            if (_currentViewModel is ProductsBrowserViewModel browserVM && _gameController != null)
                browserVM.SetTemplates(_gameController.ProductTemplates);
            _currentViewModel.Refresh(snapshot);
        }
    }

    private void RefreshAll() {
        _topBarDirty = true;
        _viewDirty = true;
        _modalDirty = true;
    }

    private void MarkViewDirtyFor<T>() {
        _topBarDirty = true;
        var type = typeof(T);
        if (!_eventRelevance.TryGetValue(type, out var screens)) {
            _viewDirty = true;
            return;
        }
        if (screens.Contains(_currentScreenId)) _viewDirty = true;
    }

    private void LateUpdate() {
        if (_topBarDirty) {
            _topBarDirty = false;
            RefreshTopBar();
        }
        if (_viewDirty) {
            _viewDirty = false;
            RefreshCurrentViewModel();
            _currentView?.Bind(_currentViewModel);
        }
        if (_modalDirty) {
            _modalDirty = false;
            RefreshCurrentModal();
        }
    }

    private void RefreshCurrentModal() {
        if (_currentModalView == null || _currentModalViewModel == null) return;
        var snapshot = BuildSnapshot();
        if (snapshot == null) return;
        _currentModalViewModel.Refresh(snapshot);
        _currentModalView.Bind(_currentModalViewModel);
    }

    private GameStateSnapshot BuildSnapshot() {
        if (_gameController == null) return null;
        if (_cachedSnapshot == null) _cachedSnapshot = new GameStateSnapshot();
        _cachedSnapshot.PopulateFrom(
            _gameController.GetGameState(),
            _gameController.LoanReadModel,
            _gameController.InterviewSystem,
            _gameController.NegotiationSystem,
            _gameController.HRSystem,
            _gameController.RecruitmentReputationSystem,
            _gameController,
            _gameController.TeamSystem,
            _gameController.ContractSystem,
            _gameController.AbilitySystem,
            _gameController.Tuning,
            _gameController.ProductSystem,
            _gameController.MarketSystem,
            _gameController.ReputationSystem,
            _gameController.GenerationSystem
        );
        _cachedSnapshot.SetTaxSystem(_gameController.TaxSystem);
        _cachedSnapshot.SetTeamChemistrySystem(_gameController.TeamChemistrySystem);
        _cachedSnapshot.SetFatigueSystem(_gameController.FatigueSystem);
        return _cachedSnapshot;
    }

    public IReadOnlyGameState GetCurrentSnapshot() => BuildSnapshot();

    private void DisposeCurrentView() {
        if (_currentView != null) {
            _currentView.Dispose();
            _currentView = null;
        }
        _currentViewModel = null;
    }

    private static Dictionary<Type, HashSet<ScreenId>> BuildEventRelevanceMap() {
        var map = new Dictionary<Type, HashSet<ScreenId>>();

        map[typeof(EmployeeCountChangedEvent)] = new HashSet<ScreenId> {
            ScreenId.HREmployees, ScreenId.HRPortalLanding
        };
        map[typeof(SkillImprovedEvent)] = new HashSet<ScreenId> {
            ScreenId.HREmployees
        };
        map[typeof(EmployeeDecayEvent)] = new HashSet<ScreenId> {
            ScreenId.HREmployees
        };
        map[typeof(EmployeeRetiredEvent)] = new HashSet<ScreenId> {
            ScreenId.HREmployees, ScreenId.HRPortalLanding
        };
        map[typeof(ContractAcceptedEvent)] = new HashSet<ScreenId> {
            ScreenId.ProductionContracts, ScreenId.FinanceOverview, ScreenId.DashboardCalendar
        };
        map[typeof(ContractAssignedEvent)] = new HashSet<ScreenId> {
            ScreenId.ProductionContracts, ScreenId.HRTeams
        };
        map[typeof(ContractProgressUpdatedEvent)] = new HashSet<ScreenId> {
            ScreenId.ProductionContracts, ScreenId.ProductionProductsInDev
        };
        map[typeof(ContractCompletedEvent)] = new HashSet<ScreenId> {
            ScreenId.ProductionContracts, ScreenId.FinanceOverview, ScreenId.DashboardCalendar
        };
        map[typeof(ContractFailedEvent)] = new HashSet<ScreenId> {
            ScreenId.ProductionContracts, ScreenId.FinanceOverview, ScreenId.DashboardCalendar
        };
        map[typeof(ContractExpiredEvent)] = new HashSet<ScreenId> {
            ScreenId.ProductionContracts, ScreenId.DashboardCalendar
        };
        map[typeof(PoolRerolledEvent)] = new HashSet<ScreenId> {
            ScreenId.ProductionContracts
        };
        map[typeof(FinanceChangedEvent)] = new HashSet<ScreenId> {
            ScreenId.FinanceOverview
        };
        map[typeof(SalaryPaidEvent)] = new HashSet<ScreenId> {
            ScreenId.FinanceOverview, ScreenId.HREmployees
        };
        map[typeof(TeamCreatedEvent)] = new HashSet<ScreenId> {
            ScreenId.HRTeams, ScreenId.HRPortalLanding
        };
        map[typeof(TeamDeletedEvent)] = new HashSet<ScreenId> {
            ScreenId.HRTeams, ScreenId.HRPortalLanding
        };
        map[typeof(CrunchModeChangedEvent)] = new HashSet<ScreenId> {
            ScreenId.HRTeams
        };
        map[typeof(EmployeeAssignedToTeamEvent)] = new HashSet<ScreenId> {
            ScreenId.HRTeams, ScreenId.HREmployees
        };
        map[typeof(EmployeeRemovedFromTeamEvent)] = new HashSet<ScreenId> {
            ScreenId.HRTeams, ScreenId.HREmployees
        };
        map[typeof(ReputationChangedEvent)] = new HashSet<ScreenId> {
            ScreenId.DashboardReputation
        };
        map[typeof(AutoActionTakenEvent)] = new HashSet<ScreenId> {
            ScreenId.DashboardInbox
        };
        map[typeof(NewsArticleAddedEvent)] = new HashSet<ScreenId> {
            ScreenId.DashboardInbox, ScreenId.ProductionProductsInDev, ScreenId.ProductionProductsLive
        };
        map[typeof(CandidatesGeneratedEvent)] = new HashSet<ScreenId> {
            ScreenId.HRCandidates, ScreenId.HRPortalLanding
        };
        map[typeof(CandidateDeclinedEvent)] = new HashSet<ScreenId> {
            ScreenId.HRCandidates, ScreenId.HRPortalLanding
        };
        map[typeof(CandidateWithdrewEvent)] = new HashSet<ScreenId> {
            ScreenId.HRCandidates, ScreenId.HRPortalLanding
        };
        map[typeof(CandidateHardRejectedEvent)] = new HashSet<ScreenId> {
            ScreenId.HRCandidates
        };
        map[typeof(InterviewThresholdEvent)] = new HashSet<ScreenId> {
            ScreenId.HRCandidates
        };
        map[typeof(InterviewStartedEvent)] = new HashSet<ScreenId> {
            ScreenId.HRCandidates
        };
        map[typeof(TeamAssignedToProductEvent)] = new HashSet<ScreenId> {
            ScreenId.ProductionProductsInDev, ScreenId.HRTeams
        };
        map[typeof(MarketingCampaignChangedEvent)] = new HashSet<ScreenId> {
            ScreenId.ProductionProductsInDev, ScreenId.ProductionProductsLive
        };
        map[typeof(ProductCreatedEvent)] = new HashSet<ScreenId> {
            ScreenId.ProductionProductsInDev, ScreenId.DashboardCalendar
        };
        map[typeof(ProductProgressUpdatedEvent)] = new HashSet<ScreenId> {
            ScreenId.ProductionProductsInDev
        };
        map[typeof(ProductPhaseCompletedEvent)] = new HashSet<ScreenId> {
            ScreenId.ProductionProductsInDev
        };
        map[typeof(ProductPhaseIterationStartedEvent)] = new HashSet<ScreenId> {
            ScreenId.ProductionProductsInDev
        };
        map[typeof(ProductPhaseIterationCompletedEvent)] = new HashSet<ScreenId> {
            ScreenId.ProductionProductsInDev
        };

        // Competitor / stock / disruption events
        map[typeof(CompetitorBankruptEvent)] = new HashSet<ScreenId> {
            ScreenId.CompetitorsList, ScreenId.CompetitorsIndustryOverview, ScreenId.DashboardInbox
        };
        map[typeof(CompetitorProductLaunchedEvent)] = new HashSet<ScreenId> {
            ScreenId.CompetitorsList, ScreenId.MarketProductsBrowser, ScreenId.MarketOverview, ScreenId.DashboardCalendar
        };
        map[typeof(CompetitorDevStartedEvent)] = new HashSet<ScreenId> {
            ScreenId.DashboardCalendar, ScreenId.CompetitorsList
        };
        map[typeof(CompanyAcquiredEvent)] = new HashSet<ScreenId> {
            ScreenId.CompetitorsList, ScreenId.FinanceMyInvestments, ScreenId.DashboardInbox
        };
        map[typeof(DividendPaidEvent)] = new HashSet<ScreenId> {
            ScreenId.FinanceOverview, ScreenId.FinanceMyInvestments, ScreenId.DashboardInbox
        };
        map[typeof(StockPurchasedEvent)] = new HashSet<ScreenId> {
            ScreenId.FinanceMyInvestments, ScreenId.FinanceStockInvestments
        };
        map[typeof(ProductCrisisEvent)] = new HashSet<ScreenId> {
            ScreenId.ProductionProductsLive, ScreenId.MarketProductsBrowser, ScreenId.DashboardInbox
        };
        map[typeof(MinorDisruptionStartedEvent)] = new HashSet<ScreenId> {
            ScreenId.MarketOverview, ScreenId.DashboardInbox, ScreenId.DashboardCalendar
        };
        map[typeof(MajorDisruptionStartedEvent)] = new HashSet<ScreenId> {
            ScreenId.MarketOverview, ScreenId.DashboardInbox, ScreenId.DashboardCalendar
        };
        map[typeof(ShowdownResolvedEvent)] = new HashSet<ScreenId> {
            ScreenId.MarketOverview, ScreenId.MarketProductsBrowser, ScreenId.DashboardInbox
        };
        map[typeof(ProductShippedEvent)] = new HashSet<ScreenId> {
            ScreenId.ProductionProductsLive, ScreenId.MarketProductsBrowser, ScreenId.DashboardCalendar
        };
        map[typeof(ProductShipWarningEvent)] = new HashSet<ScreenId> {
            ScreenId.DashboardInbox
        };
        map[typeof(MonthlyNewsReportEvent)] = new HashSet<ScreenId> {
            ScreenId.DashboardInbox
        };
        map[typeof(PlayerAcquiredEvent)] = new HashSet<ScreenId> {
            ScreenId.DashboardInbox
        };

        // Tax events
        map[typeof(TaxReminderEvent)] = new HashSet<ScreenId> {
            ScreenId.FinanceOverview, ScreenId.DashboardInbox
        };
        map[typeof(TaxDueEvent)] = new HashSet<ScreenId> {
            ScreenId.FinanceOverview, ScreenId.DashboardInbox
        };
        map[typeof(TaxOverdueEvent)] = new HashSet<ScreenId> {
            ScreenId.FinanceOverview, ScreenId.DashboardInbox
        };
        map[typeof(TaxPaidEvent)] = new HashSet<ScreenId> {
            ScreenId.FinanceOverview
        };
        map[typeof(TaxBankruptcyEvent)] = new HashSet<ScreenId> {
            ScreenId.FinanceOverview, ScreenId.DashboardInbox
        };

        // Negotiation events
        map[typeof(CounterOfferReceivedEvent)] = new HashSet<ScreenId> {
            ScreenId.HRCandidates, ScreenId.DashboardInbox
        };
        map[typeof(CandidateLostPatienceEvent)] = new HashSet<ScreenId> {
            ScreenId.HRCandidates, ScreenId.HRPortalLanding, ScreenId.DashboardInbox
        };
        map[typeof(PatienceLowEvent)] = new HashSet<ScreenId> {
            ScreenId.HRCandidates, ScreenId.HREmployees, ScreenId.DashboardInbox
        };
        map[typeof(CounterOfferExpiredEvent)] = new HashSet<ScreenId> {
            ScreenId.HRCandidates, ScreenId.DashboardInbox
        };
        map[typeof(EmployeeFrustratedEvent)] = new HashSet<ScreenId> {
            ScreenId.HREmployees, ScreenId.DashboardInbox
        };
        map[typeof(EmployeeCooldownExpiredEvent)] = new HashSet<ScreenId> {
            ScreenId.HREmployees, ScreenId.DashboardInbox
        };

        return map;
    }

    private void DisposeCurrentModal() {
        _modalCleanup?.Invoke();
        _modalCleanup = null;
        if (_currentModalView != null) {
            _currentModalView.Dispose();
            _currentModalView = null;
        }
        _currentModalViewModel = null;
        if (_modalContent != null) {
            _modalContent.RemoveFromClassList("modal-content--large");
            _modalContent.RemoveFromClassList("modal-content--inspector");
        }
    }

    private void OnContinueClicked() {
        if (_gameController == null) return;
        if (_gameController.IsAdvancing) {
            _gameController.StopAdvance();
        } else {
            _gameController.StartAdvance();
        }
        RefreshTopBar();
    }

    private void InitializePauseMenu() {
        if (_root == null) return;

        _pauseMenuViewModel = new PauseMenuViewModel();
        _pauseMenuView = new PauseMenuView(this, this, _gameController);

        var pauseContainer = new VisualElement();
        pauseContainer.name = "pause-overlay";
        _root.Add(pauseContainer);

        _pauseMenuView.Initialize(pauseContainer);

        var snapshot = BuildSnapshot();
        if (snapshot != null) {
            _pauseMenuViewModel.Refresh(snapshot);
        }
        _pauseMenuView.Bind(_pauseMenuViewModel);

        if (_topBar != null) {
            _settingsButton = new Button { text = "⚙" };
            _settingsButton.AddToClassList("btn-settings-icon");
            _settingsButton.clicked += OnSettingsButtonClicked;
            _topBar.Add(_settingsButton);
        }
    }

    private void SetupEscapeKey() {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        _escapeAction = new InputAction(type: InputActionType.Button);
        _escapeAction.AddBinding("<Keyboard>/escape");
        _escapeAction.performed += OnEscapePerformed;
        _escapeAction.Enable();
    }

    private void OnEscapePerformed(InputAction.CallbackContext ctx) {
        if (_pauseMenuView == null) return;

        if (_pauseMenuView.IsVisible) {
            _pauseMenuView.Hide();
        } else if (_currentModalView != null) {
            DismissModal();
        } else {
            _pauseMenuView.Show();
            var snapshot = BuildSnapshot();
            if (snapshot != null) {
                _pauseMenuViewModel.Refresh(snapshot);
                _pauseMenuView.Bind(_pauseMenuViewModel);
            }
        }
    }

    private void OnSettingsButtonClicked() {
        if (_pauseMenuView == null) return;
        _pauseMenuView.Show();
        var snapshot = BuildSnapshot();
        if (snapshot != null) {
            _pauseMenuViewModel.Refresh(snapshot);
            _pauseMenuView.Bind(_pauseMenuViewModel);
        }
    }

    // --- Event subscriptions ---

    private void SubscribeToEvents() {
        if (_eventBus == null) return;

        // Time / advance
        _eventBus.Subscribe<AdvanceStartedEvent>(OnAdvanceStarted);
        _eventBus.Subscribe<AdvancePausedEvent>(OnAdvancePaused);
        _eventBus.Subscribe<AdvanceTickedEvent>(OnAdvanceTicked);
        _eventBus.Subscribe<DayChangedEvent>(OnDayChanged);

        // Finance
        _eventBus.Subscribe<FinanceChangedEvent>(OnFinanceChanged);
        _eventBus.Subscribe<SalaryPaidEvent>(OnSalaryPaid);

        // Contracts
        _eventBus.Subscribe<ContractAcceptedEvent>(OnContractAccepted);
        _eventBus.Subscribe<ContractAssignedEvent>(OnContractAssigned);
        _eventBus.Subscribe<ContractCompletedEvent>(OnContractCompleted);
        _eventBus.Subscribe<ContractFailedEvent>(OnContractFailed);
        _eventBus.Subscribe<ContractExpiredEvent>(OnContractExpired);
        _eventBus.Subscribe<ContractProgressUpdatedEvent>(OnContractProgressUpdated);
        _eventBus.Subscribe<PoolRerolledEvent>(OnPoolRerolled);

        // Employees
        _eventBus.Subscribe<EmployeeCountChangedEvent>(OnEmployeeCountChanged);
        _eventBus.Subscribe<SkillImprovedEvent>(OnSkillImproved);

        // Teams
        _eventBus.Subscribe<TeamCreatedEvent>(OnTeamCreated);
        _eventBus.Subscribe<TeamDeletedEvent>(OnTeamDeleted);
        _eventBus.Subscribe<EmployeeAssignedToTeamEvent>(OnEmployeeAssignedToTeam);
        _eventBus.Subscribe<EmployeeRemovedFromTeamEvent>(OnEmployeeRemovedFromTeam);
        _eventBus.Subscribe<CrunchModeChangedEvent>(OnCrunchModeChanged);

        // HR / recruitment
        _eventBus.Subscribe<CandidatesGeneratedEvent>(OnCandidatesGenerated);
        _eventBus.Subscribe<CandidateDeclinedEvent>(OnCandidateDeclined);
        _eventBus.Subscribe<CandidateWithdrewEvent>(OnCandidateWithdrew);
        _eventBus.Subscribe<CandidateHardRejectedEvent>(OnCandidateHardRejected);
        _eventBus.Subscribe<InterviewThresholdEvent>(OnInterviewThresholdReached);
        _eventBus.Subscribe<InterviewStartedEvent>(OnInterviewStarted);

        // Employee aging
        _eventBus.Subscribe<EmployeeDecayEvent>(OnEmployeeDecay);
        _eventBus.Subscribe<EmployeeRetiredEvent>(OnEmployeeRetired);

        // Reputation
        _eventBus.Subscribe<ReputationChangedEvent>(OnReputationChanged);

        // Inbox
        _eventBus.Subscribe<AutoActionTakenEvent>(OnAutoActionTaken);
        _eventBus.Subscribe<InboxChangedEvent>(OnInboxChanged);

        // Marketing / Hype
        _eventBus.Subscribe<NewsArticleAddedEvent>(OnNewsArticleAdded);

        // Products
        _eventBus.Subscribe<TeamAssignedToProductEvent>(OnTeamAssignedToProduct);
        _eventBus.Subscribe<MarketingCampaignChangedEvent>(OnMarketingCampaignChanged);
        _eventBus.Subscribe<ProductCreatedEvent>(OnProductCreated);

        // Competitor / stock / disruption / market
        _eventBus.Subscribe<CompetitorBankruptEvent>(OnCompetitorBankrupt);
        _eventBus.Subscribe<CompetitorProductLaunchedEvent>(OnCompetitorProductLaunched);
        _eventBus.Subscribe<CompetitorDevStartedEvent>(OnCompetitorDevStarted);
        _eventBus.Subscribe<CompanyAcquiredEvent>(OnCompanyAcquired);
        _eventBus.Subscribe<DividendPaidEvent>(OnDividendPaid);
        _eventBus.Subscribe<StockPurchasedEvent>(OnStockPurchased);
        _eventBus.Subscribe<ProductCrisisEvent>(OnProductCrisis);
        _eventBus.Subscribe<MinorDisruptionStartedEvent>(OnMinorDisruptionStarted);
        _eventBus.Subscribe<MajorDisruptionStartedEvent>(OnMajorDisruptionStarted);
        _eventBus.Subscribe<ShowdownResolvedEvent>(OnShowdownResolved);
        _eventBus.Subscribe<ProductShippedEvent>(OnProductShipped);
        _eventBus.Subscribe<ProductShipWarningEvent>(OnProductShipWarning);
        _eventBus.Subscribe<MonthlyNewsReportEvent>(OnMonthlyNewsReport);
        _eventBus.Subscribe<PlayerAcquiredEvent>(OnPlayerAcquired);
    }

    private void UnsubscribeFromEvents() {
        // GameEventBus doesn't support targeted unsubscribe by delegate identity.
        // Events are GC'd when the bus is cleared on destroy.
    }

    // Tick-only — top bar only, avoids full view rebuild every tick
    private void OnAdvanceStarted(AdvanceStartedEvent evt) { _topBarDirty = true; }
    private void OnAdvancePaused(AdvancePausedEvent evt)   { RefreshAll(); }
    private void OnAdvanceTicked(AdvanceTickedEvent evt)   { _topBarDirty = true; }
    private void OnDayChanged(DayChangedEvent evt)         { RefreshAll(); }
    private void OnInboxChanged(InboxChangedEvent evt)     { _topBarDirty = true; }
    private void OnFinanceChanged(FinanceChangedEvent evt) { MarkViewDirtyFor<FinanceChangedEvent>(); }

    private void OnSalaryPaid(SalaryPaidEvent evt)                           { MarkViewDirtyFor<SalaryPaidEvent>(); }
    private void OnContractAccepted(ContractAcceptedEvent evt)               { MarkViewDirtyFor<ContractAcceptedEvent>(); }
    private void OnContractAssigned(ContractAssignedEvent evt)               { MarkViewDirtyFor<ContractAssignedEvent>(); }
    private void OnContractCompleted(ContractCompletedEvent evt)             { MarkViewDirtyFor<ContractCompletedEvent>(); }
    private void OnContractFailed(ContractFailedEvent evt)                   { MarkViewDirtyFor<ContractFailedEvent>(); }
    private void OnContractExpired(ContractExpiredEvent evt)                 { MarkViewDirtyFor<ContractExpiredEvent>(); }
    private void OnContractProgressUpdated(ContractProgressUpdatedEvent evt) { MarkViewDirtyFor<ContractProgressUpdatedEvent>(); }
    private void OnPoolRerolled(PoolRerolledEvent evt)                       { MarkViewDirtyFor<PoolRerolledEvent>(); }
    private void OnEmployeeCountChanged(EmployeeCountChangedEvent evt)       { MarkViewDirtyFor<EmployeeCountChangedEvent>(); }
    private void OnSkillImproved(SkillImprovedEvent evt)                     { MarkViewDirtyFor<SkillImprovedEvent>(); }
    private void OnTeamCreated(TeamCreatedEvent evt)                         { MarkViewDirtyFor<TeamCreatedEvent>(); }
    private void OnTeamDeleted(TeamDeletedEvent evt)                         { MarkViewDirtyFor<TeamDeletedEvent>(); }
    private void OnEmployeeAssignedToTeam(EmployeeAssignedToTeamEvent evt)   { MarkViewDirtyFor<EmployeeAssignedToTeamEvent>(); }
    private void OnEmployeeRemovedFromTeam(EmployeeRemovedFromTeamEvent evt) { MarkViewDirtyFor<EmployeeRemovedFromTeamEvent>(); }
    private void OnCrunchModeChanged(CrunchModeChangedEvent evt)             { MarkViewDirtyFor<CrunchModeChangedEvent>(); }
    private void OnCandidatesGenerated(CandidatesGeneratedEvent evt)         { MarkViewDirtyFor<CandidatesGeneratedEvent>(); }
    private void OnCandidateDeclined(CandidateDeclinedEvent evt)             { MarkViewDirtyFor<CandidateDeclinedEvent>(); }
    private void OnCandidateWithdrew(CandidateWithdrewEvent evt)             { MarkViewDirtyFor<CandidateWithdrewEvent>(); }
    private void OnCandidateHardRejected(CandidateHardRejectedEvent evt)     { MarkViewDirtyFor<CandidateHardRejectedEvent>(); }
    private void OnInterviewThresholdReached(InterviewThresholdEvent evt)       { MarkViewDirtyFor<InterviewThresholdEvent>(); }
    private void OnInterviewStarted(InterviewStartedEvent evt)               { MarkViewDirtyFor<InterviewStartedEvent>(); }
    private void OnEmployeeDecay(EmployeeDecayEvent evt)                     { MarkViewDirtyFor<EmployeeDecayEvent>(); }
    private void OnEmployeeRetired(EmployeeRetiredEvent evt)                 { MarkViewDirtyFor<EmployeeRetiredEvent>(); }
    private void OnReputationChanged(ReputationChangedEvent evt)             { MarkViewDirtyFor<ReputationChangedEvent>(); }
    private void OnAutoActionTaken(AutoActionTakenEvent evt)                 { MarkViewDirtyFor<AutoActionTakenEvent>(); }
    private void OnNewsArticleAdded(NewsArticleAddedEvent evt)               { MarkViewDirtyFor<NewsArticleAddedEvent>(); }
    private void OnTeamAssignedToProduct(TeamAssignedToProductEvent evt)     { MarkViewDirtyFor<TeamAssignedToProductEvent>(); }
    private void OnMarketingCampaignChanged(MarketingCampaignChangedEvent evt) { MarkViewDirtyFor<MarketingCampaignChangedEvent>(); }
    private void OnProductCreated(ProductCreatedEvent evt)                   { MarkViewDirtyFor<ProductCreatedEvent>(); }

    private void OnCompetitorBankrupt(CompetitorBankruptEvent evt)           { MarkViewDirtyFor<CompetitorBankruptEvent>(); }
    private void OnCompetitorProductLaunched(CompetitorProductLaunchedEvent evt) { MarkViewDirtyFor<CompetitorProductLaunchedEvent>(); }
    private void OnCompetitorDevStarted(CompetitorDevStartedEvent evt)       { MarkViewDirtyFor<CompetitorDevStartedEvent>(); }
    private void OnDividendPaid(DividendPaidEvent evt)                       { MarkViewDirtyFor<DividendPaidEvent>(); }
    private void OnStockPurchased(StockPurchasedEvent evt)                   { MarkViewDirtyFor<StockPurchasedEvent>(); }
    private void OnMinorDisruptionStarted(MinorDisruptionStartedEvent evt)   { MarkViewDirtyFor<MinorDisruptionStartedEvent>(); }
    private void OnMonthlyNewsReport(MonthlyNewsReportEvent evt) {
        MarkViewDirtyFor<MonthlyNewsReportEvent>();
    }

    private void OnCompanyAcquired(CompanyAcquiredEvent evt) {
        MarkViewDirtyFor<CompanyAcquiredEvent>();
        if (_gameController == null) return;
        var gs = _gameController.GetGameState();
        if (gs == null) return;
        var vm = new AcquisitionViewModel();
        vm.Refresh(evt.Target, gs.competitorState, gs.productState, gs.stockState, gs.financeState);
        ShowModal(new AcquisitionView(this, this), vm);
    }

    private void OnProductCrisis(ProductCrisisEvent evt) {
        MarkViewDirtyFor<ProductCrisisEvent>();
    }

    private void OnMajorDisruptionStarted(MajorDisruptionStartedEvent evt) {
        MarkViewDirtyFor<MajorDisruptionStartedEvent>();
        if (_gameController == null) return;
        var gs = _gameController.GetGameState();
        if (gs == null) return;
        var disruptions = _gameController.DisruptionSystem?.GetActiveDisruptions();
        ActiveDisruption disruption = null;
        if (disruptions != null)
        {
            int count = disruptions.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                if (disruptions[i].IsMajor) { disruption = disruptions[i]; break; }
            }
        }
        var vm = new DisruptionModalViewModel();
        vm.Refresh(disruption, _gameController.CurrentTick);
        ShowModal(new DisruptionModalView(this), vm);
    }

    private void OnShowdownResolved(ShowdownResolvedEvent evt) {
        MarkViewDirtyFor<ShowdownResolvedEvent>();
        if (_gameController == null) return;
        var gs = _gameController.GetGameState();
        if (gs == null) return;
        bool playerOrCompetitorInvolved = false;
        if (gs.productState?.shippedProducts != null) {
            gs.productState.shippedProducts.TryGetValue(evt.Result.WinnerId, out var w);
            gs.productState.shippedProducts.TryGetValue(evt.Result.LoserId, out var l);
            playerOrCompetitorInvolved = (w != null && !w.IsCompetitorProduct) || (l != null && !l.IsCompetitorProduct);
        }
        if (!playerOrCompetitorInvolved) return;
        var vm = new ShowdownResultViewModel();
        bool playerWon = false;
        if (gs.productState?.shippedProducts != null)
        {
            gs.productState.shippedProducts.TryGetValue(evt.Result.WinnerId, out var winner);
            playerWon = winner != null && !winner.IsCompetitorProduct;
        }
        vm.Refresh(evt.Result.WinnerId, evt.Result.LoserId, gs.productState, gs.competitorState, playerWon);
        ShowModal(new ShowdownResultView(this), vm);
    }

    private void OnProductShipped(ProductShippedEvent evt) {
        MarkViewDirtyFor<ProductShippedEvent>();
        if (_gameController == null) return;
        var gs = _gameController.GetGameState();
        if (gs == null) return;
        if (gs.productState?.shippedProducts != null &&
            gs.productState.shippedProducts.TryGetValue(evt.ProductId, out var product) &&
            !product.IsCompetitorProduct) {
            var vm = new ProductReviewViewModel();
            vm.SetProduct(evt.ProductId);
            vm.RefreshFromProduct(product);
            ShowModal(new ProductReviewModalView(this), vm);
        }
    }

    private void OnProductShipWarning(ProductShipWarningEvent evt) {
        MarkViewDirtyFor<ProductShipWarningEvent>();
    }

    private void OnPlayerAcquired(PlayerAcquiredEvent evt) {
        MarkViewDirtyFor<PlayerAcquiredEvent>();
        if (_gameController == null) return;
        var gs = _gameController.GetGameState();
        if (gs == null) return;
        var vm = new GameOverViewModel();
        vm.Refresh(gs, gs.competitorState, "Your company was acquired");
        ShowModal(new GameOverView(this), vm);
    }
}
