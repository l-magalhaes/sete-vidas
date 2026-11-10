using System;
using UnityEngine;

namespace SeteVidas.World.Scenery
{
    /// <summary>
    /// Formato do <c>manifest.json</c> de cada pasta de cenário (plano de assets, seção 3.4).
    /// Os nomes dos campos são os do JSON (em português, sem acento), por isso fogem do padrão de nomes do C#.
    /// O manifesto é a fonte de verdade dos metadados: dá para editar à mão; o importador do editor
    /// (<c>EnvLibraryImporter</c>) transforma cada entrada num <see cref="EnvAssetDefinition"/>.
    /// </summary>
    [Serializable]
    public class EnvManifest
    {
        /// <summary>Região padrão das entradas (com, ext, sol, cid, pri, nec...).</summary>
        public string regiao = "";
        public EnvManifestEntry[] assets = new EnvManifestEntry[0];

        public static EnvManifest Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new EnvManifest();
            var m = JsonUtility.FromJson<EnvManifest>(json) ?? new EnvManifest();
            if (m.assets == null) m.assets = new EnvManifestEntry[0];
            foreach (var a in m.assets)
                if (a != null && string.IsNullOrEmpty(a.regiao)) a.regiao = m.regiao;
            return m;
        }

        public static readonly string[] Supports = { "chao", "teto", "parede", "borda", "livre" };
        public static readonly string[] Functions =
            { "decorativo", "colisor", "plataforma", "interativo", "gate", "perigo", "esconderijo", "luz", "narrativo" };

        /// <summary>Problemas de uma entrada, sem olhar arquivos (o importador confere as imagens).</summary>
        public static System.Collections.Generic.List<string> Validate(EnvManifestEntry e)
        {
            var list = new System.Collections.Generic.List<string>();
            if (e == null) { list.Add("entrada vazia"); return list; }
            if (string.IsNullOrEmpty(e.id)) list.Add("sem id");
            if (string.IsNullOrEmpty(e.regiao)) list.Add("sem região");
            if (!SceneryLayers.IsKnown(e.camada))
                list.Add("camada desconhecida \"" + e.camada + "\" (válidas: " + string.Join(", ", SceneryLayers.All) + ")");
            if (Array.IndexOf(Functions, e.funcao) < 0) list.Add("função desconhecida \"" + e.funcao + "\"");
            if ((e.sprites == null || e.sprites.Length == 0) && string.IsNullOrEmpty(e.sprites_prefixo)) list.Add("sem sprites (nem sprites_prefixo)");
            if (e.categoria == "plx" && (e.fundo == null || e.fundo.paralaxe == null || e.fundo.paralaxe.Length < 2))
                list.Add("fundo (plx) sem paralaxe [x, y]");
            if (e.animacao != null && e.animacao.quadros != null && e.animacao.quadros.Length > 0 && e.animacao.fps <= 0f)
                list.Add("animação com quadros mas sem fps");
            string apoio = e.posicionamento != null ? e.posicionamento.apoio : "";
            if (Array.IndexOf(Supports, apoio) < 0) list.Add("apoio desconhecido \"" + apoio + "\"");
            return list;
        }
    }

    [Serializable]
    public class EnvManifestEntry
    {
        public string id = "";
        public string nome = "";
        public string regiao = "";
        public string categoria = "";
        public string funcao = "decorativo";
        public string material = "";
        public string epoca = "";
        public int[] tamanho_px = new int[0];
        /// <summary>Uma imagem por variação, relativas à pasta do manifesto.</summary>
        public string[] sprites = new string[0];
        /// <summary>
        /// Alternativa a <see cref="sprites"/> para famílias grandes (tilesets): todos os PNGs que começam com este
        /// caminho, relativo à pasta do manifesto, em ordem alfabética. Ex.: "Terrain/tile_ext_solo_".
        /// </summary>
        public string sprites_prefixo = "";
        public bool espelhar;
        public string camada = "decor_atras";
        /// <summary>Ordem de desenho; 0 = a padrão da camada (ver <see cref="SceneryLayers"/>).</summary>
        public int sorting;
        public string colisor = "nenhum";
        public EnvAnimation animacao = new EnvAnimation();
        public string[] paleta = new string[0];
        public EnvPlacement posicionamento = new EnvPlacement();
        public EnvBackground fundo = new EnvBackground();
        public string leitura = "";
        public string[] tags = new string[0];
    }

    [Serializable]
    public class EnvAnimation
    {
        public string nome = "";
        /// <summary>Quadros, relativos à pasta do manifesto. Vazio = sem animação.</summary>
        public string[] quadros = new string[0];
        /// <summary>
        /// Animação POR VARIAÇÃO (árvores): cada sprite de <c>sprites</c> termina em "_0.png" e tem os irmãos
        /// "_1.png" … até este número de quadros. Quando > 0, <see cref="quadros"/> é ignorado.
        /// </summary>
        public int quadros_por_variacao;
        public float fps;
        public int amplitude_px;
    }

    [Serializable]
    public class EnvPlacement
    {
        /// <summary>chao (apoia a base), teto (pendura pelo topo), parede, borda, livre (centro no ponto).</summary>
        public string apoio = "livre";
        public string inclinacao_max = "";
        public int distancia_minima_mesmo_id;
        /// <summary>Deslocamento aleatório (por semente) máximo, em pixels, em x.</summary>
        public int deslocamento_px;
        public string[] nao_perto_de = new string[0];
        public string[] perto_de = new string[0];
        public string densidade = "";
        public string[] zonas = new string[0];
    }

    /// <summary>Só para a categoria plx (fundos): paralaxe, repetição e coerência de luz (plano, seção 7).</summary>
    [Serializable]
    public class EnvBackground
    {
        /// <summary>Quanto a camada fica parada no mundo [x, y] (0 = presa à câmera, 1 = parada).</summary>
        public float[] paralaxe = new float[0];
        /// <summary>Posição da camada quando a câmera está em cima dela [x, y], em tiles.</summary>
        public float[] ancora = new float[0];
        public bool repete;
        /// <summary>
        /// Cor (hex) que continua a camada para BAIXO, sem fim: com paralaxe, a câmera desce mais que a altura do
        /// desenho (a queda de 60 tiles) e a borda de baixo entraria na tela, mostrando o céu atrás. Vazio = sem.
        /// </summary>
        public string preencher_abaixo = "";
        public float[] altitude_tiles = new float[0];
        public string[] estado_do_ceu = new string[0];
        public string lado_da_luz = "";
        public float valor_max;
        public string[] conjuntos = new string[0];
    }
}
