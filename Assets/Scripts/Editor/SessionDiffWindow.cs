using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class SessionDiffWindow : EditorWindow
{
    private string[] _sessionFiles;
    private string[] _sessionDisplayNames;
    private int _leftIndex;
    private int _rightIndex;
    private Vector2 _scrollPos;

    private string[] _leftLines;
    private string[] _rightLines;
    private List<DiffEntry> _diffEntries;
    private bool _hasDiff;

    // Category filters
    private bool _showEmployee = true;
    private bool _showTeam = true;
    private bool _showContract = true;
    private bool _showFinance = true;
    private bool _showReputation = true;
    private bool _showHiring = true;
    private bool _showInterview = true;
    private bool _showTime = true;
    private bool _showAuto = true;
    private bool _showConsole = true;
    private bool _showSession = true;

    private int _leftOnlyCount;
    private int _rightOnlyCount;
    private int _commonCount;

    private const int MaxLinesPerFile = 10000;

    private enum DiffType { Common, LeftOnly, RightOnly }

    private struct DiffEntry
    {
        public DiffType Type;
        public string LeftLine;
        public string RightLine;
        public string Category;
    }

    [MenuItem("Tools/Session Diff")]
    private static void ShowWindow()
    {
        var window = GetWindow<SessionDiffWindow>("Session Diff");
        window.minSize = new Vector2(800, 400);
        window.RefreshFileList();
    }

    private void OnEnable()
    {
        RefreshFileList();
    }

    private void RefreshFileList()
    {
        string logsDir = Path.Combine(Application.dataPath, "..", "Logs");
        if (!Directory.Exists(logsDir))
        {
            _sessionFiles = new string[0];
            _sessionDisplayNames = new string[0];
            return;
        }

        var files = new List<string>(Directory.GetFiles(logsDir, "session_*.txt"));
        files.Sort((a, b) =>
        {
            int idxA = ParseSessionIndex(a);
            int idxB = ParseSessionIndex(b);
            return idxA.CompareTo(idxB);
        });

        _sessionFiles = files.ToArray();
        _sessionDisplayNames = new string[_sessionFiles.Length];
        for (int i = 0; i < _sessionFiles.Length; i++)
            _sessionDisplayNames[i] = Path.GetFileNameWithoutExtension(_sessionFiles[i]);
    }

    private static int ParseSessionIndex(string path)
    {
        string name = Path.GetFileNameWithoutExtension(path);
        if (name.Length > 8)
        {
            string numStr = name.Substring(8);
            if (int.TryParse(numStr, out int idx))
                return idx;
        }
        return 0;
    }

    private void OnGUI()
    {
        if (_sessionFiles == null || _sessionFiles.Length == 0)
        {
            EditorGUILayout.HelpBox("No session log files found in Logs/", MessageType.Info);
            if (GUILayout.Button("Refresh"))
                RefreshFileList();
            return;
        }

        // File selection
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.35f));
        EditorGUILayout.LabelField("Session A", EditorStyles.boldLabel);
        _leftIndex = EditorGUILayout.Popup(_leftIndex, _sessionDisplayNames);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.35f));
        EditorGUILayout.LabelField("Session B", EditorStyles.boldLabel);
        _rightIndex = EditorGUILayout.Popup(_rightIndex, _sessionDisplayNames);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical();
        if (GUILayout.Button("Diff", GUILayout.Height(36)))
            PerformDiff();
        if (GUILayout.Button("Refresh Files"))
            RefreshFileList();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // Category filters
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Filters:", GUILayout.Width(50));
        _showEmployee = GUILayout.Toggle(_showEmployee, "Employee", "Button", GUILayout.Width(70));
        _showTeam = GUILayout.Toggle(_showTeam, "Team", "Button", GUILayout.Width(50));
        _showContract = GUILayout.Toggle(_showContract, "Contract", "Button", GUILayout.Width(65));
        _showFinance = GUILayout.Toggle(_showFinance, "Finance", "Button", GUILayout.Width(60));
        _showReputation = GUILayout.Toggle(_showReputation, "Rep", "Button", GUILayout.Width(40));
        _showHiring = GUILayout.Toggle(_showHiring, "Hiring", "Button", GUILayout.Width(50));
        _showInterview = GUILayout.Toggle(_showInterview, "Interview", "Button", GUILayout.Width(65));
        _showTime = GUILayout.Toggle(_showTime, "Time", "Button", GUILayout.Width(45));
        _showAuto = GUILayout.Toggle(_showAuto, "Auto", "Button", GUILayout.Width(45));
        _showConsole = GUILayout.Toggle(_showConsole, "Console", "Button", GUILayout.Width(60));
        _showSession = GUILayout.Toggle(_showSession, "Session", "Button", GUILayout.Width(55));
        EditorGUILayout.EndHorizontal();

        if (!_hasDiff)
        {
            EditorGUILayout.HelpBox("Select two sessions and click 'Diff' to compare.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(4);

        // Summary
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"A: {_leftLines.Length} lines", GUILayout.Width(120));
        EditorGUILayout.LabelField($"B: {_rightLines.Length} lines", GUILayout.Width(120));
        EditorGUILayout.LabelField($"Common: {_commonCount}", GUILayout.Width(100));

        var origColor = GUI.color;
        GUI.color = new Color(1f, 0.6f, 0.6f);
        EditorGUILayout.LabelField($"-{_leftOnlyCount} (A only)", GUILayout.Width(90));
        GUI.color = new Color(0.6f, 1f, 0.6f);
        EditorGUILayout.LabelField($"+{_rightOnlyCount} (B only)", GUILayout.Width(90));
        GUI.color = origColor;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // Diff view
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        float halfWidth = (position.width - 30) * 0.5f;

        for (int i = 0; i < _diffEntries.Count; i++)
        {
            var entry = _diffEntries[i];

            if (!ShouldShowCategory(entry.Category))
                continue;

            EditorGUILayout.BeginHorizontal();

            switch (entry.Type)
            {
                case DiffType.Common:
                    GUI.color = Color.white;
                    EditorGUILayout.LabelField(entry.LeftLine ?? "", GUILayout.Width(halfWidth));
                    EditorGUILayout.LabelField(entry.RightLine ?? "", GUILayout.Width(halfWidth));
                    break;

                case DiffType.LeftOnly:
                    GUI.color = new Color(1f, 0.7f, 0.7f);
                    EditorGUILayout.LabelField($"- {entry.LeftLine}", GUILayout.Width(halfWidth));
                    GUI.color = Color.white;
                    EditorGUILayout.LabelField("", GUILayout.Width(halfWidth));
                    break;

                case DiffType.RightOnly:
                    GUI.color = Color.white;
                    EditorGUILayout.LabelField("", GUILayout.Width(halfWidth));
                    GUI.color = new Color(0.7f, 1f, 0.7f);
                    EditorGUILayout.LabelField($"+ {entry.RightLine}", GUILayout.Width(halfWidth));
                    break;
            }

            GUI.color = origColor;
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private bool ShouldShowCategory(string category)
    {
        if (string.IsNullOrEmpty(category)) return true;
        switch (category)
        {
            case "EMPLOYEE": return _showEmployee;
            case "TEAM": return _showTeam;
            case "CONTRACT": return _showContract;
            case "FINANCE": return _showFinance;
            case "REPUTATION": return _showReputation;
            case "HIRING": return _showHiring;
            case "INTERVIEW": return _showInterview;
            case "TIME": return _showTime;
            case "AUTO": return _showAuto;
            case "CONSOLE": return _showConsole;
            case "SESSION": return _showSession;
            default: return true;
        }
    }

    private void PerformDiff()
    {
        if (_leftIndex >= _sessionFiles.Length || _rightIndex >= _sessionFiles.Length) return;

        _leftLines = ReadLines(_sessionFiles[_leftIndex]);
        _rightLines = ReadLines(_sessionFiles[_rightIndex]);

        // Simple LCS-based diff
        _diffEntries = ComputeDiff(_leftLines, _rightLines);

        _leftOnlyCount = 0;
        _rightOnlyCount = 0;
        _commonCount = 0;

        for (int i = 0; i < _diffEntries.Count; i++)
        {
            switch (_diffEntries[i].Type)
            {
                case DiffType.Common: _commonCount++; break;
                case DiffType.LeftOnly: _leftOnlyCount++; break;
                case DiffType.RightOnly: _rightOnlyCount++; break;
            }
        }

        _hasDiff = true;
    }

    private static string[] ReadLines(string path)
    {
        if (!File.Exists(path)) return new string[0];
        var allLines = File.ReadAllLines(path);
        if (allLines.Length > MaxLinesPerFile)
        {
            var capped = new string[MaxLinesPerFile];
            System.Array.Copy(allLines, capped, MaxLinesPerFile);
            return capped;
        }
        return allLines;
    }

    private static string ExtractCategory(string line)
    {
        // Parse "[T:xxx | Day x Mo x Yr x] [CATEGORY] ..." format
        int firstBracket = line.IndexOf('[');
        if (firstBracket < 0) return "";

        int closeBracket = line.IndexOf(']', firstBracket);
        if (closeBracket < 0) return "";

        int secondOpen = line.IndexOf('[', closeBracket);
        if (secondOpen < 0) return "";

        int secondClose = line.IndexOf(']', secondOpen);
        if (secondClose < 0) return "";

        return line.Substring(secondOpen + 1, secondClose - secondOpen - 1);
    }

    private static List<DiffEntry> ComputeDiff(string[] left, string[] right)
    {
        int m = left.Length;
        int n = right.Length;

        // For large files, use a simpler line-by-line comparison
        if (m + n > 5000)
            return SimpleDiff(left, right);

        // LCS table (space-optimized would be better but this is an editor tool)
        int[,] lcs = new int[m + 1, n + 1];
        for (int i = m - 1; i >= 0; i--)
        {
            for (int j = n - 1; j >= 0; j--)
            {
                if (left[i] == right[j])
                    lcs[i, j] = lcs[i + 1, j + 1] + 1;
                else
                    lcs[i, j] = lcs[i + 1, j] > lcs[i, j + 1] ? lcs[i + 1, j] : lcs[i, j + 1];
            }
        }

        // Backtrack to produce diff
        var result = new List<DiffEntry>();
        int li = 0, ri = 0;
        while (li < m && ri < n)
        {
            if (left[li] == right[ri])
            {
                result.Add(new DiffEntry
                {
                    Type = DiffType.Common,
                    LeftLine = left[li],
                    RightLine = right[ri],
                    Category = ExtractCategory(left[li])
                });
                li++;
                ri++;
            }
            else if (lcs[li + 1, ri] >= lcs[li, ri + 1])
            {
                result.Add(new DiffEntry
                {
                    Type = DiffType.LeftOnly,
                    LeftLine = left[li],
                    Category = ExtractCategory(left[li])
                });
                li++;
            }
            else
            {
                result.Add(new DiffEntry
                {
                    Type = DiffType.RightOnly,
                    RightLine = right[ri],
                    Category = ExtractCategory(right[ri])
                });
                ri++;
            }
        }

        while (li < m)
        {
            result.Add(new DiffEntry
            {
                Type = DiffType.LeftOnly,
                LeftLine = left[li],
                Category = ExtractCategory(left[li])
            });
            li++;
        }

        while (ri < n)
        {
            result.Add(new DiffEntry
            {
                Type = DiffType.RightOnly,
                RightLine = right[ri],
                Category = ExtractCategory(right[ri])
            });
            ri++;
        }

        return result;
    }

    private static List<DiffEntry> SimpleDiff(string[] left, string[] right)
    {
        // Hash-based: build set of right lines, compare
        var rightSet = new HashSet<string>();
        for (int i = 0; i < right.Length; i++)
            rightSet.Add(right[i]);

        var leftSet = new HashSet<string>();
        for (int i = 0; i < left.Length; i++)
            leftSet.Add(left[i]);

        var result = new List<DiffEntry>();

        // Process left lines
        for (int i = 0; i < left.Length; i++)
        {
            if (rightSet.Contains(left[i]))
            {
                result.Add(new DiffEntry
                {
                    Type = DiffType.Common,
                    LeftLine = left[i],
                    RightLine = left[i],
                    Category = ExtractCategory(left[i])
                });
            }
            else
            {
                result.Add(new DiffEntry
                {
                    Type = DiffType.LeftOnly,
                    LeftLine = left[i],
                    Category = ExtractCategory(left[i])
                });
            }
        }

        // Add right-only lines
        for (int i = 0; i < right.Length; i++)
        {
            if (!leftSet.Contains(right[i]))
            {
                result.Add(new DiffEntry
                {
                    Type = DiffType.RightOnly,
                    RightLine = right[i],
                    Category = ExtractCategory(right[i])
                });
            }
        }

        return result;
    }
}
