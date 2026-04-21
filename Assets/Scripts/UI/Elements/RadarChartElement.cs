using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Elements {
    public class RadarChartElement : VisualElement {
        public int AxisCount {
            get => _axisCount;
            set { _axisCount = Mathf.Max(3, value); RebuildLabels(); RecalculateVertices(); MarkDirtyRepaint(); }
        }

        public string[] AxisLabels {
            get => _axisLabels;
            set { _axisLabels = value; UpdateLabelText(); MarkDirtyRepaint(); }
        }

        public Color[] AxisColors {
            get => _axisColors;
            set { _axisColors = value; MarkDirtyRepaint(); }
        }

        public float[] MarketProfile {
            get => _marketProfile;
            set { _marketProfile = value; MarkDirtyRepaint(); }
        }

        public float[] ProductProfile {
            get => _productProfile;
            set { _productProfile = value; MarkDirtyRepaint(); }
        }

        public Color MarketFillColor {
            get => _marketFillColor;
            set { _marketFillColor = value; MarkDirtyRepaint(); }
        }

        public Color ProductFillColor {
            get => _productFillColor;
            set { _productFillColor = value; MarkDirtyRepaint(); }
        }

        public Color GridColor {
            get => _gridColor;
            set { _gridColor = value; MarkDirtyRepaint(); }
        }

        public float ChartRadius {
            get => _chartRadius;
            set { _chartRadius = value; RecalculateVertices(); MarkDirtyRepaint(); }
        }

        public int GridRings {
            get => _gridRings;
            set { _gridRings = Mathf.Max(1, value); MarkDirtyRepaint(); }
        }

        private int _axisCount = 3;
        private string[] _axisLabels;
        private Color[] _axisColors;
        private float[] _marketProfile;
        private float[] _productProfile;
        private Color _marketFillColor = new Color(0.3f, 0.7f, 1f, 0.25f);
        private Color _productFillColor = new Color(0.2f, 1f, 0.4f, 0.6f);
        private Color _gridColor = new Color(1f, 1f, 1f, 0.15f);
        private float _chartRadius = 80f;
        private int _gridRings = 3;

        private Vector2[] _vertexDirections;
        private Label[] _labelElements;
        private bool _labelsBuilt;

        public RadarChartElement() {
            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            style.minWidth = 180f;
            style.minHeight = 180f;
            RebuildLabels();
            RecalculateVertices();
        }

        public void SetMarketProfile(float[] values) {
            _marketProfile = values;
            MarkDirtyRepaint();
        }

        public void SetProductProfile(float[] values) {
            _productProfile = values;
            MarkDirtyRepaint();
        }

        public void SetAxisLabels(string[] labels) {
            _axisLabels = labels;
            UpdateLabelText();
            MarkDirtyRepaint();
        }

        public void SetAxisCount(int count) {
            _axisCount = Mathf.Max(3, count);
            RebuildLabels();
            RecalculateVertices();
            MarkDirtyRepaint();
        }

        private void RecalculateVertices() {
            _vertexDirections = new Vector2[_axisCount];
            for (int i = 0; i < _axisCount; i++) {
                float angle = (i * 2f * Mathf.PI / _axisCount) - (Mathf.PI * 0.5f);
                _vertexDirections[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }
        }

        private void RebuildLabels() {
            if (_labelElements != null) {
                for (int i = 0; i < _labelElements.Length; i++) {
                    if (_labelElements[i] != null && _labelElements[i].parent == this) {
                        Remove(_labelElements[i]);
                    }
                }
            }

            _labelElements = new Label[_axisCount];
            for (int i = 0; i < _axisCount; i++) {
                var label = new Label();
                label.style.position = Position.Absolute;
                label.style.fontSize = 10f;
                label.style.color = new Color(0.85f, 0.85f, 0.85f, 1f);
                label.style.unityTextAlign = TextAnchor.MiddleCenter;
                label.style.width = 64f;
                label.pickingMode = PickingMode.Ignore;
                if (_axisLabels != null && i < _axisLabels.Length) {
                    label.text = _axisLabels[i];
                }
                _labelElements[i] = label;
                Add(label);
            }

            _labelsBuilt = true;
        }

        private void UpdateLabelText() {
            if (_labelElements == null) return;
            for (int i = 0; i < _labelElements.Length && i < _axisCount; i++) {
                if (_axisLabels != null && i < _axisLabels.Length) {
                    _labelElements[i].text = _axisLabels[i];
                } else {
                    _labelElements[i].text = string.Empty;
                }
            }
        }

        private void PositionLabels(float cx, float cy) {
            if (_labelElements == null || _vertexDirections == null) return;
            float labelOffset = _chartRadius + 20f;
            int count = Mathf.Min(_labelElements.Length, _vertexDirections.Length);
            for (int i = 0; i < count; i++) {
                var label = _labelElements[i];
                if (label == null) continue;
                float lx = cx + _vertexDirections[i].x * labelOffset - 32f;
                float ly = cy + _vertexDirections[i].y * labelOffset - 8f;
                label.style.left = lx;
                label.style.top = ly;
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent evt) {
            if (!_labelsBuilt) return;
            float w = resolvedStyle.width;
            float h = resolvedStyle.height;
            if (w < 1f || h < 1f) return;
            PositionLabels(w * 0.5f, h * 0.5f);
            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext ctx) {
            float w = resolvedStyle.width;
            float h = resolvedStyle.height;
            if (w < 50f || h < 50f) return;

            float cx = w * 0.5f;
            float cy = h * 0.5f;

            PositionLabels(cx, cy);

            var painter = ctx.painter2D;

            DrawGrid(painter, cx, cy);
            DrawAxisLines(painter, cx, cy);
            DrawProfile(painter, cx, cy, _marketProfile, _marketFillColor, false);

            var productOutlineColor = _productFillColor;
            var productFillColor = new Color(_productFillColor.r, _productFillColor.g, _productFillColor.b, _productFillColor.a * 0.3f);
            DrawProfileFilled(painter, cx, cy, _productProfile, productFillColor, productOutlineColor);
        }

        private void DrawGrid(Painter2D painter, float cx, float cy) {
            if (_vertexDirections == null || _vertexDirections.Length < 3) return;
            Color ringColor = _gridColor;

            for (int ring = 1; ring <= _gridRings; ring++) {
                float fraction = (float)ring / _gridRings;
                float r = _chartRadius * fraction;
                painter.strokeColor = ringColor;
                painter.lineWidth = 1f;
                painter.BeginPath();
                for (int i = 0; i < _axisCount; i++) {
                    float x = cx + _vertexDirections[i].x * r;
                    float y = cy + _vertexDirections[i].y * r;
                    if (i == 0) painter.MoveTo(new Vector2(x, y));
                    else painter.LineTo(new Vector2(x, y));
                }
                painter.ClosePath();
                painter.Stroke();
            }
        }

        private void DrawAxisLines(Painter2D painter, float cx, float cy) {
            if (_vertexDirections == null || _vertexDirections.Length < 3) return;
            painter.strokeColor = _gridColor;
            painter.lineWidth = 1f;
            for (int i = 0; i < _axisCount; i++) {
                float vx = cx + _vertexDirections[i].x * _chartRadius;
                float vy = cy + _vertexDirections[i].y * _chartRadius;
                painter.BeginPath();
                painter.MoveTo(new Vector2(cx, cy));
                painter.LineTo(new Vector2(vx, vy));
                painter.Stroke();
            }
        }

        private void DrawProfile(Painter2D painter, float cx, float cy, float[] profile, Color fillColor, bool strokeOnly) {
            if (profile == null || profile.Length != _axisCount || _vertexDirections == null) return;
            painter.fillColor = fillColor;
            painter.strokeColor = fillColor;
            painter.lineWidth = 1.5f;
            painter.BeginPath();
            for (int i = 0; i < _axisCount; i++) {
                float val = Mathf.Clamp01(profile[i]);
                float px = cx + _vertexDirections[i].x * _chartRadius * val;
                float py = cy + _vertexDirections[i].y * _chartRadius * val;
                if (i == 0) painter.MoveTo(new Vector2(px, py));
                else painter.LineTo(new Vector2(px, py));
            }
            painter.ClosePath();
            if (!strokeOnly) painter.Fill(FillRule.NonZero);
            painter.Stroke();
        }

        private void DrawProfileFilled(Painter2D painter, float cx, float cy, float[] profile, Color fillColor, Color strokeColor) {
            if (profile == null || profile.Length != _axisCount || _vertexDirections == null) return;
            painter.fillColor = fillColor;
            painter.BeginPath();
            for (int i = 0; i < _axisCount; i++) {
                float val = Mathf.Clamp01(profile[i]);
                float px = cx + _vertexDirections[i].x * _chartRadius * val;
                float py = cy + _vertexDirections[i].y * _chartRadius * val;
                if (i == 0) painter.MoveTo(new Vector2(px, py));
                else painter.LineTo(new Vector2(px, py));
            }
            painter.ClosePath();
            painter.Fill(FillRule.NonZero);

            painter.strokeColor = strokeColor;
            painter.lineWidth = 2f;
            painter.BeginPath();
            for (int i = 0; i < _axisCount; i++) {
                float val = Mathf.Clamp01(profile[i]);
                float px = cx + _vertexDirections[i].x * _chartRadius * val;
                float py = cy + _vertexDirections[i].y * _chartRadius * val;
                if (i == 0) painter.MoveTo(new Vector2(px, py));
                else painter.LineTo(new Vector2(px, py));
            }
            painter.ClosePath();
            painter.Stroke();
        }
    }
}
