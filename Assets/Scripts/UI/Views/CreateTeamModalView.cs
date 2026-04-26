using System;
using UnityEngine.UIElements;

public class CreateTeamModalView : IGameView
{
    private readonly ICommandDispatcher _dispatcher;
    private readonly IModalPresenter _modal;

    private VisualElement _root;
    private TextField _nameField;
    private Button[] _typeButtons;
    private Label _errorLabel;
    private Button _createBtn;
    private Button _cancelBtn;
    private TeamType _selectedType = TeamType.Development;

    public CreateTeamModalView(ICommandDispatcher dispatcher, IModalPresenter modal) {
        _dispatcher = dispatcher;
        _modal = modal;
    }

    public void Initialize(VisualElement root) {
        _root = root;
        _root.AddToClassList("create-team-modal");

        var title = new Label("Create New Team");
        title.AddToClassList("modal-title");
        title.AddToClassList("text-bold");
        title.style.marginBottom = 16;
        _root.Add(title);

        var nameRow = new VisualElement();
        nameRow.style.marginBottom = 8;
        var nameLabel = new Label("Team Name");
        nameLabel.AddToClassList("metric-tertiary");
        nameLabel.style.marginBottom = 4;
        nameRow.Add(nameLabel);
        _nameField = new TextField();
        _nameField.AddToClassList("input-field");
        _nameField.maxLength = 40;
        nameRow.Add(_nameField);
        _root.Add(nameRow);

        var typeRow = new VisualElement();
        typeRow.style.marginBottom = 16;
        var typeLabel = new Label("Team Type");
        typeLabel.AddToClassList("metric-tertiary");
        typeLabel.style.marginBottom = 4;
        typeRow.Add(typeLabel);

        var btnGroup = new VisualElement();
        btnGroup.style.flexDirection = FlexDirection.Row;
        btnGroup.style.flexWrap = Wrap.Wrap;
        typeRow.Add(btnGroup);

        var values = (TeamType[])Enum.GetValues(typeof(TeamType));
        _typeButtons = new Button[values.Length];
        for (int i = 0; i < values.Length; i++) {
            var type = values[i];
            var btn = new Button { text = UIFormatting.FormatTeamType(type) };
            btn.userData = type;
            btn.AddToClassList("team-type-btn");
            btn.AddToClassList(UIFormatting.TeamTypeBadgeClass(type));
            btn.RegisterCallback<ClickEvent>(OnTypeButtonClick);
            _typeButtons[i] = btn;
            btnGroup.Add(btn);
        }

        _root.Add(typeRow);
        SelectType(TeamType.Development);

        _errorLabel = new Label();
        _errorLabel.AddToClassList("text-danger");
        _errorLabel.AddToClassList("text-sm");
        _errorLabel.style.display = DisplayStyle.None;
        _errorLabel.style.marginBottom = 8;
        _root.Add(_errorLabel);

        var footer = new VisualElement();
        footer.AddToClassList("modal-footer");
        footer.AddToClassList("flex-row");
        footer.AddToClassList("justify-end");
        footer.style.marginTop = 8;
        _root.Add(footer);

        _cancelBtn = new Button { text = "Cancel" };
        _cancelBtn.AddToClassList("btn-ghost");
        _cancelBtn.style.marginRight = 8;
        footer.Add(_cancelBtn);

        _createBtn = new Button { text = "Create Team" };
        _createBtn.AddToClassList("btn-primary");
        footer.Add(_createBtn);

        _createBtn.clicked += OnCreateClicked;
        _cancelBtn.clicked += OnCancelClicked;
    }

    public void Bind(IViewModel viewModel) {
        _nameField.value = "";
        if (_errorLabel != null) _errorLabel.style.display = DisplayStyle.None;
        SelectType(TeamType.Development);
    }

    public void Dispose() {
        if (_createBtn != null) _createBtn.clicked -= OnCreateClicked;
        if (_cancelBtn != null) _cancelBtn.clicked -= OnCancelClicked;
        if (_typeButtons != null) {
            for (int i = 0; i < _typeButtons.Length; i++) {
                if (_typeButtons[i] != null)
                    _typeButtons[i].UnregisterCallback<ClickEvent>(OnTypeButtonClick);
            }
        }
        _nameField = null;
        _typeButtons = null;
        _errorLabel = null;
        _createBtn = null;
        _cancelBtn = null;
        _root = null;
    }

    private void OnTypeButtonClick(ClickEvent evt) {
        var btn = evt.currentTarget as Button;
        if (btn?.userData is TeamType type) SelectType(type);
    }

    private void SelectType(TeamType type) {
        _selectedType = type;
        if (_typeButtons == null) return;
        for (int i = 0; i < _typeButtons.Length; i++) {
            var btn = _typeButtons[i];
            if (btn == null) continue;
            if (btn.userData is TeamType btnType && btnType == type)
                btn.AddToClassList("team-type-btn--selected");
            else
                btn.RemoveFromClassList("team-type-btn--selected");
        }
    }

    private void OnCreateClicked() {
        string teamName = _nameField != null ? _nameField.value.Trim() : "";
        if (string.IsNullOrEmpty(teamName)) {
            ShowError("Team name cannot be empty.");
            return;
        }

        _dispatcher.Dispatch(new CreateTeamCommand {
            TeamType = _selectedType,
            CompanyId = CompanyId.Player,
            Name = teamName
        });
        _modal?.DismissModal();
    }

    private void OnCancelClicked() {
        _modal?.DismissModal();
    }

    private void ShowError(string message) {
        if (_errorLabel == null) return;
        _errorLabel.text = message;
        _errorLabel.style.display = DisplayStyle.Flex;
    }
}
