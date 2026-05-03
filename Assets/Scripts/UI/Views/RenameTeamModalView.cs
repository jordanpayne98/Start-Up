using UnityEngine.UIElements;

public class RenameTeamModalView : IGameView
{
    private readonly ICommandDispatcher _dispatcher;
    private readonly IModalPresenter _modal;
    private readonly TeamId _teamId;
    private readonly string _currentName;

    private VisualElement _root;
    private TextField _nameField;
    private Label _errorLabel;
    private Button _saveBtn;
    private Button _cancelBtn;

    public RenameTeamModalView(ICommandDispatcher dispatcher, IModalPresenter modal, TeamId teamId, string currentName) {
        _dispatcher = dispatcher;
        _modal = modal;
        _teamId = teamId;
        _currentName = currentName;
    }

    public void Initialize(VisualElement root, UIServices services) {
        _root = root;
        _root.AddToClassList("rename-team-modal");

        var title = new Label("Rename Team");
        title.AddToClassList("modal-title");
        title.AddToClassList("text-bold");
        title.style.marginBottom = 16;
        _root.Add(title);

        var nameLabel = new Label("New Team Name");
        nameLabel.AddToClassList("metric-tertiary");
        nameLabel.style.marginBottom = 4;
        _root.Add(nameLabel);

        _nameField = new TextField();
        _nameField.AddToClassList("input-field");
        _nameField.maxLength = 40;
        _nameField.value = _currentName;
        _nameField.style.marginBottom = 8;
        _root.Add(_nameField);

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

        _saveBtn = new Button { text = "Save" };
        _saveBtn.AddToClassList("btn-primary");
        footer.Add(_saveBtn);

        _saveBtn.clicked += OnSaveClicked;
        _cancelBtn.clicked += OnCancelClicked;
    }

    public void Bind(IViewModel viewModel) { }

    public void Dispose() {
        if (_saveBtn != null)   _saveBtn.clicked   -= OnSaveClicked;
        if (_cancelBtn != null) _cancelBtn.clicked -= OnCancelClicked;
        _nameField = null;
        _errorLabel = null;
        _saveBtn = null;
        _cancelBtn = null;
        _root = null;
    }

    private void OnSaveClicked() {
        string newName = _nameField != null ? _nameField.value.Trim() : "";
        if (string.IsNullOrEmpty(newName)) {
            ShowError("Team name cannot be empty.");
            return;
        }
        _dispatcher.Dispatch(new RenameTeamCommand { TeamId = _teamId, NewName = newName });
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
