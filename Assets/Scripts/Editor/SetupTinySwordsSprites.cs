using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// BroTD → Setup Tiny Swords Sprites
/// Настраивает импорт всех спрайтов из папки Tiny Swords/Units:
/// - Sprite Mode = Multiple (для анимаций)
/// - Filter Mode = Point (пиксель-арт без размытия)
/// - Pixels Per Unit = 16
/// - Compression = None
/// После этого нужно нарезать в Sprite Editor (Slice → Auto).
/// </summary>
public static class SetupTinySwordsSprites
{
    private const string SpritesPath = "Assets/Sprites/Tiny Swords/Units";

    [MenuItem("BroTD/Setup Tiny Swords Sprites")]
    public static void Setup()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SpritesPath });

        int count = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            bool changed = false;

            if (importer.spriteImportMode != SpriteImportMode.Multiple)
            {
                importer.spriteImportMode = SpriteImportMode.Multiple;
                changed = true;
            }
            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                changed = true;
            }
            if (importer.spritePixelsPerUnit != 16f)
            {
                importer.spritePixelsPerUnit = 16f;
                changed = true;
            }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }

            importer.mipmapEnabled = false;

            if (changed)
            {
                importer.SaveAndReimport();
                count++;
            }
        }

        Debug.Log($"[TinySwords] Настроено {count} спрайтов из {guids.Length}. Теперь нарежь их в Sprite Editor.");
        EditorUtility.DisplayDialog(
            "Tiny Swords — импорт настроен",
            $"Настроено {count} спрайтов.\n\n" +
            "Следующий шаг:\n" +
            "1. Выдели все PNG в папке Units\n" +
            "2. Sprite Editor → Slice → Type: Automatic\n" +
            "3. Slice → Apply\n\n" +
            "После этого запусти BroTD → Create Unit Animations",
            "OK");
    }
}
