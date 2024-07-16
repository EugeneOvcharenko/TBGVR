using UnityEngine;
using UnityEditor;
using System.IO;

public class SetMaterialTextures : Editor
{
    [MenuItem("Tools/Set Material Textures")]
    private static void SetTextures()
    {
        // Specify the path to the folder containing the materials and textures
        string folderPath = "Assets/Tropicana/Content"; // Replace with your folder path

        // Get all material asset paths in the folder
        string[] materialPaths = Directory.GetFiles(folderPath, "MI_*.mat", SearchOption.AllDirectories);

        foreach (string materialPath in materialPaths)
        {
            // Load the material
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material != null && material.GetTexture("_MainTex") == null)
            {
                // Get the material name
                string materialName = Path.GetFileNameWithoutExtension(materialPath);

                // Construct the corresponding texture name
                string textureName = "T_" + materialName.Substring(3);

                // Construct the texture path
                string texturePath = Path.Combine(folderPath, textureName + ".png"); // Assuming the texture is a PNG

                // Load the texture
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

                if(texture == null)
                {
                    texturePath = Path.Combine(folderPath, textureName + "_BaseColor.png"); // Assuming the texture is a PNG
                    texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                }

                if (texture != null)
                {
                    // Set the texture as the main texture of the material
                    material.SetTexture("_MainTex", texture);

                    // Save the material asset
                    EditorUtility.SetDirty(material);
                }
                else
                {
                    Debug.LogWarning($"Texture not found for material {materialName}: {texturePath}");
                }
            }
            else
            {
                Debug.LogWarning($"Material not found: {materialPath}");
            }
        }

        // Save all changes to assets
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
