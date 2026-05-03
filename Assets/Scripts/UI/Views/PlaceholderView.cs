using UnityEngine.UIElements;

/// <summary>
/// IGameView for screens not yet implemented.
/// Builds a centred container with a screen title and a "Coming soon" message.
/// Initialize() creates the element tree once; Bind() updates labels; Dispose() clears refs.
/// </summary>
public class PlaceholderView : IGameView
{
    private Label _titleLabel;
    private Label _messageLabel;
    private PlaceholderViewModel _viewModel;

    // ── IGameView ─────────────────────────────────────────────────────────────

    public void Initialize(VisualElement root, UIServices services)
    {
        if (root == null) return;

        var container = new VisualElement();
        container.AddToClassList("placeholder-container");

        _titleLabel = new Label();
        _titleLabel.AddToClassList("placeholder-title");
        container.Add(_titleLabel);

        _messageLabel = new Label("Implemented in later roadmap wave");
        _messageLabel.AddToClassList("placeholder-message");
        container.Add(_messageLabel);

        root.Add(container);
    }

    public void Bind(IViewModel viewModel)
    {
        _viewModel = viewModel as PlaceholderViewModel;
        if (_viewModel == null) return;

        if (_titleLabel   != null) _titleLabel.text   = _viewModel.ScreenName;
        if (_messageLabel != null) _messageLabel.text = "Implemented in later roadmap wave";
    }

    public void Dispose()
    {
        _titleLabel   = null;
        _messageLabel = null;
        _viewModel    = null;
    }
}
