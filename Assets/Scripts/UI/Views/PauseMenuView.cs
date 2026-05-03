using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class PauseMenuView : IGameView {
    private readonly ICommandDispatcher _dispatcher;
    private readonly IModalPresenter _modal;
    private readonly GameController _gameController;

    private VisualElement _pauseRoot;

    // Pause panel
    private VisualElement _pausePanel;
    private Button _btnResume;
    private Button _btnSaveGame;
    private Button _btnLoadGame;
    private Button _btnSettings;
    private Button _btnMainMenu;
    private Button _btnExit;

    // Save panel
    private VisualElement _savePanel;
    private VisualElement _saveSlotsContainer;
    private Button _btnSaveBack;
    private Label _slotsRemainingLabel;

    // Load panel
    private VisualElement _loadPanel;
    private VisualElement _manualSavesContainer;
    private VisualElement _autoSavesContainer;
    private Button _btnLoadBack;

    // Confirm dialog
    private VisualElement _confirmRoot;
    private Label _confirmMessage;
    private Button _btnConfirmYes;
    private Button _btnConfirmNo;
    private Label _btnConfirmAlt;
    private Button _btnConfirmAltAction;

    private Action _onConfirmYes;
    private Action _onConfirmNo;
    private Action _onConfirmAlt;

    // Save slot pools
    private readonly List<VisualElement> _saveSlotPool = new List<VisualElement>();
    private readonly List<VisualElement> _manualLoadSlotPool = new List<VisualElement>();
    private readonly List<VisualElement> _autoLoadSlotPool = new List<VisualElement>();

    private PauseMenuViewModel _vm;

    public bool IsVisible { get; private set; }

    public PauseMenuView(ICommandDispatcher dispatcher, IModalPresenter modal, GameController gameController) {
        _dispatcher = dispatcher;
        _modal = modal;
        _gameController = gameController;
    }

    public void Initialize(VisualElement root, UIServices services) {
        _pauseRoot = root;
        _pauseRoot.AddToClassList("pause-overlay");
        _pauseRoot.style.display = DisplayStyle.None;

        _pausePanel = BuildPausePanel();
        _pauseRoot.Add(_pausePanel);

        _savePanel = BuildSavePanel();
        _pauseRoot.Add(_savePanel);

        _loadPanel = BuildLoadPanel();
        _pauseRoot.Add(_loadPanel);

        _confirmRoot = BuildConfirmDialog();
        _pauseRoot.Add(_confirmRoot);

        HideAllPanels();
    }

    public void Bind(IViewModel viewModel) {
        _vm = viewModel as PauseMenuViewModel;
    }

    public void Dispose() {
        if (_btnResume != null)   _btnResume.clicked   -= OnResumeClicked;
        if (_btnSaveGame != null) _btnSaveGame.clicked -= OnSaveGameClicked;
        if (_btnLoadGame != null) _btnLoadGame.clicked -= OnLoadGameClicked;
        if (_btnMainMenu != null) _btnMainMenu.clicked -= OnMainMenuClicked;
        if (_btnExit != null)     _btnExit.clicked     -= OnExitClicked;
        if (_btnSaveBack != null) _btnSaveBack.clicked -= OnSaveBackClicked;
        if (_btnLoadBack != null) _btnLoadBack.clicked -= OnLoadBackClicked;
        if (_btnConfirmYes != null) _btnConfirmYes.clicked -= OnConfirmYesClicked;
        if (_btnConfirmNo != null)  _btnConfirmNo.clicked  -= OnConfirmNoClicked;
        if (_btnConfirmAltAction != null) _btnConfirmAltAction.clicked -= OnConfirmAltClicked;
        _vm = null;
    }

    public void Show() {
        if (_vm != null) {
            _vm.RefreshSaveSlots();
        }
        _pauseRoot.style.display = DisplayStyle.Flex;
        ShowPausePanel();
        _gameController?.StopAdvance();
        IsVisible = true;
    }

    public void Hide() {
        _pauseRoot.style.display = DisplayStyle.None;
        HideAllPanels();
        _gameController?.StartAdvance();
        IsVisible = false;
    }

    // --- Panel builders ---

    private VisualElement BuildPausePanel() {
        var panel = new VisualElement();
        panel.AddToClassList("pause-panel");

        var title = new Label("Paused");
        title.AddToClassList("pause-panel__title");
        panel.Add(title);

        _btnResume = new Button { text = "Resume" };
        _btnResume.AddToClassList("btn-primary");
        _btnResume.AddToClassList("pause-panel__btn");
        _btnResume.clicked += OnResumeClicked;
        panel.Add(_btnResume);

        _btnSaveGame = new Button { text = "Save Game" };
        _btnSaveGame.AddToClassList("btn-secondary");
        _btnSaveGame.AddToClassList("pause-panel__btn");
        _btnSaveGame.clicked += OnSaveGameClicked;
        panel.Add(_btnSaveGame);

        _btnLoadGame = new Button { text = "Load Game" };
        _btnLoadGame.AddToClassList("btn-secondary");
        _btnLoadGame.AddToClassList("pause-panel__btn");
        _btnLoadGame.clicked += OnLoadGameClicked;
        panel.Add(_btnLoadGame);

        _btnSettings = new Button { text = "Settings" };
        _btnSettings.AddToClassList("btn-secondary");
        _btnSettings.AddToClassList("pause-panel__btn");
        _btnSettings.SetEnabled(false);
        panel.Add(_btnSettings);

        _btnMainMenu = new Button { text = "Main Menu" };
        _btnMainMenu.AddToClassList("btn-ghost");
        _btnMainMenu.AddToClassList("pause-panel__btn");
        _btnMainMenu.clicked += OnMainMenuClicked;
        panel.Add(_btnMainMenu);

        _btnExit = new Button { text = "Exit Game" };
        _btnExit.AddToClassList("btn-danger");
        _btnExit.AddToClassList("pause-panel__btn");
        _btnExit.AddToClassList("pause-panel__btn--last");
        _btnExit.clicked += OnExitClicked;
        panel.Add(_btnExit);

        return panel;
    }

    private VisualElement BuildSavePanel() {
        var panel = new VisualElement();
        panel.AddToClassList("save-load-panel");

        var header = new VisualElement();
        header.AddToClassList("save-load-panel__header");

        _btnSaveBack = new Button { text = "< Back" };
        _btnSaveBack.AddToClassList("btn-ghost");
        _btnSaveBack.clicked += OnSaveBackClicked;
        header.Add(_btnSaveBack);

        var title = new Label("Save Game");
        title.AddToClassList("save-load-panel__title");
        header.Add(title);

        panel.Add(header);

        var scrollView = new ScrollView();
        scrollView.AddToClassList("save-load-panel__scroll");
        _saveSlotsContainer = scrollView.contentContainer;
        panel.Add(scrollView);

        _slotsRemainingLabel = new Label();
        _slotsRemainingLabel.AddToClassList("slots-remaining-label");
        panel.Add(_slotsRemainingLabel);

        return panel;
    }

    private VisualElement BuildLoadPanel() {
        var panel = new VisualElement();
        panel.AddToClassList("save-load-panel");

        var header = new VisualElement();
        header.AddToClassList("save-load-panel__header");

        _btnLoadBack = new Button { text = "< Back" };
        _btnLoadBack.AddToClassList("btn-ghost");
        _btnLoadBack.clicked += OnLoadBackClicked;
        header.Add(_btnLoadBack);

        var title = new Label("Load Game");
        title.AddToClassList("save-load-panel__title");
        header.Add(title);

        panel.Add(header);

        var manualHeader = new Label("Manual Saves");
        manualHeader.AddToClassList("save-section-header");
        panel.Add(manualHeader);

        _manualSavesContainer = new VisualElement();
        _manualSavesContainer.AddToClassList("save-section-container");
        panel.Add(_manualSavesContainer);

        var autoHeader = new Label("Auto Saves");
        autoHeader.AddToClassList("save-section-header");
        panel.Add(autoHeader);

        _autoSavesContainer = new VisualElement();
        _autoSavesContainer.AddToClassList("save-section-container");
        panel.Add(_autoSavesContainer);

        return panel;
    }

    private VisualElement BuildConfirmDialog() {
        var dialog = new VisualElement();
        dialog.AddToClassList("confirm-dialog");

        _confirmMessage = new Label();
        _confirmMessage.AddToClassList("confirm-dialog__message");
        dialog.Add(_confirmMessage);

        var buttons = new VisualElement();
        buttons.AddToClassList("confirm-dialog__buttons");

        _btnConfirmNo = new Button { text = "Cancel" };
        _btnConfirmNo.AddToClassList("btn-secondary");
        _btnConfirmNo.clicked += OnConfirmNoClicked;
        buttons.Add(_btnConfirmNo);

        _btnConfirmAltAction = new Button();
        _btnConfirmAltAction.AddToClassList("btn-ghost");
        _btnConfirmAltAction.clicked += OnConfirmAltClicked;
        buttons.Add(_btnConfirmAltAction);

        _btnConfirmYes = new Button { text = "Confirm" };
        _btnConfirmYes.AddToClassList("btn-primary");
        _btnConfirmYes.clicked += OnConfirmYesClicked;
        buttons.Add(_btnConfirmYes);

        dialog.Add(buttons);
        return dialog;
    }

    // --- Panel visibility ---

    private void HideAllPanels() {
        SetDisplay(_pausePanel, false);
        SetDisplay(_savePanel, false);
        SetDisplay(_loadPanel, false);
        SetDisplay(_confirmRoot, false);
    }

    private void ShowPausePanel() {
        HideAllPanels();
        SetDisplay(_pausePanel, true);
    }

    private void ShowSavePanel() {
        HideAllPanels();
        RefreshSaveSlots();
        SetDisplay(_savePanel, true);
    }

    private void ShowLoadPanel() {
        HideAllPanels();
        RefreshLoadSlots();
        SetDisplay(_loadPanel, true);
    }

    private void ShowConfirm(string message, string confirmText, Action onYes, Action onNo = null, string altText = null, Action onAlt = null) {
        _confirmMessage.text = message;
        _btnConfirmYes.text = confirmText;
        _onConfirmYes = onYes;
        _onConfirmNo = onNo;
        _onConfirmAlt = onAlt;

        bool hasAlt = !string.IsNullOrEmpty(altText) && onAlt != null;
        _btnConfirmAltAction.text = altText ?? "";
        SetDisplay(_btnConfirmAltAction, hasAlt);

        SetDisplay(_confirmRoot, true);
    }

    private static void SetDisplay(VisualElement el, bool visible) {
        if (el != null) el.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // --- Save slot rendering ---

    private void RefreshSaveSlots() {
        if (_vm == null) return;
        _vm.RefreshSaveSlots();

        foreach (var el in _saveSlotPool) _saveSlotsContainer.Remove(el);
        _saveSlotPool.Clear();

        int manualCount = _vm.ManualSaves.Count;
        for (int i = 0; i < manualCount; i++) {
            var slot = BuildSaveSlotElement(_vm.ManualSaves[i], false);
            _saveSlotsContainer.Add(slot);
            _saveSlotPool.Add(slot);
        }

        if (_vm.CanCreateNewSave) {
            var newSlot = BuildNewSaveSlotElement(_vm.DefaultSaveName);
            _saveSlotsContainer.Add(newSlot);
            _saveSlotPool.Add(newSlot);
        }

        _slotsRemainingLabel.text = _vm.ManualSaveCount + "/" + _vm.MaxManualSaves + " slots used";
    }

    private void RefreshLoadSlots() {
        if (_vm == null) return;
        _vm.RefreshSaveSlots();

        foreach (var el in _manualLoadSlotPool) _manualSavesContainer.Remove(el);
        _manualLoadSlotPool.Clear();

        foreach (var el in _autoLoadSlotPool) _autoSavesContainer.Remove(el);
        _autoLoadSlotPool.Clear();

        int manualCount = _vm.ManualSaves.Count;
        for (int i = 0; i < manualCount; i++) {
            var slot = BuildLoadSlotElement(_vm.ManualSaves[i]);
            _manualSavesContainer.Add(slot);
            _manualLoadSlotPool.Add(slot);
        }

        if (manualCount == 0) {
            var empty = new Label("No manual saves");
            empty.AddToClassList("text-muted");
            _manualSavesContainer.Add(empty);
            _manualLoadSlotPool.Add(empty);
        }

        int autoCount = _vm.AutoSaves.Count;
        for (int i = 0; i < autoCount; i++) {
            var slot = BuildLoadSlotElement(_vm.AutoSaves[i]);
            _autoSavesContainer.Add(slot);
            _autoLoadSlotPool.Add(slot);
        }

        if (autoCount == 0) {
            var empty = new Label("No auto saves");
            empty.AddToClassList("text-muted");
            _autoSavesContainer.Add(empty);
            _autoLoadSlotPool.Add(empty);
        }
    }

    private VisualElement BuildSaveSlotElement(SaveSlotDisplay data, bool isNew) {
        var row = new VisualElement();
        row.AddToClassList("save-slot");
        row.userData = data.SlotName;

        var nameField = new TextField { value = data.DisplayName };
        nameField.AddToClassList("save-slot__name");
        nameField.name = "slot-name-field";
        row.Add(nameField);

        var meta = new Label(data.DateDisplay + "  " + data.MoneyDisplay + "  " + data.EmployeeCount + " emp");
        meta.AddToClassList("save-slot__meta");
        row.Add(meta);

        var actions = new VisualElement();
        actions.AddToClassList("save-slot__actions");

        var btnSave = new Button { text = "Save" };
        btnSave.AddToClassList("btn-primary");
        btnSave.AddToClassList("btn-sm");
        btnSave.userData = row;
        btnSave.clicked += OnSaveToExistingSlotClicked;
        actions.Add(btnSave);

        var btnDelete = new Button { text = "Delete" };
        btnDelete.AddToClassList("btn-danger");
        btnDelete.AddToClassList("btn-sm");
        btnDelete.userData = data.SlotName;
        btnDelete.clicked += OnDeleteSlotClicked;
        actions.Add(btnDelete);

        row.Add(actions);
        return row;
    }

    private VisualElement BuildNewSaveSlotElement(string defaultName) {
        var row = new VisualElement();
        row.AddToClassList("save-slot");
        row.AddToClassList("save-slot--new");

        var nameField = new TextField { value = defaultName };
        nameField.AddToClassList("save-slot__name");
        nameField.name = "slot-name-field";
        row.Add(nameField);

        var meta = new Label("New Save");
        meta.AddToClassList("save-slot__meta");
        row.Add(meta);

        var actions = new VisualElement();
        actions.AddToClassList("save-slot__actions");

        var btnSave = new Button { text = "Save" };
        btnSave.AddToClassList("btn-primary");
        btnSave.AddToClassList("btn-sm");
        btnSave.userData = row;
        btnSave.clicked += OnSaveToNewSlotClicked;
        actions.Add(btnSave);

        row.Add(actions);
        return row;
    }

    private VisualElement BuildLoadSlotElement(SaveSlotDisplay data) {
        var row = new VisualElement();
        row.AddToClassList("save-slot");
        row.userData = data.SlotName;

        var info = new VisualElement();
        info.AddToClassList("save-slot__info");

        var nameLabel = new Label(data.DisplayName);
        nameLabel.AddToClassList("save-slot__name");
        info.Add(nameLabel);

        var meta = new Label(data.DateDisplay + "  " + data.MoneyDisplay + "  " + data.EmployeeCount + " emp  " + data.TimestampDisplay);
        meta.AddToClassList("save-slot__meta");
        info.Add(meta);

        row.Add(info);

        var btnLoad = new Button { text = "Load" };
        btnLoad.AddToClassList("btn-secondary");
        btnLoad.AddToClassList("btn-sm");
        btnLoad.userData = data.SlotName;
        btnLoad.clicked += OnLoadSlotClicked;
        row.Add(btnLoad);

        return row;
    }

    // --- Button handlers ---

    private void OnResumeClicked() {
        Hide();
    }

    private void OnSaveGameClicked() {
        ShowSavePanel();
    }

    private void OnLoadGameClicked() {
        ShowLoadPanel();
    }

    private void OnMainMenuClicked() {
        ShowPausePanel();
        ShowConfirm(
            "Return to Main Menu? Unsaved progress will be lost.",
            "Leave Without Saving",
            OnLeaveWithoutSaving,
            OnConfirmCancelToPause,
            "Save & Leave",
            OnSaveAndLeave
        );
    }

    private void OnExitClicked() {
        ShowPausePanel();
        ShowConfirm(
            "Exit game? Unsaved progress will be lost.",
            "Exit",
            OnExitConfirmed,
            OnConfirmCancelToPause
        );
    }

    private void OnSaveBackClicked() {
        ShowPausePanel();
    }

    private void OnLoadBackClicked() {
        ShowPausePanel();
    }

    private void OnSaveToExistingSlotClicked() {
        // Determine which slot row this came from via the event sender
        // We can't directly identify sender here; use userData on the button itself
        // The button's userData is the row element
        // Since we can't get the sender in clicked, we must walk the pool
        // Instead, iterate all save slot rows and check which btn was just clicked
        // Unity's clicked event doesn't pass a sender, so we cache the pending row in slot building
        // As a workaround, we use a single named method and rely on pool traversal.
        // The row's name-field holds the display name; we match the save slot via slot userData.
        // This design is intentional: buttons are created once per slot refresh, handler is shared.
        // Actual slot identification happens by finding which row's save-button is focused.
        // Since Unity UIToolkit doesn't expose sender, we scan for the focused/clicked element.
        // For correctness, use per-button userData as (row element) set at creation time.
        // Re-read: we set btnSave.userData = row at build time — but clicked doesn't pass sender.
        // Resolution: register per-button closure-free handlers via named method + int index approach.
        // The cleanest approach is to capture slot name in a small helper struct stored in userData
        // and read it from the focused element after click. But since this runs on click, the clicked
        // button IS the focused element at that time. Query it:
        var focused = _pauseRoot.focusController?.focusedElement as Button;
        if (focused == null) return;

        var row = focused.userData as VisualElement;
        if (row == null) return;

        string slotName = row.userData as string;
        var nameField = row.Q<TextField>("slot-name-field");
        string displayName = nameField != null ? nameField.value : slotName;

        if (!string.IsNullOrEmpty(slotName)) {
            string capturedSlot = slotName;
            string capturedName = displayName;
            ShowConfirm(
                "Overwrite this save?",
                "Overwrite",
                () => ExecuteSave(capturedSlot, capturedName),
                OnSaveConfirmCancel
            );
        } else {
            ExecuteSaveNewSlot(displayName);
        }
    }

    private void OnSaveToNewSlotClicked() {
        var focused = _pauseRoot.focusController?.focusedElement as Button;
        if (focused == null) return;

        var row = focused.userData as VisualElement;
        if (row == null) return;

        var nameField = row.Q<TextField>("slot-name-field");
        string displayName = nameField != null ? nameField.value : _vm?.DefaultSaveName ?? "New Save";
        ExecuteSaveNewSlot(displayName);
    }

    private void OnDeleteSlotClicked() {
        var focused = _pauseRoot.focusController?.focusedElement as Button;
        if (focused == null) return;

        string slotName = focused.userData as string;
        if (string.IsNullOrEmpty(slotName)) return;

        string capturedSlot = slotName;
        ShowConfirm(
            "Delete this save? This cannot be undone.",
            "Delete",
            () => {
                SaveManager.DeleteSave(capturedSlot);
                HideConfirm();
                RefreshSaveSlots();
            },
            OnSaveConfirmCancel
        );
    }

    private void OnLoadSlotClicked() {
        var focused = _pauseRoot.focusController?.focusedElement as Button;
        if (focused == null) return;

        string slotName = focused.userData as string;
        if (string.IsNullOrEmpty(slotName)) return;

        string capturedSlot = slotName;
        ShowConfirm(
            "Load this save? Unsaved progress will be lost.",
            "Load",
            () => ExecuteLoad(capturedSlot),
            OnLoadConfirmCancel
        );
    }

    private void OnConfirmYesClicked() {
        _onConfirmYes?.Invoke();
    }

    private void OnConfirmNoClicked() {
        var action = _onConfirmNo;
        HideConfirm();
        action?.Invoke();
    }

    private void OnConfirmAltClicked() {
        _onConfirmAlt?.Invoke();
    }

    private void OnConfirmCancelToPause() {
        HideConfirm();
        ShowPausePanel();
    }

    private void OnSaveConfirmCancel() {
        HideConfirm();
        ShowSavePanel();
    }

    private void OnLoadConfirmCancel() {
        HideConfirm();
        ShowLoadPanel();
    }

    private void HideConfirm() {
        SetDisplay(_confirmRoot, false);
        _onConfirmYes = null;
        _onConfirmNo = null;
        _onConfirmAlt = null;
    }

    // --- Save / Load execution ---

    private void ExecuteSave(string slotName, string displayName) {
        _gameController?.SaveCurrentGame(slotName, displayName, isAutoSave: false);
        HideConfirm();
        RefreshSaveSlots();
    }

    private void ExecuteSaveNewSlot(string displayName) {
        string slotName = "save_" + System.DateTime.UtcNow.Ticks.ToString();
        _gameController?.SaveCurrentGame(slotName, displayName, isAutoSave: false);
        HideConfirm();
        RefreshSaveSlots();
    }

    private void ExecuteLoad(string slotName) {
        NewGameData.IsNewGame = false;
        NewGameData.LoadSlotName = slotName;
        SceneManager.LoadScene("MainGame");
    }

    private void OnLeaveWithoutSaving() {
        SceneManager.LoadScene("MainMenu");
    }

    private void OnSaveAndLeave() {
        var latest = SaveManager.GetLatestSave();
        if (latest.HasValue && !latest.Value.IsAutoSave) {
            _gameController?.SaveCurrentGame(latest.Value.SlotName, latest.Value.DisplayName, isAutoSave: false);
        } else {
            string slotName = "save_" + System.DateTime.UtcNow.Ticks.ToString();
            string displayName = _vm?.DefaultSaveName ?? "Quick Save";
            _gameController?.SaveCurrentGame(slotName, displayName, isAutoSave: false);
        }
        SceneManager.LoadScene("MainMenu");
    }

    private void OnExitConfirmed() {
        Application.Quit();
    }
}
