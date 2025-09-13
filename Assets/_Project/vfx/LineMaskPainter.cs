using UnityEngine;
[ExecuteAlways]
public class LineMaskPainter : MonoBehaviour
{
    public Material maskMat;
    RenderTexture rt, tmp;

    public float thickness = 0.005f;
    public Vector2 A;
    public Vector2 B;
    void OnEnable()
    {
        rt = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.R8);
        tmp = new RenderTexture(rt.descriptor);
    }
    void OnDisable()
    {
        rt.Release(); tmp.Release();
    }
    void Update()
    {
        maskMat.SetVector("_StartEnd", new Vector4(A.x,A.y,B.x,B.y));
        maskMat.SetFloat("_Thickness", thickness);

        // ping‐pong copy + paint
        Graphics.Blit(rt, tmp);
        Graphics.Blit(tmp, rt, maskMat);
    }
    // exponha rt para seu shader de composição
    public RenderTexture GetMask() => rt;
}