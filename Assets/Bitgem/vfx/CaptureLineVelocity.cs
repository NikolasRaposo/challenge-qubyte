using UnityEngine;

[ExecuteAlways, RequireComponent(typeof(Camera))]
public class CaptureLineVelocity : MonoBehaviour
{
    public Material velocityMat;
    public RenderTexture velocityRT;
    [HideInInspector] public Vector4 startEnd;
    Vector4 _prevStartEnd;
    [SerializeField]
    Camera _cam;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        if (velocityRT == null)
            velocityRT = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat);
        _prevStartEnd = startEnd;
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        velocityMat.SetVector("_StartEnd",     startEnd);
        velocityMat.SetVector("_PrevStartEnd", _prevStartEnd);
        velocityMat.SetFloat("_DeltaTime",     Time.deltaTime);

        Graphics.Blit(null, velocityRT, velocityMat);
        _prevStartEnd = startEnd;

        Graphics.Blit(src, dest);
    }
}