using SeteVidas.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SeteVidas.UI
{
    /// <summary>
    /// Tela inicial (spec: Docs/Design/tela-inicial.md): Novo jogo (Fortaleza), Continuar, Área de testes (Playground) e Sair.
    /// Continuar só fica ativo com save no disco e mostra um resumo (corações, classes, santuário); ao entrar,
    /// o gato começa no último santuário (o PlayerHealth cuida disso). Novo jogo com save existente pede confirmação.
    /// Teclado (W/S, setas, Enter/Espaço/E), controle e mouse.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        // Campo renomeado (antes "newGameScene"): telas iniciais antigas guardavam "Playground" nele; esse valor fica ignorado
        [Tooltip("Cena do novo jogo (a Fortaleza)")]
        [SerializeField] string newGameSceneName = "Fortress_MVP";
        [Tooltip("Prólogo do novo jogo (a leitura do contrato). Se a cena não estiver na build, vai direto para a Fortaleza")]
        [SerializeField] string prologueScene = "Prologue";
        [Tooltip("Cena da área de testes (save separado)")]
        [SerializeField] string testScene = "Playground";

        [Header("Botões")]
        [SerializeField] Button newGameButton;
        [SerializeField] Button continueButton;
        [SerializeField] Button quitButton;
        [SerializeField] Button testAreaButton;
        [Tooltip("Opções: liga e desliga as dicas do tutorial (melhoria 12)")]
        [SerializeField] Button optionsButton;
        [SerializeField] Text optionsLabel;
        [Tooltip("Resumo do save embaixo do Continuar")]
        [SerializeField] Text continueInfo;

        [Header("Confirmação do novo jogo")]
        [SerializeField] GameObject confirmPanel;
        [SerializeField] Button confirmYes;
        [SerializeField] Button confirmNo;

        [Header("Visual")]
        [Tooltip("Seta \"▶\" ao lado do botão selecionado")]
        [SerializeField] RectTransform cursor;

        public string NewGameScene => newGameSceneName;
        public bool CanContinue => continueButton != null && continueButton.interactable;

        void Awake()
        {
            Time.timeScale = 1f; // pode chegar aqui com o jogo pausado
            SaveManager.Slot = SaveManager.MainSlot; // o Continuar e o resumo são do jogo, não da área de testes
            if (newGameButton != null) newGameButton.onClick.AddListener(OnNewGame);
            if (continueButton != null) continueButton.onClick.AddListener(Continue);
            if (quitButton != null) quitButton.onClick.AddListener(Quit);
            if (testAreaButton != null) testAreaButton.onClick.AddListener(OpenTestArea);
            if (optionsButton != null) optionsButton.onClick.AddListener(ToggleHints);
            if (confirmYes != null) confirmYes.onClick.AddListener(StartNewGame);
            if (confirmNo != null) confirmNo.onClick.AddListener(CloseConfirm);
        }

        void Start()
        {
            if (confirmPanel != null) confirmPanel.SetActive(false);
            // a Área de testes só aparece em build de desenvolvimento (e no editor)
            if (testAreaButton != null) testAreaButton.gameObject.SetActive(Debug.isDebugBuild);
            Refresh();
            Select(CanContinue ? continueButton : newGameButton);
        }

        /// <summary>Atualiza o Continuar a partir do save no disco.</summary>
        public void Refresh()
        {
            bool has = SaveManager.HasSaveFile;
            if (continueButton != null) continueButton.interactable = has;
            // o resumo do save embaixo do Continuar saiu (pedido do usuário, 12/11/2026): some também nas cenas antigas
            if (continueInfo != null) continueInfo.gameObject.SetActive(false);
            if (optionsLabel != null) optionsLabel.text = "Dicas: " + (TutorialScroll.HintsDisabled ? "desligadas" : "ligadas");
        }

        GameObject _lastSelected;

        void LateUpdate()
        {
            // Seta ao lado do botão selecionado (teclado/controle) ou sob o mouse
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected != _lastSelected && selected != null && _lastSelected != null) SeteVidas.Audio.Sfx.Play("ui_cursor");
            if (selected != null) _lastSelected = selected;
            if (cursor == null) return;
            cursor.gameObject.SetActive(selected != null);
            if (selected == null) return;
            var rt = (RectTransform)selected.transform;
            cursor.position = rt.position;
            cursor.anchoredPosition += new Vector2(-rt.rect.width * 0.5f - 8f, 0f);
        }

        void Update()
        {
            // Sem nada selecionado (clicou fora), volta para um botão: teclado e controle nunca ficam "perdidos"
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
                Select(confirmPanel != null && confirmPanel.activeSelf ? confirmNo : (CanContinue ? continueButton : newGameButton));
        }

        // ------------------------------------------------------------ ações

        void OnNewGame()
        {
            if (SaveManager.HasSaveFile && confirmPanel != null)
            {
                SeteVidas.Audio.Sfx.Play("ui_aviso");
                confirmPanel.SetActive(true);
                Select(confirmNo); // o padrão é o seguro: não apagar
                return;
            }
            StartNewGame();
        }

        /// <summary>Apaga o save e começa do zero (também usado pelos testes).</summary>
        public void StartNewGame()
        {
            SaveManager.Slot = SaveManager.MainSlot;
            SaveManager.DeleteSave();
            bool hasPrologue = !string.IsNullOrEmpty(prologueScene) && Application.CanStreamedLevelBeLoaded(prologueScene);
            SceneManager.LoadScene(hasPrologue ? prologueScene : newGameSceneName);
        }

        /// <summary>Carrega a cena do último santuário; o PlayerHealth coloca o gato nele.</summary>
        public void Continue()
        {
            SaveManager.Slot = SaveManager.MainSlot;
            if (!SaveManager.HasSaveFile) { SeteVidas.Audio.Sfx.Play("ui_negado"); return; }
            SeteVidas.Audio.Sfx.Play("ui_confirma");
            var data = SaveManager.Data;
            string scene = !string.IsNullOrEmpty(data.scene) && Application.CanStreamedLevelBeLoaded(data.scene) ? data.scene : newGameSceneName;
            SceneManager.LoadScene(scene);
        }

        /// <summary>Abre a Playground com o save de testes (o progresso do jogo não muda).</summary>
        public void OpenTestArea()
        {
            SeteVidas.Audio.Sfx.Play("ui_confirma");
            SaveManager.Slot = SaveManager.TestSlot;
            SceneManager.LoadScene(testScene);
        }

        /// <summary>Opções: liga e desliga as dicas do tutorial (fica nas preferências do jogador).</summary>
        public void ToggleHints()
        {
            TutorialScroll.HintsDisabled = !TutorialScroll.HintsDisabled;
            SeteVidas.Audio.Sfx.Play("ui_confirma");
            Refresh();
        }

        void CloseConfirm()
        {
            SeteVidas.Audio.Sfx.Play("ui_volta");
            if (confirmPanel != null) confirmPanel.SetActive(false);
            Select(newGameButton);
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        static void Select(Selectable s)
        {
            if (s == null || EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(s.gameObject);
        }
    }
}
