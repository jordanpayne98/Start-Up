using UnityEngine.UIElements;

/// <summary>
/// Minimal placeholder screen view. Used as the default factory for screens that
/// have not yet been implemented. Prevents dead navigation — every sidebar item
/// navigates successfully and shows a "Coming Soon" label.
/// Implements IGameView: Initialize creates a centred label; Bind and Dispose are no-ops.
/// </summary>
public class PlaceholderScreenView : IGameView
{
    private readonly string _screenName;

    public PlaceholderScreenView(string screenName = "Screen")
    {
        _screenName = screenName;
    }

    // ── IGameView ────────────────────────────────────────────────────────────

    public void Initialize(VisualElement root, UIServices services)
    {
        if (root == null) return;

        var container = new VisualElement();
        container.AddToClassList("placeholder-screen");
        container.style.flexGrow        = 1;
        container.style.alignItems      = Align.Center;
        container.style.justifyContent  = Justify.Center;

        var title = new Label(_screenName);
        title.AddToClassList("placeholder-screen__title");
        title.AddToClassList("text-muted");
        container.Add(title);

        var subtitle = new Label("Coming Soon");
        subtitle.AddToClassList("placeholder-screen__subtitle");
        subtitle.AddToClassList("text-muted");
        container.Add(subtitle);

        root.Add(container);
    }

    public void Bind(IViewModel viewModel) { }

    public void Dispose() { }
}
