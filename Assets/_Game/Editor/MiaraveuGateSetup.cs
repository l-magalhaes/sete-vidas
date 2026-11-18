using System.IO;
using SeteVidas.Dialogue;
using SeteVidas.World;
using SeteVidas.World.Scenery;
using UnityEditor;
using UnityEngine;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// A saída da fase na cabeceira leste (R5, 18/11/2026, pedido do usuário): o portão em ruínas com a placa
    /// "Bem-vindo a Miaravéu" (<see cref="MiaraveuGateArt"/>), a conversa do fim (uma vez) e o amanhecer no fundo
    /// (<see cref="DawnSky"/>). Roda junto do "Floresta: gerar as cenas" (objetos "miaraveu_gate", "dawn" e o "trigger"
    /// <see cref="EndDialogue"/> do ForestLayout).
    /// </summary>
    public static class MiaraveuGateSetup
    {
        public const string EndDialogue = "D-MIARAVEU-PORTAO";
        const string ArtPath = "Assets/_Game/Art/Environments/Forest/Placeholder/portao_miaraveu.png";

        /// <summary>A conversa ao passar pelo portão (Docs/Narrativa/floresta-roteiro.md, D-MIARAVEU-PORTAO).</summary>
        public static DialogueDefinition CreateDialogue()
        {
            var (cat, _) = DialogueSetup.Speakers();
            var d = DialogueSetup.Conversation(EndDialogue, true, 1,
                (cat, "ironico", "\"Bem-vindo a Miaravéu.\" A placa ainda acredita nisso."),
                (cat, "serio", "Ninguém conserta este portão faz tempo. Do que será que a cidade estava se protegendo?"),
                (null, "", "Fim da Floresta Silenciosa. A estrada para Miaravéu continua... em breve."));
            AssetDatabase.SaveAssets();
            return d;
        }

        /// <summary>O portão (os pés no chão em <c>o.Y</c>, o meio em <c>o.X</c>), atrás do gato. Só desenho: não bloqueia.</summary>
        public static void Gate(Transform parent, ForestLayout.Obj o)
        {
            var go = new GameObject(o.Name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(o.X, o.Y, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GateSprite();
            sr.sortingOrder = -4;
        }

        /// <summary>O amanhecer: acende da ponta leste da ponte até um pouco dentro da R5 (<c>o.X</c> = o começo da sala).</summary>
        public static void Dawn(Transform parent, ForestLayout.Obj o)
        {
            var go = new GameObject("Amanhecer (R5)");
            go.transform.SetParent(parent, false);
            go.AddComponent<DawnSky>().Configure(o.X - 14f, o.X + 4f);
        }

        /// <summary>Grava o desenho (sempre de novo: a arte é do código) e importa como sprite de pixel art, o pivô embaixo.</summary>
        static Sprite GateSprite()
        {
            var px = MiaraveuGateArt.Pixels();
            var tex = new Texture2D(MiaraveuGateArt.W, MiaraveuGateArt.H, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply(false);
            File.WriteAllBytes(Path.GetFullPath(ArtPath), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(ArtPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(ArtPath);
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 16f;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath);
        }
    }
}
