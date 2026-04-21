using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class DemandLineChart : VisualElement {

    public struct ChartLine {
        public List<float> DataPoints;
        public Color32 LineColor;
        public string Label;
    }

    private static readonly Color32[] LinePalette = {
        new(66, 133, 244, 255),
        new(234, 67, 53, 255),
        new(52, 168, 83, 255),
        new(251, 188, 4, 255),
        new(171, 71, 188, 255),
        new(255, 112, 67, 255),
        new(0, 172, 193, 255),
        new(124, 179, 66, 255),
        new(244, 143, 177, 255),
        new(158, 157, 36, 255),
        new(121, 134, 203, 255),
        new(255, 183, 77, 255),
    };

    private const float LeftPad = 40f;
    private const float BottomPad = 24f;
    private const float TopPad = 8f;
    private const float RightPad = 8f;
    private const int GridLineCount = 5;
    private const int MarkerInterval = 30;
    private const float MarkerRadius = 3f;
    private const float DataLineWidth = 2f;
    private const float GridLineWidth = 1f;

    private static readonly Color GridColor = new Color(0.235f, 0.235f, 0.235f, 0.314f);

    private static readonly string[] YLabels = { "0", "25", "50", "75", "100" };
    private static readonly string[] XLabels = { "Today", "+3mo", "+6mo", "+9mo", "+12mo" };

    private List<ChartLine> _lines;
    private float _minY;
    private float _maxY;

    private readonly Label[] _yAxisLabels;
    private readonly Label[] _xAxisLabels;

    public DemandLineChart() {
        AddToClassList("demand-line-chart");
        generateVisualContent += OnGenerateVisualContent;

        _yAxisLabels = new Label[GridLineCount];
        for (int i = 0; i < GridLineCount; i++) {
            var label = new Label(YLabels[i]);
            label.AddToClassList("demand-chart-y-label");
            label.style.position = Position.Absolute;
            label.style.left = 0;
            label.style.width = LeftPad - 4f;
            label.style.unityTextAlign = TextAnchor.MiddleRight;
            label.style.fontSize = 10;
            label.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            label.pickingMode = PickingMode.Ignore;
            Add(label);
            _yAxisLabels[i] = label;
        }

        _xAxisLabels = new Label[XLabels.Length];
        for (int i = 0; i < XLabels.Length; i++) {
            var label = new Label(XLabels[i]);
            label.AddToClassList("demand-chart-x-label");
            label.style.position = Position.Absolute;
            label.style.bottom = 0;
            label.style.height = BottomPad;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.fontSize = 10;
            label.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            label.pickingMode = PickingMode.Ignore;
            Add(label);
            _xAxisLabels[i] = label;
        }

        RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }

    public static Color32 GetLineColor(int index) => LinePalette[index % LinePalette.Length];

    public void SetData(List<ChartLine> lines, float minY, float maxY) {
        _lines = lines;
        _minY = minY;
        _maxY = maxY;
        UpdateLabelPositions();
        MarkDirtyRepaint();
    }

    public void ClearData() {
        _lines = null;
        MarkDirtyRepaint();
    }

    private void OnGeometryChanged(GeometryChangedEvent evt) {
        UpdateLabelPositions();
    }

    private void UpdateLabelPositions() {
        float totalHeight = resolvedStyle.height;
        float totalWidth = resolvedStyle.width;
        if (float.IsNaN(totalHeight) || float.IsNaN(totalWidth)) return;
        if (totalHeight <= 0f || totalWidth <= 0f) return;

        float chartHeight = totalHeight - TopPad - BottomPad;
        float chartWidth = totalWidth - LeftPad - RightPad;

        for (int i = 0; i < GridLineCount; i++) {
            float fraction = i / (float)(GridLineCount - 1);
            float y = TopPad + chartHeight - fraction * chartHeight;
            _yAxisLabels[i].style.top = y - 7f;
        }

        for (int i = 0; i < _xAxisLabels.Length; i++) {
            float fraction = i / (float)(_xAxisLabels.Length - 1);
            float x = LeftPad + fraction * chartWidth;
            _xAxisLabels[i].style.left = x - 20f;
            _xAxisLabels[i].style.width = 40f;
        }
    }

    private void OnGenerateVisualContent(MeshGenerationContext ctx) {
        if (_lines == null || _lines.Count == 0) return;

        float totalWidth = contentRect.width;
        float totalHeight = contentRect.height;
        if (totalWidth <= 0f || totalHeight <= 0f) return;

        var painter = ctx.painter2D;
        float chartLeft = LeftPad;
        float chartTop = TopPad;
        float chartWidth = totalWidth - LeftPad - RightPad;
        float chartHeight = totalHeight - TopPad - BottomPad;
        float range = _maxY - _minY;
        if (range <= 0f) range = 1f;

        DrawGridLines(painter, chartLeft, chartTop, chartWidth, chartHeight);
        DrawDataLines(painter, chartLeft, chartTop, chartWidth, chartHeight, range);
        DrawMarkers(painter, chartLeft, chartTop, chartWidth, chartHeight, range);
    }

    private void DrawGridLines(Painter2D painter, float chartLeft, float chartTop, float chartWidth, float chartHeight) {
        painter.strokeColor = GridColor;
        painter.lineWidth = GridLineWidth;

        for (int i = 0; i < GridLineCount; i++) {
            float fraction = i / (float)(GridLineCount - 1);
            float y = chartTop + chartHeight - fraction * chartHeight;
            painter.BeginPath();
            painter.MoveTo(new Vector2(chartLeft, y));
            painter.LineTo(new Vector2(chartLeft + chartWidth, y));
            painter.Stroke();
        }
    }

    private void DrawDataLines(Painter2D painter, float chartLeft, float chartTop, float chartWidth, float chartHeight, float range) {
        int lineCount = _lines.Count;
        for (int li = 0; li < lineCount; li++) {
            var line = _lines[li];
            if (line.DataPoints == null || line.DataPoints.Count == 0) continue;

            int pointCount = line.DataPoints.Count;
            float stepX = pointCount > 1 ? chartWidth / (pointCount - 1) : 0f;

            painter.strokeColor = line.LineColor;
            painter.lineWidth = DataLineWidth;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;
            painter.BeginPath();

            for (int i = 0; i < pointCount; i++) {
                float x = chartLeft + i * stepX;
                float normalizedY = (line.DataPoints[i] - _minY) / range;
                float y = chartTop + chartHeight - normalizedY * chartHeight;

                if (i == 0)
                    painter.MoveTo(new Vector2(x, y));
                else
                    painter.LineTo(new Vector2(x, y));
            }

            painter.Stroke();
        }
    }

    private void DrawMarkers(Painter2D painter, float chartLeft, float chartTop, float chartWidth, float chartHeight, float range) {
        int lineCount = _lines.Count;
        for (int li = 0; li < lineCount; li++) {
            var line = _lines[li];
            if (line.DataPoints == null || line.DataPoints.Count == 0) continue;

            int pointCount = line.DataPoints.Count;
            float stepX = pointCount > 1 ? chartWidth / (pointCount - 1) : 0f;
            painter.fillColor = line.LineColor;

            for (int i = 0; i < pointCount; i += MarkerInterval) {
                float x = chartLeft + i * stepX;
                float normalizedY = (line.DataPoints[i] - _minY) / range;
                float y = chartTop + chartHeight - normalizedY * chartHeight;

                painter.BeginPath();
                painter.Arc(new Vector2(x, y), MarkerRadius, 0f, 360f);
                painter.Fill();
            }

            int lastIndex = pointCount - 1;
            if (lastIndex % MarkerInterval != 0) {
                float x = chartLeft + lastIndex * stepX;
                float normalizedY = (line.DataPoints[lastIndex] - _minY) / range;
                float y = chartTop + chartHeight - normalizedY * chartHeight;

                painter.BeginPath();
                painter.Arc(new Vector2(x, y), MarkerRadius, 0f, 360f);
                painter.Fill();
            }
        }
    }
}
