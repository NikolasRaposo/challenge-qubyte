using System.IO;
using UnityEditor;
using UnityEngine;
namespace EditorTools {
    /// <summary>
    /// A utility component to generate and save a Mask Map texture by combining up to four separate textures.
    /// Each channel of the output texture (RGBA) is populated by the red channel of an input texture.
    /// This is commonly used in PBR workflows where Metallic, AO, Detail, and Smoothness maps are packed together.
    /// </summary>
    public class MaskMapGenerator : MonoBehaviour {
        [Header("Input Textures")]
        [Tooltip("The texture to use for the Red channel (typically Metallic).")]
        public Texture2D metallicTexture;
        
        [Tooltip("The texture to use for the Green channel (typically Ambient Occlusion).")]
        public Texture2D aoTexture;

        [Tooltip("The texture to use for the Blue channel (typically a Detail Mask).")]
        public Texture2D detailMaskTexture;

        [Tooltip("The texture to use for the Alpha channel (typically Smoothness or Roughness).")]
        public Texture2D smoothingTexture;

        [Header("Channel Adjustments")]
        [Tooltip("Controls the intensity of the metallic map in the final texture.")]
        [Range(0f, 1f)]
        public float metallicAdjustment = 1f;

        [Tooltip("Controls the intensity of the ambient occlusion map in the final texture.")]
        [Range(0f, 1f)]
        public float aoAdjustment = 1f;

        [Tooltip("Controls the intensity of the detail mask in the final texture.")]
        [Range(0f, 1f)]
        public float detailMaskAdjustment = 1f;

        [Tooltip("Controls the intensity of the smoothness map in the final texture.")]
        [Range(0f, 1f)]
        public float smoothingAdjustment = 1f;

        /// <summary>
        /// Generates the mask map by combining the provided textures and saves it as a PNG file in the project's Assets folder.
        /// </summary>
        /// <returns>The generated Texture2D object.</returns>
        public Texture2D GenerateMaskMap() {
            // Define the resolution of the output texture. Consider making these public variables for more control.
            const int width = 2048;
            const int height = 2048;

            // Create a new blank texture to store the combined mask map.
            Texture2D maskMap = new Texture2D(width, height);

            // Create an array to hold the pixel color data.
            Color[] pixels = new Color[width * height];

            // Loop through each pixel of the new texture.
            for (int i = 0; i < pixels.Length; i++) {
                // Calculate the x and y coordinates for sampling the input textures.
                int x = i % width;
                int y = i / height;

                // Create a new Color object for the current pixel.
                Color pixelColor = new Color();

                // Populate each channel of the new color with data from the corresponding input texture.
                // It uses the red channel '.r' of the input texture.
                // If an input texture is not assigned, the channel value defaults to 0 (black).
                pixelColor.r = metallicTexture ? metallicTexture.GetPixel(x, y).r * metallicAdjustment : 0f;
                pixelColor.g = aoTexture ? aoTexture.GetPixel(x, y).r * aoAdjustment : 0f;
                pixelColor.b = detailMaskTexture ? detailMaskTexture.GetPixel(x, y).r * detailMaskAdjustment : 0f;
                pixelColor.a = smoothingTexture ? smoothingTexture.GetPixel(x, y).r * smoothingAdjustment : 0f;

                // Assign the combined color to the pixel array.
                pixels[i] = pixelColor;
            }

            // Apply all the pixel color changes to the mask map texture.
            maskMap.SetPixels(pixels);
            maskMap.Apply();

            // --- Save the texture to a file ---
            // Define the file path within the Unity project.
            const string path = "Assets/GeneratedMaskMap.png";
            // Encode the texture data into PNG format.
            byte[] bytes = maskMap.EncodeToPNG();
            // Write the byte array to a file at the specified path.
            File.WriteAllBytes(path, bytes);
            // Refresh the Unity Asset Database to make the new texture visible in the editor.
            AssetDatabase.Refresh();

            // Log a confirmation message to the console.
            Debug.Log($"Mask map saved to: {path}");

            // Return the newly created texture.
            return maskMap;
        }
    }
}