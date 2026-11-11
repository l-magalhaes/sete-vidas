using System.Collections.Generic;
using SeteVidas.World;
using SeteVidas.World.Scenery;
using UnityEditor;
using UnityEngine;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// Mundo vivo da Fortaleza (plano de assets, seções 6.1–6.3, Etapa 7): vento por área, partículas de ambiente,
    /// bichos que fogem, o cristal Primata que acende, as velas que se inclinam e o pulso das runas e das veias.
    /// Tudo só visual. Regras: no máximo 40 partículas na cena (AmbientEmitter.GlobalCap), efeitos calmos nas salas de
    /// combate e de armadilhas (só poeira roxa rara), nada no vermelho das armadilhas.
    /// </summary>
    public static class FortressLife
    {
        const string Life = "Assets/_Game/Art/Environments/Common/Life/";
        const string Plague = "Assets/_Game/Art/Environments/Necro/Plague/";
        const string Culture = "Assets/_Game/Art/Environments/Citadel/Culture/";
        const string Primates = "Assets/_Game/Art/Environments/Primates/Objects/";

        public static void Build(Transform root, Transform dressing)
        {
            const int WallWalk = FortressLayout.WallWalk, BellHall = FortressLayout.BellHall, SealFloor = FortressLayout.SealFloor;
            const int Cliff = FortressLayout.CliffTop;
            var life = new GameObject("Mundo vivo").transform;
            life.SetParent(root, false);

            // ---- vento (6.2): da montanha para a direita, mais forte no alto; dentro do castelo não venta
            Wind(life, "Vento da montanha", Rect.MinMaxRect(-20f, 40f, 40f, 130f), new Vector2(0.6f, 0f));
            Wind(life, "Vento do vale", Rect.MinMaxRect(-20f, -6f, 136f, 40f), new Vector2(0.35f, 0f));
            Wind(life, "Vento da muralha", Rect.MinMaxRect(110f, 40f, 136f, 64f), new Vector2(0.5f, 0f));
            Wind(life, "Vento da estrada", Rect.MinMaxRect(234f, -6f, 262f, 20f), new Vector2(0.25f, 0f));

            // ---- partículas (6.2)
            var dust = Sprites(Life + "env_com_fx_poeira_0.png", Life + "env_com_fx_poeira_1.png");
            var leaves = Sprites(Life + "env_com_fx_folha_0.png", Life + "env_com_fx_folha_1.png");
            var fireflies = Sprites(Life + "env_com_fx_vagalume.png");
            var plague = Sprites(Plague + "env_nec_fx_poeira.png");
            // folhas caindo das copas (1 a cada 3–5 s por tela)
            Emitter(life, "Folhas da montanha", AmbientEmitter.Kind.Leaves, Rect.MinMaxRect(-11f, Cliff + 1f, 12f, Cliff + 8f), 0.25f, 3, leaves);
            Emitter(life, "Folhas da trilha", AmbientEmitter.Kind.Leaves, Rect.MinMaxRect(26f, 1f, 38f, 6f), 0.22f, 2, leaves);
            Emitter(life, "Folhas da estrada", AmbientEmitter.Kind.Leaves, Rect.MinMaxRect(235f, 1f, 250f, 8f), 0.25f, 3, leaves);
            // vaga-lumes ao pôr do sol (3–5 por tela), baixos, sobre a grama
            Emitter(life, "Vaga-lumes da trilha", AmbientEmitter.Kind.Fireflies, Rect.MinMaxRect(14f, 0.4f, 45f, 3.5f), 0.5f, 4, fireflies);
            Emitter(life, "Vaga-lumes do pátio", AmbientEmitter.Kind.Fireflies, Rect.MinMaxRect(45f, 0.4f, 110f, 3.5f), 0.6f, 5, fireflies);
            Emitter(life, "Vaga-lumes da estrada", AmbientEmitter.Kind.Fireflies, Rect.MinMaxRect(235f, 0.4f, 250f, 3.5f), 0.5f, 4, fireflies);
            // poeira só nos feixes de luz das janelas (do peitoril até perto do chão)
            foreach (var w in FortressBackgrounds.Windows())
            {
                if (w.Kind == "seteira") continue;
                Emitter(life, "Poeira na luz: " + w.Name, AmbientEmitter.Kind.Dust, Rect.MinMaxRect(w.X - 0.9f, w.Y - 5f, w.X + 0.9f, w.Y - 1.2f), 0.35f, 4, dust);
            }
            Emitter(life, "Poeira dos fundos", AmbientEmitter.Kind.Dust, Rect.MinMaxRect(186f, SealFloor + 0.5f, 217f, SealFloor + 4.5f), 0.3f, 5, dust);
            // a praga crescendo com a altura (6.3): rara nas armadilhas (1 a cada 4 s), suspensa na câmara
            Emitter(life, "Poeira roxa das armadilhas", AmbientEmitter.Kind.PlagueDust, Rect.MinMaxRect(140f, BellHall + 1f, 176f, BellHall + 9f), 0.25f, 3, plague);
            Emitter(life, "Poeira roxa da câmara", AmbientEmitter.Kind.PlagueDust, Rect.MinMaxRect(146f, SealFloor + 0.5f, 177f, SealFloor + 8.5f), 0.6f, 8, plague);

            // ---- bichos que fogem (só visual: não fazem barulho para a IA)
            var birdRest = Sprite(Life + "env_ext_ani_passaro_0.png");
            var birdFly = Sprites(Life + "env_ext_ani_passaro_voo_0.png", Life + "env_ext_ani_passaro_voo_1.png");
            Creature(life, "Pássaro da corrida do salto", new Vector2(6.2f, Cliff), birdRest, birdFly, 4f, new Vector2(3.5f, 3f), 1.4f, 0.1f);
            Creature(life, "Pássaro do marco", new Vector2(24.9f, 0f), birdRest, birdFly, 4f, new Vector2(3.5f, 3f), 1.4f, 0.1f);
            var ratRest = Sprite(Life + "env_com_ani_rato_0.png");
            var ratRun = Sprites(Life + "env_com_ani_rato_corre_0.png", Life + "env_com_ani_rato_corre_1.png");
            // os ratos correm até o buraquinho na parede (qualquer que seja o lado do gato) e somem nele
            var ratBack = Creature(life, "Rato dos fundos", new Vector2(213.6f, SealFloor), ratRest, ratRun, 3f, new Vector2(3.2f, 0f), 0.6f, 0.08f, false);
            if (ratBack != null) ratBack.SetHideout(215.6f);
            var ratKitchen = Creature(life, "Rato da cozinha", new Vector2(196.9f, WallWalk), ratRest, ratRun, 3f, new Vector2(3.2f, 0f), 1.2f, 0.08f, false);
            if (ratKitchen != null) ratKitchen.SetHideout(200.5f);

            // ---- enfeites que reagem e pulsam (pelo id da biblioteca de cada enfeite)
            var ids = new Dictionary<string, string>();
            foreach (var item in FortressDressing.Items()) ids[item.Name] = item.AssetId;
            var crystalLit = Sprites(Primates + "env_pri_obj_cristal_aceso_0.png", Primates + "env_pri_obj_cristal_aceso_1.png", Primates + "env_pri_obj_cristal_aceso_2.png");
            var candleLean = Sprites(Culture + "env_cid_cul_vela_inclinada.png");
            int pulses = 0;
            foreach (Transform child in dressing)
            {
                string id;
                if (!ids.TryGetValue(child.name, out id)) continue;
                var sr = child.GetComponentInChildren<SpriteRenderer>();
                if (id == "env_pri_obj_cristal_apagado")
                    child.gameObject.AddComponent<ReactOnProximity>().Configure(ReactOnProximity.Mode.Swap, 3f, sr, crystalLit, 0.25f, Vector2.zero, 1f);
                else if (id == "env_cid_cul_vela")
                    child.gameObject.AddComponent<ReactOnProximity>().Configure(ReactOnProximity.Mode.Lean, 1.6f, sr, candleLean, 0.1f, Vector2.zero, 1f);
                else if (id == "env_mag_runa" || id == "env_nec_veia" || id == "env_mag_selo_porta" && child.position.y >= BellHall)   // selos: só as portas corrompidas (4 a 7)
                {
                    // runas douradas e veias roxas pulsam fora de tempo (como se brigassem); perto do tomo as runas falham
                    bool rune = id != "env_nec_veia";
                    bool failing = rune && child.position.y > SealFloor - 1f;
                    float phase = Mathf.Repeat(child.position.x * 0.137f + (rune ? 0f : 0.5f), 1f);
                    child.gameObject.AddComponent<PulseLight>().Configure(rune ? 2.6f : 2.2f, phase, 0.6f, 1f, failing);
                    pulses++;
                }
            }
            Debug.Log($"[Sete Vidas] Mundo vivo: {life.childCount} peças (vento, partículas, bichos), {pulses} pulsos.");
        }

        static void Wind(Transform parent, string name, Rect area, Vector2 wind)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<WindZone2D>().Configure(area, wind);
        }

        static void Emitter(Transform parent, string name, AmbientEmitter.Kind kind, Rect area, float rate, int max, Sprite[] sprites)
        {
            if (sprites.Length == 0) { Debug.LogWarning($"[Sete Vidas] {name}: falta a arte (rode Tools/EnvironmentSprites/gerar.ps1)"); return; }
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<AmbientEmitter>().Configure(kind, area, rate, max, sprites, kind == AmbientEmitter.Kind.Leaves ? 9 : 8);
        }

        static ReactOnProximity Creature(Transform parent, string name, Vector2 at, Sprite rest, Sprite[] flee, float radius, Vector2 velocity, float fleeTime, float frameTime, bool untilOffscreen = true)
        {
            if (rest == null || flee.Length == 0) { Debug.LogWarning($"[Sete Vidas] {name}: falta a arte (rode Tools/EnvironmentSprites/gerar.ps1)"); return null; }
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(at.x, at.y, 0f);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = new Vector3(0f, rest.rect.height / 32f, 0f);   // base no chão
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = rest;
            sr.sortingOrder = 3;                                                           // atrás do gato (5)
            var react = go.AddComponent<ReactOnProximity>();
            react.Configure(ReactOnProximity.Mode.Flee, radius, sr, flee, frameTime, velocity, fleeTime, untilOffscreen);
            return react;
        }

        static Sprite Sprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        static Sprite[] Sprites(params string[] paths)
        {
            var list = new List<Sprite>();
            foreach (var p in paths) { var s = Sprite(p); if (s != null) list.Add(s); }
            return list.ToArray();
        }
    }
}
