using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[AddComponentMenu("Utils/Cam Name Feedback Editor")]
public class CamNameFeedbackEditor : MonoBehaviour
{
    [Header("Label")]
    [Tooltip("Se vazio, usa o nome do GameObject.")]
    public string overrideText = "";
    [Tooltip("Offset do label em relação à posição do objeto (em unidades do mundo).")]
    public Vector3 labelOffset = Vector3.up * 0.5f;

    [Header("Aparência")]
    public Color textColor = Color.white;
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.5f);
    [Range(10, 32)] public int fontSize = 14;
    public FontStyle fontStyle = FontStyle.Bold;

    [Header("Opções")]
    [Tooltip("Exibir o label também durante o Play Mode.")]
    public bool showInPlayMode = true;
    [Tooltip("Exibir o label enquanto no Editor (fora do Play Mode).")]
    public bool showInEditor = true;

    [Header("Culling")]
    [Tooltip("Só mostra o rótulo se estiver dentro do enquadramento da Scene View.")]
    public bool onlyWhenVisibleInSceneView = true;
    [Tooltip("Oculta o rótulo além desta distância da câmera da Scene View (0 = sem limite).")]
    public float maxDistance = 0f;

#if UNITY_EDITOR
    private GUIStyle _style;
    private Texture2D _bgTex;

    private void EnsureStyle()
    {
        if (_style == null)
        {
            _style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = fontSize,
                fontStyle = fontStyle,
                normal = { textColor = textColor }
            };
        }
        else
        {
            _style.fontSize = fontSize;
            _style.fontStyle = fontStyle;
            _style.normal.textColor = textColor;
            _style.alignment = TextAnchor.MiddleCenter;
        }

        if (_bgTex == null)
        {
            _bgTex = new Texture2D(1, 1);
            _bgTex.hideFlags = HideFlags.HideAndDontSave;
        }
        _bgTex.SetPixel(0, 0, backgroundColor);
        _bgTex.Apply();
    }

    private void DrawLabel()
    {
        if (Application.isPlaying)
        {
            if (!showInPlayMode)
                return;
        }
        else
        {
            if (!showInEditor)
                return;
        }

        EnsureStyle();

        string text = string.IsNullOrEmpty(overrideText) ? gameObject.name : overrideText;
        Vector3 worldPos = transform.position + labelOffset;

        // Culling baseado na câmera da Scene View
        var sceneView = SceneView.currentDrawingSceneView ?? SceneView.lastActiveSceneView;
        var cam = sceneView != null ? sceneView.camera : null;
        if (cam == null)
            return;

        if (onlyWhenVisibleInSceneView)
        {
            var vp = cam.WorldToViewportPoint(worldPos);
            // z <= 0: atrás da câmera; x/y fora de 0..1: fora do enquadramento
            if (vp.z <= 0f || vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f)
                return;
        }

        if (maxDistance > 0f)
        {
            float dist = Vector3.Distance(cam.transform.position, worldPos);
            if (dist > maxDistance)
                return;
        }

        // Desenha um retângulo de fundo e o texto em coordenadas de GUI da SceneView
        Handles.BeginGUI();
        Vector2 guiPos = HandleUtility.WorldToGUIPoint(worldPos);

        Vector2 size = _style.CalcSize(new GUIContent(text));
        Rect rect = new Rect(guiPos.x - size.x / 2f, guiPos.y - size.y, size.x, size.y);

        // Fundo com padding leve
        const float pad = 4f;
        Rect bgRect = new Rect(rect.x - pad, rect.y - pad, rect.width + pad * 2f, rect.height + pad * 2f);
        GUI.DrawTexture(bgRect, _bgTex);

        GUI.Label(rect, text, _style);
        Handles.EndGUI();
    }
#endif

    // Sempre visível (mesmo sem estar selecionado)
    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        DrawLabel();
#endif
    }
}
