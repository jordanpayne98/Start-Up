#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;

public class MarketReadDebugPanel
{
    private VisualElement _root;
    private VisualElement _tableContainer;
    private Label _deltaLabel;
    private bool _initialized;

    public void Show(MarketReadResolver.MarketReadDebugData data, VisualElement panelRoot)
    {
        if (!_initialized)
        {
            Build(panelRoot);
            _initialized = true;
        }
        _root.style.display = DisplayStyle.Flex;
        PopulateTable(data);
        _deltaLabel.text = data.LastDelta.HasDelta
            ? "Last delta: " + data.LastDelta.Message
            : "Last delta: none";
    }

    public void Hide()
    {
        if (_root != null)
            _root.style.display = DisplayStyle.None;
    }

    private void Build(VisualElement panelRoot)
    {
        _root = new VisualElement();
        _root.style.position = Position.Absolute;
        _root.style.top = 8;
        _root.style.right = 8;
        _root.style.width = 480;
        _root.style.backgroundColor = new Color(0.08f, 0.08f, 0.10f, 0.96f);
        _root.style.borderTopLeftRadius = 6;
        _root.style.borderTopRightRadius = 6;
        _root.style.borderBottomLeftRadius = 6;
        _root.style.borderBottomRightRadius = 6;
        _root.style.paddingTop = 8;
        _root.style.paddingBottom = 8;
        _root.style.paddingLeft = 10;
        _root.style.paddingRight = 10;
        _root.style.display = DisplayStyle.None;

        var titleLbl = new Label("MARKET READ DEBUG");
        titleLbl.style.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        titleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLbl.style.marginBottom = 6;
        _root.Add(titleLbl);

        var header = BuildRow("ReadType", "Str", "Tier", "Conf", "Supp", "Vis", isHeader: true);
        _root.Add(header);

        _tableContainer = new VisualElement();
        _root.Add(_tableContainer);

        _deltaLabel = new Label();
        _deltaLabel.style.color = new Color(0.90f, 0.75f, 0.30f, 1f);
        _deltaLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
        _deltaLabel.style.fontSize = 10;
        _deltaLabel.style.marginTop = 6;
        _root.Add(_deltaLabel);

        panelRoot.Add(_root);
    }

    private void PopulateTable(MarketReadResolver.MarketReadDebugData data)
    {
        _tableContainer.Clear();
        AddCandidateRow(data.Candidate0);
        AddCandidateRow(data.Candidate1);
        AddCandidateRow(data.Candidate2);
        AddCandidateRow(data.Candidate3);
        AddCandidateRow(data.Candidate4);
        AddCandidateRow(data.Candidate5);
        AddCandidateRow(data.Candidate6);
        AddCandidateRow(data.Candidate7);
        AddCandidateRow(data.Candidate8);
        AddCandidateRow(data.Candidate9);
    }

    private void AddCandidateRow(MarketReadResolver.CandidateReadDebug c)
    {
        string tier = c.StrengthTier == 3 ? "Strong" : c.StrengthTier == 2 ? "Mod" : c.StrengthTier == 1 ? "Mild" : "-";
        var row = BuildRow(c.Type.ToString(), c.AbsStrength.ToString(), tier,
            c.Confidence.ToString(), c.Suppressed ? "Y" : "N", c.Visible ? "Y" : "N", isHeader: false);

        bool highlight = !c.Suppressed && c.AbsStrength >= 25;
        if (highlight)
        {
            foreach (var child in row.Children())
                child.style.color = new Color(0.75f, 0.95f, 0.70f, 1f);
        }
        _tableContainer.Add(row);
    }

    private static VisualElement BuildRow(string col0, string col1, string col2, string col3, string col4, string col5, bool isHeader)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.marginBottom = 1;

        Color textColor = isHeader ? new Color(0.60f, 0.60f, 0.60f, 1f) : new Color(0.78f, 0.78f, 0.78f, 1f);
        FontStyle style = isHeader ? FontStyle.Bold : FontStyle.Normal;

        AddCell(row, col0, 170, textColor, style);
        AddCell(row, col1, 38, textColor, style);
        AddCell(row, col2, 55, textColor, style);
        AddCell(row, col3, 38, textColor, style);
        AddCell(row, col4, 38, textColor, style);
        AddCell(row, col5, 38, textColor, style);

        return row;
    }

    private static void AddCell(VisualElement row, string text, float width, Color color, FontStyle style)
    {
        var lbl = new Label(text);
        lbl.style.width = width;
        lbl.style.color = color;
        lbl.style.unityFontStyleAndWeight = style;
        lbl.style.fontSize = 10;
        lbl.style.overflow = Overflow.Hidden;
        row.Add(lbl);
    }
}
#endif
