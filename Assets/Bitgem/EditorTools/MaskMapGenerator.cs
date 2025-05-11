using UnityEngine;
using System.Collections;
using System.IO;
using UnityEditor;

public class MaskMapGenerator : MonoBehaviour
{
    public Texture2D metallicTexture;
    public Texture2D aoTexture;
    public Texture2D detailMaskTexture;
    public Texture2D smoothingTexture;

    [Range(0f, 1f)]
    public float metallicAdjustment = 1f;

    [Range(0f, 1f)]
    public float aoAdjustment = 1f;

    [Range(0f, 1f)]
    public float detailMaskAdjustment = 1f;

    [Range(0f, 1f)]
    public float smoothingAdjustment = 1f;

    public Texture2D GenerateMaskMap()
    {
        int width = 2048; // Set desired width
        int height = 2048; // Set desired height
        Texture2D maskMap = new Texture2D(width, height);

        Color[] pixels = new Color[width * height];

        for (int i = 0; i < pixels.Length; i++)
        {
            Color pixelColor = new Color();

            pixelColor.r = metallicTexture != null ? metallicTexture.GetPixel(i % metallicTexture.width, i / metallicTexture.width).r * metallicAdjustment : 0f;
            pixelColor.g = aoTexture != null ? aoTexture.GetPixel(i % aoTexture.width, i / aoTexture.width).r * aoAdjustment : 0f;
            pixelColor.b = detailMaskTexture != null ? detailMaskTexture.GetPixel(i % detailMaskTexture.width, i / detailMaskTexture.width).r * detailMaskAdjustment : 0f;
            pixelColor.a = smoothingTexture != null ? smoothingTexture.GetPixel(i % smoothingTexture.width, i / smoothingTexture.width).r * smoothingAdjustment : 0f;

            pixels[i] = pixelColor;
        }

        maskMap.SetPixels(pixels);
        maskMap.Apply();

        // Save the texture to the project
        string path = "Assets/GeneratedMaskMap.png";
        byte[] bytes = maskMap.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        AssetDatabase.Refresh();

        Debug.Log($"Mask map saved to: {path}");

        return maskMap;
    }
}