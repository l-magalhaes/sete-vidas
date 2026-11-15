using System.Collections.Generic;
using SeteVidas.Dialogue;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Catálogo de todas as placas de lore do jogo (F5 da Floresta, 14/11/2026). O Diário do inventário lista as placas
    /// lidas; antes ele só achava as da cena aberta (<see cref="LoreSign.All"/>), e a Floresta tem uma cena por região:
    /// a placa lida nas Raízes sumia do Diário no Prédio Soterrado. Com o catálogo, toda placa lida aparece, de qualquer
    /// cena. Gerado pelos menus da Fortaleza (13) e da Floresta (21) em <c>Resources/LoreBook.asset</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "Sete Vidas/Catálogo de placas", fileName = "LoreBook")]
    public class LoreBook : ScriptableObject
    {
        public const string ResourcePath = "LoreBook";

        [System.Serializable]
        public class Entry
        {
            [Tooltip("ScriptId do texto (a marca do save é lido_ + key)")]
            public string key;
            [Tooltip("Título no Diário (o nome da placa na cena)")]
            public string title;
            [Tooltip("Capítulo do Diário (1 = Cidadela, 2 = Floresta)")]
            public int chapter = 1;
            [Tooltip("Ordem dentro do capítulo (a posição x no mundo: a ordem do caminho)")]
            public float order;
            public DialogueDefinition text;
        }

        public List<Entry> entries = new List<Entry>();

        static LoreBook s_loaded;
        static bool s_tried;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_loaded = null; s_tried = false; } // Play sem recarregar o domínio

        /// <summary>O catálogo em Resources (null se ainda não foi gerado).</summary>
        public static LoreBook Active
        {
            get
            {
                if (!s_tried) { s_tried = true; s_loaded = Resources.Load<LoreBook>(ResourcePath); }
                return s_loaded;
            }
        }

        public Entry Find(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            foreach (var e in entries) if (e != null && e.key == key) return e;
            return null;
        }

        /// <summary>Acrescenta ou atualiza a entrada da chave (usado pelos menus de montagem).</summary>
        public void Put(string key, string title, int chapter, float order, DialogueDefinition text)
        {
            var e = Find(key);
            if (e == null) { e = new Entry { key = key }; entries.Add(e); }
            e.title = title; e.chapter = chapter; e.order = order; e.text = text;
        }
    }
}
