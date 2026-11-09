using UnityEditor;
using UnityEngine;
using SeteVidas.Core;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// Aplica automaticamente as configurações de pixel art (skill Pixel Art) a todo PNG
    /// importado dentro de Assets/_Game/Art/: Point, sem compressão, PPU 16, sem mipmaps.
    /// Personagens e inimigos recebem pivot nos pés; tiles e UI, pivot central.
    /// Arquivos .aseprite usam o importador próprio: ajuste PPU 16 nele (ver Docs/Arte/decisoes.md).
    /// </summary>
    public class PixelArtImportPostprocessor : AssetPostprocessor
    {
        const string ArtRoot = "Assets/_Game/Art/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot)) return;
            var importer = (TextureImporter)assetImporter;

            // Configuração inicial só na primeira importação (preserva ajustes manuais depois).
            if (importer.importSettingsMissing)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                bool isCharacter = assetPath.StartsWith(ArtRoot + "Characters/") ||
                                   assetPath.StartsWith(ArtRoot + "Enemies/");
                settings.spriteAlignment = (int)(isCharacter ? SpriteAlignment.BottomCenter : SpriteAlignment.Center);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
            }

            // Regras de pixel art: sempre aplicadas.
            importer.spritePixelsPerUnit = GameConstants.PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
        }
    }
}
