using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class ShadowElement : VisualElement
{

    public float OffsetX      { get; set; } = 2f;
    public float OffsetY      { get; set; } = 4f;
    public float Spread       { get; set; } = 6f;
    public Color ShadowColor  { get; set; } = new Color(0f, 0f, 0f, 0.35f);
    public float CornerRadius { get; set; } = 8f;

    // 3 layers × 4 verts = 12 verts, 3 layers × 6 indices = 18 indices
    private readonly Vertex[] _verts   = new Vertex[12];
    private readonly ushort[] _indices = new ushort[]
    {
        0,  1,  2,  1,  2,  3,   // layer 0
        4,  5,  6,  5,  6,  7,   // layer 1
        8,  9, 10,  9, 10, 11    // layer 2
    };

    public ShadowElement() {
        style.position = Position.Absolute;
        style.left = 0;
        style.top = 0;
        style.right = 0;
        style.bottom = 0;
        pickingMode = PickingMode.Ignore;

        generateVisualContent += GenerateShadow;
    }

    private void GenerateShadow(MeshGenerationContext mgc) {
        Rect r = contentRect;
        if (r.width <= 0 || r.height <= 0) return;

        var mesh = mgc.Allocate(12, 18);

        for (int i = 0; i < 3; i++) {
            float t       = (float)i / 3f;        // 0, 0.33, 0.66
            float expand  = Spread * (1f - t);     // outermost = most expanded
            float alpha   = ShadowColor.a * t;     // outermost = most transparent

            Color layerColor = new Color(ShadowColor.r, ShadowColor.g, ShadowColor.b, alpha);

            float x0 = r.xMin + OffsetX - expand;
            float y0 = r.yMin + OffsetY - expand;
            float x1 = r.xMax + OffsetX + expand;
            float y1 = r.yMax + OffsetY + expand;

            int vi = i * 4;
            _verts[vi + 0].position = new Vector3(x0, y0, Vertex.nearZ);
            _verts[vi + 1].position = new Vector3(x1, y0, Vertex.nearZ);
            _verts[vi + 2].position = new Vector3(x0, y1, Vertex.nearZ);
            _verts[vi + 3].position = new Vector3(x1, y1, Vertex.nearZ);

            _verts[vi + 0].tint = layerColor;
            _verts[vi + 1].tint = layerColor;
            _verts[vi + 2].tint = layerColor;
            _verts[vi + 3].tint = layerColor;
        }

        mesh.SetAllVertices(_verts);
        mesh.SetAllIndices(_indices);
    }
}
