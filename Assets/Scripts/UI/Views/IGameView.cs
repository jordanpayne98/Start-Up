using UnityEngine.UIElements;

public interface IGameView
{
    void Initialize(VisualElement root, UIServices services);
    void Bind(IViewModel viewModel);
    void Dispose();
}
