namespace SeteVidas.World.Scenery
{
    /// <summary>
    /// Camadas do cenário e a ordem de desenho padrão de cada uma (plano de assets, seção 3.5).
    /// Os valores antigos continuam: fundo de tiles -10, enfeites -5, jogo 0, primeiro plano 10.
    /// </summary>
    public static class SceneryLayers
    {
        public const string Sky = "ceu";
        public const string Far = "bg_longe";
        public const string Mid = "bg_medio";
        public const string Near = "bg_perto";
        public const string BackWall = "parede_fundo";
        public const string DecorBehind = "decor_atras";
        public const string Gameplay = "jogo";
        /// <summary>Por cima dos tiles e atrás dos personagens (5): tufos, seixos, placas embutidas no chão.</summary>
        public const string TerrainDetail = "terreno_detalhe";
        public const string DecorFront = "decor_frente";
        public const string Foreground = "fg";

        public static readonly string[] All = { Sky, Far, Mid, Near, BackWall, DecorBehind, Gameplay, TerrainDetail, DecorFront, Foreground };

        /// <summary>Ordem de desenho padrão da camada. Camada desconhecida = a dos enfeites (-5).</summary>
        public static int DefaultOrder(string layer)
        {
            switch (layer)
            {
                case Sky: return -100;
                case Far: return -90;
                case Mid: return -80;
                case Near: return -60;
                case BackWall: return -10;
                case DecorBehind: return -5;
                case Gameplay: return 0;
                case TerrainDetail: return 1;
                case DecorFront: return 10;
                case Foreground: return 20;
                default: return -5;
            }
        }

        public static bool IsKnown(string layer) { return System.Array.IndexOf(All, layer) >= 0; }
    }
}
