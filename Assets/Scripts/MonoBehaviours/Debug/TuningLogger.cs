using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// Auto-bootstrapping logger that records all TuningConfig parameter changes to
/// /Assets/Logs/sessiontuning_XXX.txt. One file per play session.
/// Writes a summary diff on shutdown listing every parameter that changed.
/// </summary>
public class TuningLogger : MonoBehaviour
{
    private GameController _gameController;
    private TuningConfig _tuning;
    private StreamWriter _writer;
    private bool _bound;
    private int _sessionIndex;

    // Tracks changes as (paramName -> (firstValue, latestValue))
    private readonly Dictionary<string, (object first, object latest)> _changes
        = new Dictionary<string, (object, object)>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        var go = new GameObject("__TuningLogger__");
        go.hideFlags = HideFlags.HideInHierarchy;
        DontDestroyOnLoad(go);
        go.AddComponent<TuningLogger>();
    }

    private void OnEnable()
    {
        string logsDir = Path.Combine(Application.dataPath, "..", "Logs");
        if (!Directory.Exists(logsDir))
            Directory.CreateDirectory(logsDir);

        _sessionIndex = FindNextSessionIndex(logsDir);
        string filePath = Path.Combine(logsDir, $"sessiontuning_{_sessionIndex:D3}.txt");

        _writer = new StreamWriter(
            new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read),
            Encoding.UTF8) { AutoFlush = true };
        _writer.WriteLine("=== TUNING LOG ===");
        _writer.WriteLine($"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        _writer.WriteLine($"Session: {_sessionIndex}");
        _writer.WriteLine(new string('=', 60));
        _writer.WriteLine();
    }

    private void OnDisable()
    {
        if (_tuning != null)
            _tuning.OnParameterChanged -= OnParameterChanged;

        if (_writer != null)
        {
            WriteSummary();
            _writer.WriteLine();
            _writer.WriteLine(new string('=', 60));
            _writer.WriteLine($"Ended: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _writer.Flush();
            _writer.Close();
            _writer = null;
        }
    }

    private void Update()
    {
        if (_bound) return;

        if (_gameController == null)
        {
            _gameController = FindAnyObjectByType<GameController>();
            if (_gameController == null) return;
        }

        _tuning = _gameController.Tuning;
        if (_tuning == null) return;

        _tuning.OnParameterChanged += OnParameterChanged;
        _bound = true;

        WriteHeader("BOUND");
    }

    private void OnParameterChanged(string name, object oldVal, object newVal)
    {
        int tick = _gameController?.GetGameState()?.currentTick ?? -1;

        if (!_changes.TryGetValue(name, out var entry))
            _changes[name] = (oldVal, newVal);
        else
            _changes[name] = (entry.first, newVal);

        _writer?.WriteLine($"[T:{tick}] CHANGE | {name}: {oldVal} -> {newVal}");
    }

    private void WriteSummary()
    {
        if (_changes.Count == 0)
        {
            _writer.WriteLine("--- No parameters were changed this session ---");
            return;
        }

        _writer.WriteLine();
        _writer.WriteLine(new string('-', 60));
        _writer.WriteLine($"SUMMARY — {_changes.Count} parameter(s) changed");
        _writer.WriteLine(new string('-', 60));

        var sb = new StringBuilder();
        foreach (var kvp in _changes)
        {
            sb.Clear();
            sb.Append(kvp.Key.PadRight(42));
            sb.Append($"{kvp.Value.first}  ->  {kvp.Value.latest}");
            _writer.WriteLine(sb.ToString());
        }
    }

    private void WriteHeader(string tag)
    {
        _writer?.WriteLine($"[{tag}] TuningLogger bound to GameController");
    }

    private static int FindNextSessionIndex(string logsDir)
    {
        int maxIndex = -1;
        string[] files = Directory.GetFiles(logsDir, "sessiontuning_*.txt");
        for (int i = 0; i < files.Length; i++)
        {
            string fileName = Path.GetFileNameWithoutExtension(files[i]);
            // "sessiontuning_".Length == 14
            if (fileName.Length > 14)
            {
                string numStr = fileName.Substring(14);
                if (int.TryParse(numStr, out int idx) && idx > maxIndex)
                    maxIndex = idx;
            }
        }
        return maxIndex + 1;
    }
}
