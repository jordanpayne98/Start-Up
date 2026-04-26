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
    private const float LabelPadding = 70f;
    private const float VertexDotRadius = 3f;
    private const float StrokeWidth = 2f;

    private static readonly Color FillColor = new Color(0.3f, 0.79f, 0.69f, 0.2f);
    private static readonly Color StrokeColor = new Color(0.3f, 0.79f, 0.69f, 0.85f);
    private static readonly Color GridColor = new Color(1f, 1f, 1f, 0.08f);
    private static readonly Color AxisColor = new Color(1f, 1f, 1f, 0.05f);
    private static readonly Color AccentSuccess = new Color(0.290f, 0.871f, 0.502f);
    private static readonly Color AccentDanger = new Color(0.973f, 0.443f, 0.443f);
    private static readonly Color TextSecondary = new Color(0.627f, 0.647f, 0.686f);

    private readonly AxisData[] _axes = new AxisData[MaxAxes];
    private readonly Color[] _skillLevelColors = new Color[MaxAxes];
    private bool _useSkillLevelColors;
    private int _axisCount;
    private bool _hidden;

    private readonly VisualElement[] _labelSlots = new VisualElement[MaxAxes];
    private readonly Label[] _nameLabels = new Label[MaxAxes];
    private readonly Label[] _levelLabels = new Label[MaxAxes];

    public RadarChartElement() {
        generateVisualContent += OnGenerateVisualContent;

        for (int i = 0; i < MaxAxes; i++) {
            var slot = new VisualElement();
            slot.AddToClassList("skill-label");
            slot.style.position = Position.Absolute;
            slot.style.flexDirection = FlexDirection.Row;
            slot.pickingMode = PickingMode.Ignore;

            var name = new Label();
            name.AddToClassList("skill-label__name");
            name.style.fontSize = 11;
            name.pickingMode = PickingMode.Ignore;

            var level = new Label();
            level.AddToClassList("skill-label__level");
            level.style.fontSize = 11;
            level.pickingMode = PickingMode.Ignore;

            slot.Add(name);
            slot.Add(level);
            Add(slot);

            _labelSlots[i] = slot;
            _nameLabels[i] = name;
            _levelLabels[i] = level;
        }

        RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }

    public void SetData(List<AxisData> axes) {
        _axisCount = Mathf.Min(axes.Count, MaxAxes);
        for (int i = 0; i < _axisCount; i++) {
            _axes[i] = axes[i];
        }
        _useSkillLevelColors = false;
        for (int i = 0; i < MaxAxes; i++) {
            _labelSlots[i].style.display = i < _axisCount ? DisplayStyle.Flex : DisplayStyle.None;
        }
        _hidden = false;
        PositionLabels();
        MarkDirtyRepaint();
        RegisterCallbackOnce<GeometryChangedEvent>(OnGeometryChangedOnce);
    }

    public void SetData(RadarChartData data) {
        int count = data.Skills != null ? Mathf.Min(data.Skills.Length, MaxAxes) : 0;
        _axisCount = count;
        for (int i = 0; i < count; i++) {
            var sp = data.Skills[i];
            _axes[i] = new AxisData {
                Name = sp.SkillName,
                NormalizedValue = sp.Level / 20f,
                DeltaDirection = sp.Level > sp.PreviousLevel ? 1 : sp.Level < sp.PreviousLevel ? -1 : 0,
                RawValue = Mathf.RoundToInt(sp.Level),
                LabelColor = sp.SkillNameColor
            };
            _skillLevelColors[i] = sp.Level > sp.PreviousLevel
                ? AccentSuccess
                : sp.Level < sp.PreviousLevel
                    ? AccentDanger
                    : TextSecondary;
        }
        _useSkillLevelColors = true;
        for (int i = 0; i < MaxAxes; i++) {
            _labelSlots[i].style.display = i < count ? DisplayStyle.Flex : DisplayStyle.None;
        }
        _hidden = false;
        PositionLabels();
        MarkDirtyRepaint();
        RegisterCallbackOnce<GeometryChangedEvent>(OnGeometryChangedOnce);
    }

    public void ClearData() {
        _axisCount = 0;
        _hidden = true;
        for (int i = 0; i < MaxAxes; i++) {
            _labelSlots[i].style.display = DisplayStyle.None;
        }
        MarkDirtyRepaint();
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

            var slot = _labelSlots[i];
            slot.style.left = lx;
            slot.style.top = ly;

            bool isLeftSide = cosA < -0.1f;
            if (isLeftSide) {
                slot.style.translate = new Translate(Length.Percent(-100), Length.Percent(-50));
            } else if (cosA > 0.1f) {
                slot.style.translate = new Translate(Length.Percent(0), Length.Percent(-50));
            } else {
                slot.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
            }
            slot.style.flexDirection = FlexDirection.Row;

            var axis = _axes[i];
            _nameLabels[i].style.color = axis.LabelColor;

            if (_useSkillLevelColors) {
                if (isLeftSide) {
                    _levelLabels[i].text = axis.RawValue + " ";
                    _nameLabels[i].text = axis.Name;
                } else {
                    _nameLabels[i].text = axis.Name;
                    _levelLabels[i].text = " " + axis.RawValue;
                }
                _levelLabels[i].style.color = _skillLevelColors[i];
                _levelLabels[i].style.display = DisplayStyle.Flex;
            } else {
                string arrow = axis.DeltaDirection > 0 ? " \u25B2" : axis.DeltaDirection < 0 ? " \u25BC" : "";
                _nameLabels[i].text = axis.Name + " " + axis.RawValue + arrow;
                _levelLabels[i].text = string.Empty;
                _levelLabels[i].style.display = DisplayStyle.None;
            }
        }
    }

    private void OnGenerateVisualContent(MeshGenerationContext ctx) {
        if (_hidden || _axisCount < 3) return;
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
