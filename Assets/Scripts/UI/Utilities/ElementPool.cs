using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class ElementPool
{
    private readonly Func<VisualElement> _factory;
    private readonly VisualElement _container;
    private readonly List<VisualElement> _elements = new List<VisualElement>();

    public ElementPool(Func<VisualElement> factory, VisualElement container) {
        _factory = factory;
        _container = container;
    }

    public void UpdateList<T>(List<T> data, Action<VisualElement, T> bind) {
        if (_container == null) {
            UnityEngine.Debug.LogError("[ElementPool] Container is null — element was not found in UXML. Skipping UpdateList.");
            return;
        }
        int dataCount = data != null ? data.Count : 0;
        int elementCount = _elements.Count;

        // Create new elements if needed
        for (int i = elementCount; i < dataCount; i++) {
            var element = _factory();
            _container.Add(element);
            _elements.Add(element);
        }

        // Bind visible elements
        for (int i = 0; i < dataCount; i++) {
            var element = _elements[i];
            element.style.display = DisplayStyle.Flex;
            bind(element, data[i]);
        }

        // Hide surplus elements
        int totalElements = _elements.Count;
        for (int i = dataCount; i < totalElements; i++) {
            _elements[i].style.display = DisplayStyle.None;
        }
    }

    public int ActiveCount {
        get {
            int count = 0;
            int total = _elements.Count;
            for (int i = 0; i < total; i++) {
                if (_elements[i].resolvedStyle.display == DisplayStyle.Flex) count++;
            }
            return count;
        }
    }

    public void Clear() {
        int count = _elements.Count;
        for (int i = 0; i < count; i++) {
            _elements[i].style.display = DisplayStyle.None;
        }
    }
}
