using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class RadarChartElement : VisualElement {

    public struct AxisData {
        public string Name;
        public float NormalizedValue;
        public int DeltaDirection;
        public int RawValue;
        public Color LabelColor;
    }

    private const int MaxAxes = 9;
    private const int GridRings = 4;
    private const float LabelPadding = 48f;
    private const float VertexDotRadius = 3f;
    private const float StrokeWidth = 2f;

    private static readonly Color FillColor = new Color(0.3f, 0.79f, 0.69f, 0.2f);
    private static readonly Color StrokeColor = new Color(0.3f, 0.79f, 0.69f, 0.85f);
    private static readonly Color GridColor = new Color(1f, 1f, 1f, 0.08f);
    private static readonly Color AxisColor = new Color(1f, 1f, 1f, 0.05f);

    private readonly AxisData[] _axes = new AxisData[MaxAxes];
    private int _axisCount;
    private readonly Label[] _labels = new Label[MaxAxes];

    public RadarChartElement() {
        generateVisualContent += OnGenerateVisualContent;

        for (int i = 0; i < MaxAxes; i++) {
            var label = new Label();
            label.style.position = Position.Absolute;
            label.style.fontSize = 10;
            label.pickingMode = PickingMode.Ignore;
            Add(label);
            _labels[i] = label;
        }

        RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }

    public void SetData(List<AxisData> axes) {
        _axisCount = Mathf.Min(axes.Count, MaxAxes);
        for (int i = 0; i < _axisCount; i++) {
            _axes[i] = axes[i];
        }
        for (int i = 0; i < MaxAxes; i++) {
            _labels[i].style.display = i < _axisCount ? DisplayStyle.Flex : DisplayStyle.None;
        }
        PositionLabels();
        MarkDirtyRepaint();
        RegisterCallbackOnce<GeometryChangedEvent>(OnGeometryChangedOnce);
    }

    private void OnGeometryChanged(GeometryChangedEvent evt) {
        PositionLabels();
    }

    private void OnGeometryChangedOnce(GeometryChangedEvent evt) {
        PositionLabels();
        MarkDirtyRepaint();
    }

    private void PositionLabels() {
        if (_axisCount < 3) return;
        float totalWidth = resolvedStyle.width;
        float totalHeight = resolvedStyle.height;
        if (float.IsNaN(totalWidth) || float.IsNaN(totalHeight)) return;
        if (totalWidth <= 0f || totalHeight <= 0f) return;

        float cx = totalWidth * 0.5f;
        float cy = totalHeight * 0.5f;
        float radius = Mathf.Min(cx, cy) - LabelPadding;
        float angleStep = 2f * Mathf.PI / _axisCount;

        for (int i = 0; i < _axisCount; i++) {
            float angle = -Mathf.PI * 0.5f + i * angleStep;
            float cosA = Mathf.Cos(angle);
            float lx = cx + cosA * (radius + LabelPadding * 0.6f);
            float ly = cy + Mathf.Sin(angle) * (radius + LabelPadding * 0.6f);

            var label = _labels[i];
            label.style.left = lx;
            label.style.top = ly;

            if (cosA < -0.1f) {
                label.style.unityTextAlign = TextAnchor.MiddleRight;
                label.style.translate = new Translate(Length.Percent(-100), Length.Percent(-50));
            } else if (cosA > 0.1f) {
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
                label.style.translate = new Translate(Length.Percent(0), Length.Percent(-50));
            } else {
                label.style.unityTextAlign = TextAnchor.MiddleCenter;
                label.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
            }

            var axis = _axes[i];
            label.style.color = axis.LabelColor;
            if (axis.DeltaDirection > 0) {
                label.text = axis.Name + " " + axis.RawValue + " \u25B2";
            } else if (axis.DeltaDirection < 0) {
                label.text = axis.Name + " " + axis.RawValue + " \u25BC";
            } else {
                label.text = axis.Name + " " + axis.RawValue;
            }
        }
    }

    private void OnGenerateVisualContent(MeshGenerationContext ctx) {
        if (_axisCount < 3) return;
        float totalWidth = contentRect.width;
        float totalHeight = contentRect.height;
        if (totalWidth <= 0f || totalHeight <= 0f) return;

        float cx = totalWidth * 0.5f;
        float cy = totalHeight * 0.5f;
        float radius = Mathf.Min(cx, cy) - LabelPadding;
        if (radius <= 0f) return;

        float angleStep = 2f * Mathf.PI / _axisCount;
        var painter = ctx.painter2D;

        DrawGridRings(painter, cx, cy, radius, angleStep);
        DrawAxisLines(painter, cx, cy, radius, angleStep);
        DrawDataPolygon(painter, cx, cy, radius, angleStep);
        DrawVertexDots(painter, cx, cy, radius, angleStep);
    }

    private void DrawGridRings(Painter2D painter, float cx, float cy, float radius, float angleStep) {
        painter.strokeColor = GridColor;
        painter.lineWidth = 1f;

        for (int ring = 1; ring <= GridRings; ring++) {
            float r = radius * (ring / (float)GridRings);
            painter.BeginPath();
            for (int i = 0; i < _axisCount; i++) {
                float angle = -Mathf.PI * 0.5f + i * angleStep;
                float x = cx + Mathf.Cos(angle) * r;
                float y = cy + Mathf.Sin(angle) * r;
                if (i == 0) painter.MoveTo(new Vector2(x, y));
                else painter.LineTo(new Vector2(x, y));
            }
            painter.ClosePath();
            painter.Stroke();
        }
    }

    private void DrawAxisLines(Painter2D painter, float cx, float cy, float radius, float angleStep) {
        painter.strokeColor = AxisColor;
        painter.lineWidth = 1f;

        for (int i = 0; i < _axisCount; i++) {
            float angle = -Mathf.PI * 0.5f + i * angleStep;
            float x = cx + Mathf.Cos(angle) * radius;
            float y = cy + Mathf.Sin(angle) * radius;
            painter.BeginPath();
            painter.MoveTo(new Vector2(cx, cy));
            painter.LineTo(new Vector2(x, y));
            painter.Stroke();
        }
    }

    private void DrawDataPolygon(Painter2D painter, float cx, float cy, float radius, float angleStep) {
        painter.BeginPath();
        for (int i = 0; i < _axisCount; i++) {
            float angle = -Mathf.PI * 0.5f + i * angleStep;
            float r = Mathf.Clamp01(_axes[i].NormalizedValue) * radius;
            float x = cx + Mathf.Cos(angle) * r;
            float y = cy + Mathf.Sin(angle) * r;
            if (i == 0) painter.MoveTo(new Vector2(x, y));
            else painter.LineTo(new Vector2(x, y));
        }
        painter.ClosePath();
        painter.fillColor = FillColor;
        painter.Fill();

        painter.BeginPath();
        for (int i = 0; i < _axisCount; i++) {
            float angle = -Mathf.PI * 0.5f + i * angleStep;
            float r = Mathf.Clamp01(_axes[i].NormalizedValue) * radius;
            float x = cx + Mathf.Cos(angle) * r;
            float y = cy + Mathf.Sin(angle) * r;
            if (i == 0) painter.MoveTo(new Vector2(x, y));
            else painter.LineTo(new Vector2(x, y));
        }
        painter.ClosePath();
        painter.strokeColor = StrokeColor;
        painter.lineWidth = StrokeWidth;
        painter.lineJoin = LineJoin.Round;
        painter.Stroke();
    }

    private void DrawVertexDots(Painter2D painter, float cx, float cy, float radius, float angleStep) {
        painter.fillColor = StrokeColor;
        for (int i = 0; i < _axisCount; i++) {
            float angle = -Mathf.PI * 0.5f + i * angleStep;
            float r = Mathf.Clamp01(_axes[i].NormalizedValue) * radius;
            float x = cx + Mathf.Cos(angle) * r;
            float y = cy + Mathf.Sin(angle) * r;
            painter.BeginPath();
            painter.Arc(new Vector2(x, y), VertexDotRadius, 0f, 360f);
            painter.Fill();
        }
    }
}
