namespace SeteVidas.Core
{
    /// <summary>
    /// Valores fixos do projeto (GDD §14 + decisões de arte [PROPOSTA] da Fase 0).
    /// Valores de gameplay ajustáveis NÃO entram aqui: eles vivem em ScriptableObjects.
    /// </summary>
    public static class GameConstants
    {
        // Pixel art
        public const int PixelsPerUnit = 16;     // 1 tile de 16 px = 1 unidade Unity
        public const int TileSize = 16;
        public const int ReferenceWidth = 480;   // ×4 = 1920
        public const int ReferenceHeight = 270;  // ×4 = 1080

        // Vida (GDD §14: cada coração = 2 hits)
        public const int HitsPerHeart = 2;
        public const int MaxHearts = 7;

        // Nomes das layers de física
        public static class Layers
        {
            public const string Player = "Player";
            public const string Enemy = "Enemy";
            public const string Ground = "Ground";
            public const string Climbable = "Climbable";
            public const string Breakable = "Breakable";
            public const string Hazard = "Hazard";
            public const string Interactable = "Interactable";

            /// <summary>Ordem e índices (8 a 14) usados pelo setup automático.</summary>
            public static readonly string[] All =
            {
                Player, Enemy, Ground, Climbable, Breakable, Hazard, Interactable
            };

            public const int FirstIndex = 8;
        }

        // Cenas
        public const string MainMenuScenePath = "Assets/_Game/Scenes/MainMenu/MainMenu.unity";
        public const string PrologueScenePath = "Assets/_Game/Scenes/Prologue/Prologue.unity";
        public const string PlaygroundScenePath = "Assets/_Game/Scenes/Dev/Playground.unity";
        public const string FortressScenePath = "Assets/_Game/Scenes/World/Fortress_MVP.unity";
    }
}
