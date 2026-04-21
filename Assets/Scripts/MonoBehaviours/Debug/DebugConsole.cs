using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class DebugConsole : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    private static DebugConsole _instance;

    private UIDocument _uiDocument;
    private VisualElement _root;
    private VisualElement _consoleContainer;
    private ScrollView _scrollView;
    private TextField _inputField;

    private readonly List<IDebugCommandHandler> _handlers = new List<IDebugCommandHandler>();
    private readonly List<string> _outputLines = new List<string>();
    private readonly List<string> _commandHistory = new List<string>();
    private int _historyIndex = -1;

    private bool _initialized;

    private const int MaxOutputLines = 200;
    private const int MaxHistoryEntries = 50;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        var go = new GameObject("__DebugConsole__");
        go.hideFlags = HideFlags.HideInHierarchy;
        DontDestroyOnLoad(go);

        var uiDoc = go.AddComponent<UIDocument>();

        PanelSettings panelSettings = null;
        var allDocs = Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
        for (int i = 0; i < allDocs.Length; i++)
        {
            if (allDocs[i] != uiDoc && allDocs[i].panelSettings != null)
            {
                panelSettings = allDocs[i].panelSettings;
                break;
            }
        }
        if (panelSettings == null)
            panelSettings = Resources.Load<PanelSettings>("DebugPanelSettings");
        if (panelSettings != null)
            uiDoc.panelSettings = panelSettings;

        uiDoc.sortingOrder = 1000;

        _instance = go.AddComponent<DebugConsole>();
        _instance._uiDocument = uiDoc;
    }

    private void OnEnable()
    {
        // May fire before Bootstrap assigns _uiDocument; defer to Update
    }

    private bool TryInitialize()
    {
        if (_uiDocument == null)
            _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null || _uiDocument.rootVisualElement == null)
            return false;
        BuildUI();
        Hide();
        _initialized = true;
        return true;
    }

    private void Update()
    {
        if (!_initialized)
        {
            if (!TryInitialize()) return;
        }

        if (Keyboard.current == null) return;

        if (Keyboard.current.backquoteKey.wasPressedThisFrame)
        {
            if (IsOpen)
                Hide();
            else
                Show();

            // Strip any backtick that was inserted before our callback ran
            if (_inputField != null && _inputField.value.Contains("`"))
                _inputField.value = _inputField.value.Replace("`", "");
            return;
        }

        if (!IsOpen) return;

        // Command history navigation
        if (Keyboard.current.upArrowKey.wasPressedThisFrame && _commandHistory.Count > 0)
        {
            if (_historyIndex < 0)
                _historyIndex = _commandHistory.Count - 1;
            else if (_historyIndex > 0)
                _historyIndex--;

            _inputField.value = _commandHistory[_historyIndex];
        }

        if (Keyboard.current.downArrowKey.wasPressedThisFrame && _commandHistory.Count > 0)
        {
            if (_historyIndex >= 0 && _historyIndex < _commandHistory.Count - 1)
            {
                _historyIndex++;
                _inputField.value = _commandHistory[_historyIndex];
            }
            else
            {
                _historyIndex = -1;
                _inputField.value = "";
            }
        }

        // Submit on Enter
        if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
        {
            string input = _inputField.value;
            if (!string.IsNullOrWhiteSpace(input))
            {
                ProcessInput(input.Trim());
                _inputField.value = "";
            }
            // Re-focus after submit
            _inputField.schedule.Execute(() => _inputField.Focus());
        }

        // Escape to close
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Hide();
        }
    }

    public void RegisterHandler(IDebugCommandHandler handler)
    {
        if (handler != null && !_handlers.Contains(handler))
            _handlers.Add(handler);
    }

    public void Print(string message)
    {
        AddOutputLine($"<color=#AAF0AA>{message}</color>");
    }

    public void PrintError(string message)
    {
        AddOutputLine($"<color=#FF6666>{message}</color>");
    }

    private void Show()
    {
        IsOpen = true;
        _consoleContainer.style.display = DisplayStyle.Flex;
        _inputField.schedule.Execute(() => _inputField.Focus());
    }

    private void Hide()
    {
        IsOpen = false;
        _consoleContainer.style.display = DisplayStyle.None;
    }

    private void ProcessInput(string input)
    {
        // Add to history
        if (_commandHistory.Count >= MaxHistoryEntries)
            _commandHistory.RemoveAt(0);
        _commandHistory.Add(input);
        _historyIndex = -1;

        AddOutputLine($"<color=#88CCFF>> {input}</color>");

        string[] parts = input.Split(' ');
        string commandName = parts[0].ToLowerInvariant();
        string[] args = new string[parts.Length - 1];
        for (int i = 1; i < parts.Length; i++)
            args[i - 1] = parts[i];

        // Built-in commands
        if (commandName == "help")
        {
            PrintHelp();
            return;
        }

        if (commandName == "clear")
        {
            ClearOutput();
            return;
        }

        // Try registered handlers
        for (int i = 0; i < _handlers.Count; i++)
        {
            if (_handlers[i].TryExecute(commandName, args, this))
                return;
        }

        PrintError($"Unknown command: {commandName}. Type 'help' for available commands.");
    }

    private void PrintHelp()
    {
        Print("=== Available Commands ===");
        Print("  help          - Show this help message");
        Print("  clear         - Clear console output");

        for (int i = 0; i < _handlers.Count; i++)
        {
            var commands = _handlers[i].GetCommands();
            if (commands == null) continue;
            foreach (var kvp in commands)
            {
                Print($"  {kvp.Key,-20} - {kvp.Value}");
            }
        }
    }

    private void AddOutputLine(string richLine)
    {
        if (_outputLines.Count >= MaxOutputLines)
        {
            _outputLines.RemoveAt(0);
            if (_scrollView != null && _scrollView.childCount > 0)
                _scrollView.RemoveAt(0);
        }
        _outputLines.Add(richLine);

        if (_scrollView != null)
        {
            var label = new Label(richLine);
            label.enableRichText = true;
            label.style.color = new Color(0.67f, 0.94f, 0.67f);
            label.style.fontSize = 13;
            label.style.unityFontStyleAndWeight = FontStyle.Normal;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginBottom = 1;
            label.style.marginTop = 0;
            label.style.paddingLeft = 4;
            _scrollView.Add(label);

            // Auto-scroll to bottom
            _scrollView.schedule.Execute(() =>
            {
                _scrollView.scrollOffset = new Vector2(0, float.MaxValue);
            });
        }
    }

    private void ClearOutput()
    {
        _outputLines.Clear();
        if (_scrollView != null)
            _scrollView.Clear();
    }

    private void BuildUI()
    {
        _root = _uiDocument.rootVisualElement;
        _root.Clear();

        // Main container - bottom 40% of screen
        _consoleContainer = new VisualElement();
        _consoleContainer.style.position = Position.Absolute;
        _consoleContainer.style.left = 0;
        _consoleContainer.style.right = 0;
        _consoleContainer.style.bottom = 0;
        _consoleContainer.style.height = Length.Percent(40);
        _consoleContainer.style.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 0.92f);
        _consoleContainer.style.borderTopWidth = 2;
        _consoleContainer.style.borderTopColor = new Color(0.2f, 0.6f, 0.2f, 0.8f);
        _consoleContainer.style.flexDirection = FlexDirection.Column;

        // Title bar
        var titleBar = new VisualElement();
        titleBar.style.flexDirection = FlexDirection.Row;
        titleBar.style.justifyContent = Justify.SpaceBetween;
        titleBar.style.paddingLeft = 8;
        titleBar.style.paddingRight = 8;
        titleBar.style.paddingTop = 4;
        titleBar.style.paddingBottom = 4;
        titleBar.style.backgroundColor = new Color(0.08f, 0.12f, 0.08f, 1f);

        var titleLabel = new Label("Debug Console");
        titleLabel.style.color = new Color(0.4f, 0.9f, 0.4f);
        titleLabel.style.fontSize = 14;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleBar.Add(titleLabel);

        var hintLabel = new Label("Press ` to toggle | ESC to close");
        hintLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
        hintLabel.style.fontSize = 11;
        titleBar.Add(hintLabel);

        _consoleContainer.Add(titleBar);

        // Output scroll view
        _scrollView = new ScrollView(ScrollViewMode.Vertical);
        _scrollView.style.flexGrow = 1;
        _scrollView.style.paddingLeft = 4;
        _scrollView.style.paddingRight = 4;
        _scrollView.style.paddingTop = 4;
        _scrollView.style.paddingBottom = 4;
        _consoleContainer.Add(_scrollView);

        // Input area
        var inputContainer = new VisualElement();
        inputContainer.style.flexDirection = FlexDirection.Row;
        inputContainer.style.paddingLeft = 4;
        inputContainer.style.paddingRight = 4;
        inputContainer.style.paddingBottom = 4;
        inputContainer.style.paddingTop = 2;
        inputContainer.style.backgroundColor = new Color(0.03f, 0.03f, 0.05f, 1f);

        var promptLabel = new Label(">");
        promptLabel.style.color = new Color(0.4f, 0.9f, 0.4f);
        promptLabel.style.fontSize = 14;
        promptLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        promptLabel.style.marginRight = 4;
        promptLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
        inputContainer.Add(promptLabel);

        _inputField = new TextField();
        _inputField.style.flexGrow = 1;
        _inputField.style.backgroundColor = new Color(0.08f, 0.08f, 0.1f, 1f);
        _inputField.style.color = new Color(0.8f, 1f, 0.8f);
        _inputField.style.fontSize = 14;
        _inputField.style.borderBottomWidth = 0;
        _inputField.style.borderTopWidth = 0;
        _inputField.style.borderLeftWidth = 0;
        _inputField.style.borderRightWidth = 0;

        // Block backtick from being typed into the field
        _inputField.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.character == '`' || evt.keyCode == KeyCode.BackQuote)
            {
                evt.StopImmediatePropagation();
                evt.PreventDefault();
            }
        }, TrickleDown.TrickleDown);

        inputContainer.Add(_inputField);

        _consoleContainer.Add(inputContainer);

        _root.Add(_consoleContainer);
    }
}
