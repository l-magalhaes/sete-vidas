using SeteVidas.Core;
using SeteVidas.Dialogue;
using SeteVidas.Enemies.Boss;
using SeteVidas.Player;
using SeteVidas.Save;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Arena do Golden. Ao entrar, a grade de trás fecha, o Golden fala e a luta começa. Vencida, as duas grades
    /// abrem (a da frente leva ao tomo) e a vitória vai para o save. Morrer ou descansar no meio da luta
    /// reinicia a arena (grade aberta, Golden com vida cheia).
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class BossArena : MonoBehaviour
    {
        public const string DefeatedFlag = "chefe_golden_derrotado";

        [SerializeField] GoldenBoss boss;
        [SerializeField] ArenaGate entryGate;
        [SerializeField] ArenaGate exitGate;
        [SerializeField] DialogueDefinition introDialogue;
        [SerializeField] DialogueDefinition defeatDialogue;

        bool _started;

        public bool Started => _started;
        public GoldenBoss Boss => boss;

        void Start()
        {
            if (SaveManager.Data.HasFlag(DefeatedFlag))
            {
                if (boss != null && !SaveManager.Data.HasFlag(CursedTome.TakenFlag)) boss.SetDefeatedPose();
                if (entryGate != null) entryGate.SetImmediate(false);
                if (exitGate != null) exitGate.SetImmediate(false);
            }
            else
            {
                if (entryGate != null) entryGate.SetImmediate(false);
                if (exitGate != null) exitGate.SetImmediate(true);
            }
        }

        void OnEnable()
        {
            if (boss != null) boss.Defeated += OnBossDefeated;
            GameEvents.WorldReset += OnWorldReset;
        }

        void OnDisable()
        {
            if (boss != null) boss.Defeated -= OnBossDefeated;
            GameEvents.WorldReset -= OnWorldReset;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerHealth>();
            if (player != null) StartEncounter(player);
        }

        /// <summary>Fecha a grade, conversa de entrada e começa a luta (também usado pelos testes).</summary>
        public void StartEncounter(PlayerHealth player)
        {
            if (_started || boss == null || boss.IsDefeated || player == null || player.IsDead) return;
            _started = true;
            if (entryGate != null) entryGate.Close();

            var runner = DialogueRunner.Active;
            var input = player.GetComponent<PlayerInputReader>();
            // Sem interface (ou outra conversa aberta): a luta começa direto
            if (runner == null || introDialogue == null || !runner.Play(introDialogue, input, boss.BeginFight))
                boss.BeginFight();
        }

        void OnBossDefeated()
        {
            var data = SaveManager.Data;
            data.SetFlag(DefeatedFlag);
            SaveManager.Save(); // a vitória nunca se perde
            if (entryGate != null) entryGate.Open();
            if (exitGate != null) exitGate.Open();

            var runner = DialogueRunner.Active;
            var player = PlayerHealth.Active;
            if (runner != null && defeatDialogue != null && player != null)
                runner.Play(defeatDialogue, player.GetComponent<PlayerInputReader>());
        }

        void OnWorldReset()
        {
            if (boss == null || boss.IsDefeated) return;
            _started = false;
            boss.ResetToStart();
            if (entryGate != null) entryGate.Open();
        }
    }
}
