using UnityEngine.UIElements;

public interface IGameView
{
    void Initialize(VisualElement root);
    void Bind(IViewModel viewModel);
    void Dispose();
}
