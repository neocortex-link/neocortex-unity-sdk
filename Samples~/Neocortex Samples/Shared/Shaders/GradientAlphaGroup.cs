using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Put this on the ScrollRect root (or Viewport) — NOT on Content.
/// Fades everything under it from topAlpha (top edge of THIS rect)
/// to bottomAlpha (bottom edge) by baking alpha into vertex colors.
/// Works with ScrollRects: items rebake as they scroll, and items
/// spawned at runtime anywhere under this rect are picked up automatically.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class GradientAlphaGroup : MonoBehaviour
{
    [Range(0, 1)] public float topAlpha = 0f;
    [Range(0, 1)] public float bottomAlpha = 1f;

    private RectTransform _rect;
    private float _lastTop = -1f, _lastBottom = -1f;

    public RectTransform Rect => _rect != null ? _rect : (_rect = (RectTransform)transform);

    private void OnEnable()
    {
        AttachToChildren();
        DirtyAll();
    }

    private void OnDisable()
    {
        foreach (var el in GetComponentsInChildren<GradientAlphaElement>(true))
            SafeDestroy(el);
        DirtyAll(); // rebuild children without the fade
    }

    private void OnRectTransformDimensionsChange()
    {
        if (isActiveAndEnabled) DirtyAll();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (isActiveAndEnabled) DirtyAll();
    }
#endif

    private void LateUpdate()
    {
        // Pick up Graphics spawned at runtime anywhere under this rect
        // (OnTransformChildrenChanged only fires for direct children,
        // so we can't rely on it for ScrollRect content items).
        AttachToChildren();

        // Alpha sliders changed
        if (!Mathf.Approximately(_lastTop, topAlpha) ||
            !Mathf.Approximately(_lastBottom, bottomAlpha))
        {
            DirtyAll();
        }
    }

    private void AttachToChildren()
    {
        foreach (var g in GetComponentsInChildren<Graphic>(true))
        {
            var el = g.GetComponent<GradientAlphaElement>();
            if (el == null)
            {
                el = g.gameObject.AddComponent<GradientAlphaElement>();
                // Invisible in the Inspector, never serialized into scene/builds
                el.hideFlags = HideFlags.HideInInspector | HideFlags.DontSave;
            }
            el.group = this;
        }
    }

    public void DirtyAll()
    {
        _lastTop = topAlpha;
        _lastBottom = bottomAlpha;
        foreach (var g in GetComponentsInChildren<Graphic>(true))
            g.SetVerticesDirty();
    }

    internal static void SafeDestroy(Object o)
    {
        if (o == null) return;

        if (Application.isPlaying)
        {
            Destroy(o); // deferred to end of frame, always safe
            return;
        }

#if UNITY_EDITOR
        // DestroyImmediate is illegal while a GameObject is (de)activating,
        // so defer it by one editor tick.
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (o != null) DestroyImmediate(o);
        };
#endif
    }
}

/// <summary>
/// Managed entirely by GradientAlphaGroup — never add manually.
/// Rebakes its Graphic whenever it MOVES RELATIVE TO THE PANEL —
/// including when a ScrollRect moves its parent Content (which does
/// not touch this transform's own local position or hasChanged flag).
/// </summary>
[ExecuteAlways]
public class GradientAlphaElement : BaseMeshEffect
{
    [System.NonSerialized] public GradientAlphaGroup group;

    private Graphic _graphic;
    private Vector2 _lastPanelPos;
    private bool _baked;

    // Orphan check in Start, not OnEnable: AddComponent fires OnEnable
    // before the group assigns itself, so checking there would kill
    // freshly created elements.
    protected override void Start()
    {
        base.Start();
        if (group == null)
            GradientAlphaGroup.SafeDestroy(this);
    }

    private void LateUpdate()
    {
        if (group == null || !group.isActiveAndEnabled) return;

        // Position of this element in the PANEL's local space — this changes
        // when scrolling, even though our own transform is untouched.
        Vector2 p = group.Rect.InverseTransformPoint(transform.position);

        if (!_baked || (p - _lastPanelPos).sqrMagnitude > 0.0001f)
        {
            _lastPanelPos = p;
            _baked = true;

            if (_graphic == null) _graphic = GetComponent<Graphic>();
            if (_graphic != null) _graphic.SetVerticesDirty();
        }
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || group == null || !group.isActiveAndEnabled)
            return;

        RectTransform panel = group.Rect;
        Rect r = panel.rect;
        UIVertex v = default;

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref v, i);

            // vertex position -> world -> panel local space
            Vector3 world = transform.TransformPoint(v.position);
            float localY = panel.InverseTransformPoint(world).y;

            // 0 at bottom edge of panel, 1 at top edge
            float t = Mathf.InverseLerp(r.yMin, r.yMax, localY);
            float a = Mathf.Lerp(group.bottomAlpha, group.topAlpha, t);

            v.color.a = (byte)Mathf.RoundToInt(v.color.a * a);
            vh.SetUIVertex(v, i);
        }
    }
}