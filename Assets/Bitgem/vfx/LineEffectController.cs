using UnityEngine;

[ExecuteAlways]
public class LineEffectController : MonoBehaviour
{
    public Material lineMaterial;

    public Vector2 startPoint;
    public Vector2 endPoint;

    public bool fireballActive = true;
    public float fireballPosition = 0.2f;
    public float fireballSize = 0.1f;

    public bool sparkActive = true;
    public float sparkDensity = 10f;
    public float sparkOffset = 0f;

    public bool explosionActive = true;
    public float explosionSize = 0.2f;

    void Update()
    {
        lineMaterial.SetVector("_StartEnd", new Vector4(startPoint.x, startPoint.y, endPoint.x, endPoint.y));

        lineMaterial.SetFloat("_FireballActive", fireballActive ? 1 : 0);
        lineMaterial.SetFloat("_FireballPos", fireballPosition);
        lineMaterial.SetFloat("_FireballSize", fireballSize);

        lineMaterial.SetFloat("_SparkActive", sparkActive ? 1 : 0);
        lineMaterial.SetFloat("_SparkDensity", sparkDensity);
        lineMaterial.SetFloat("_SparkOffset", sparkOffset);

        lineMaterial.SetFloat("_ExplosionActive", explosionActive ? 1 : 0);
        lineMaterial.SetFloat("_ExplosionSize", explosionSize);
    }
}