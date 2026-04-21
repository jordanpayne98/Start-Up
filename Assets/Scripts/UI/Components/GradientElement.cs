using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class GradientElement : VisualElement
{

    private static readonly CustomStyleProperty<Color> GradientFromProp =
        new CustomStyleProperty<Color>("--gradient-from");
    private static readonly CustomStyleProperty<Color> GradientToProp =
        new CustomStyleProperty<Color>("--gradient-to");

    private Color _from = new Color(1f, 1f, 1f, 0.04f);
    private Color _to   = new Color(0f, 0f, 0f, 0.06f);

    // Pre-allocated vertex array — never allocate inside generateVisualContent
    private readonly Vertex[] _verts = new Vertex[4];
    private readonly ushort[] _indices = new ushort[] { 0, 1, 2, 1, 2, 3 };

    public GradientElement() {
        style.position = Position.Absolute;
        style.left = 0;
        style.top = 0;
        style.right = 0;
        style.bottom = 0;
        pickingMode = PickingMode.Ignore;

        RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        generateVisualContent += GenerateGradient;
    }

    private void OnCustomStyleResolved(CustomStyleResolvedEvent evt) {
        bool dirty = false;

        if (evt.customStyle.TryGetValue(GradientFromProp, out Color from)) {
            _from = from;
            dirty = true;
        }
        if (evt.customStyle.TryGetValue(GradientToProp, out Color to)) {
            _to = to;
            dirty = true;
        }

        if (dirty) MarkDirtyRepaint();
    }

    private void GenerateGradient(MeshGenerationContext mgc) {
        Rect r = contentRect;
        if (r.width <= 0 || r.height <= 0) return;

        var mesh = mgc.Allocate(4, 6);

        _verts[0].position = new Vector3(r.xMin, r.yMin, Vertex.nearZ);
        _verts[1].position = new Vector3(r.xMax, r.yMin, Vertex.nearZ);
        _verts[2].position = new Vector3(r.xMin, r.yMax, Vertex.nearZ);
        _verts[3].position = new Vector3(r.xMax, r.yMax, Vertex.nearZ);

        _verts[0].tint = _from;
        _verts[1].tint = _from;
        _verts[2].tint = _to;
        _verts[3].tint = _to;

        mesh.SetAllVertices(_verts);
        mesh.SetAllIndices(_indices);
    }
}
