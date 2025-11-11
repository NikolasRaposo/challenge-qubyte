using System.Collections;
using Player.Powers;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UI.HUD;
using Gameplay;

namespace Managers {
    /// <summary>
    /// Manages all UI elements in the game. Listens to events from the GameManager
    /// and other systems to update the display accordingly.
    /// </summary>
    public class UIManager : MonoBehaviour {
        public static UIManager Instance { get; private set; }

        [Header("UI Panels")]
        [SerializeField] private GameObject coinsPanel;
        [SerializeField] private GameObject livesPanel;
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private GameObject respawnPanel;
        [SerializeField] private GameObject gameOverPanel;

        [Header("HUD Elements")]
        [SerializeField] private TextMeshProUGUI coinsText;
        [SerializeField] private TextMeshProUGUI livesText;
        [SerializeField] private Image tornadoCooldownImage;

        [Header("Pause Menu Elements")]
        [SerializeField] private TextMeshProUGUI pauseCoinsText;
        [SerializeField] private TextMeshProUGUI pauseEnemiesDefeatedText;

        [Header("Respawn Elements")]
        [SerializeField] private TextMeshProUGUI respawnCountdownText;
        
        [Header("HUD Settings")]
        [SerializeField] private float hudVisibleTime = 6f;
        [SerializeField] private PopUIAnimation  coinsPopAnimation;
        [SerializeField] private PopUIAnimation  livesPopAnimation;
        [Header("HUD Behavior")]
        [SerializeField] private bool hudAutoHideEnabled = false; // impede auto-hide até ativação explícita
        public bool HudAnimationsEnabled { get; private set; } = false; // animações só após ativação
        [SerializeField] private CoinUIAnimation coinIdleAnimation; // opcional, controla flutuação idle do ícone
        
        private TornadoAttack _playerTornadoAttack;
        private Coroutine _coinsCoroutine;
        private Coroutine _livesCoroutine;

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
            } else {
                Instance = this;
            }
        }

        private void Start() {
            // Find the player's tornado script.
            // This assumes the player has a "Player" tag.
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) {
                _playerTornadoAttack = player.GetComponent<TornadoAttack>();
            }

            // Ensure the initial state of the UI is correct
            pauseMenuPanel.SetActive(false);
            respawnPanel.SetActive(false);
            gameOverPanel.SetActive(false);
            
            //ShowPanel(coinsPanel);
            //ShowPanel(livesPanel);
        }

        private void OnEnable()
        {
            // Assina eventos do GameManager e inicializa HUD com estado atual
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnCoinsUpdated += UpdateCoinText;
                gm.OnLivesUpdated += UpdateLivesText;
                gm.OnEnemiesDefeatedUpdated += UpdateEnemiesDefeatedText;

                // Inicializa HUD com os valores atuais
                UpdateCoinText(gm.CollectedCoins);
                UpdateLivesText(gm.Lives);
                UpdateEnemiesDefeatedText(gm.EnemiesDefeated);
            }
            else
            {
                // Em alguns ciclos de vida, o GameManager pode não estar pronto aqui.
                // Aguarda até existir e então assina e sincroniza.
                if (_waitGmCoroutine != null) StopCoroutine(_waitGmCoroutine);
                _waitGmCoroutine = StartCoroutine(WaitForGameManagerAndInitialize());
            }
        }

        private void OnDisable()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnCoinsUpdated -= UpdateCoinText;
                gm.OnLivesUpdated -= UpdateLivesText;
                gm.OnEnemiesDefeatedUpdated -= UpdateEnemiesDefeatedText;
            }

            if (_waitGmCoroutine != null)
            {
                try { StopCoroutine(_waitGmCoroutine); } catch { }
                _waitGmCoroutine = null;
            }
        }

        private void Update() {
            // Continuously update the tornado cooldown UI
            if (_playerTornadoAttack) {
                tornadoCooldownImage.fillAmount = _playerTornadoAttack.GetCooldownProgress();
            }
        }

        private Coroutine _waitGmCoroutine;
        private IEnumerator WaitForGameManagerAndInitialize()
        {
            // Espera até que o singleton do GameManager esteja disponível
            while (GameManager.Instance == null)
            {
                yield return null; // próximo frame
            }

            var gm = GameManager.Instance;
            gm.OnCoinsUpdated += UpdateCoinText;
            gm.OnLivesUpdated += UpdateLivesText;
            gm.OnEnemiesDefeatedUpdated += UpdateEnemiesDefeatedText;

            // Sincroniza HUD com os valores atuais
            UpdateCoinText(gm.CollectedCoins);
            UpdateLivesText(gm.Lives);
            UpdateEnemiesDefeatedText(gm.EnemiesDefeated);

            _waitGmCoroutine = null;
        }

        /// <summary>
        /// Event handler for when the coin count changes.
        /// </summary>
        private void UpdateCoinText(int newCoinAmount) {
            coinsText.text = $"{newCoinAmount}";
            pauseCoinsText.text = $"{newCoinAmount}";
            // Só usa auto-hide quando explicitamente habilitado
            if (hudAutoHideEnabled) ShowPanel(coinsPanel);
            else coinsPanel.SetActive(true);

            if (HudAnimationsEnabled && coinsPopAnimation != null)
                coinsPopAnimation.PlayPop();
        }
    
        /// <summary>
        /// Event handler for when the player's lives change.
        /// </summary>
        private void UpdateLivesText(int newLivesAmount) {
            livesText.text = $"{newLivesAmount}";
            // Só usa auto-hide quando explicitamente habilitado
            if (hudAutoHideEnabled) ShowPanel(livesPanel);
            else livesPanel.SetActive(true);
            
            if (HudAnimationsEnabled && livesPopAnimation != null)
                livesPopAnimation.PlayPop();
        }
        private void ShowPanel(GameObject panel) {
            if (panel == coinsPanel) {
                if (_coinsCoroutine != null) StopCoroutine(_coinsCoroutine);
                _coinsCoroutine = StartCoroutine(HideAfterDelay(panel));
            }
            else if (panel == livesPanel) {
                if (_livesCoroutine != null) StopCoroutine(_livesCoroutine);
                _livesCoroutine = StartCoroutine(HideAfterDelay(panel));
            }

            panel.SetActive(true);
        }
        private IEnumerator HideAfterDelay(GameObject panel) {
            yield return new WaitForSeconds(hudVisibleTime);
            panel.SetActive(false);
        }
        /// <summary>
        /// Event handler for when enemies are defeated count change.
        /// </summary>
        private void UpdateEnemiesDefeatedText(int enemiesDefeatedAmount) {
            pauseEnemiesDefeatedText.text = $"{enemiesDefeatedAmount}";
        }

        public void TogglePauseMenu(bool isPaused) {
            // Garante que toda a cadeia de pais e CanvasGroups estejam visíveis para o Pause Menu
            if (isPaused)
            {
                EnsureParentsActive(pauseMenuPanel);
                EnsureCanvasGroupsVisible(pauseMenuPanel);
            }

            pauseMenuPanel.SetActive(isPaused);
            coinsPanel.SetActive(!isPaused);
            livesPanel.SetActive(!isPaused);
            if (isPaused)
            {
                // Failsafe: interrompe qualquer vibração quando o menu de pausa é exibido
                RumbleManager.Instance?.StopAllRumble();
            }
        }
        /// <summary>
        /// Shows the Game Over screen.
        /// </summary>
        public void ShowGameOverScreen() {
            // Garante que pais e CanvasGroups do Game Over estejam visíveis
            EnsureParentsActive(gameOverPanel);
            EnsureCanvasGroupsVisible(gameOverPanel);

            coinsPanel.SetActive(false);
            livesPanel.SetActive(false);
            gameOverPanel.SetActive(true);
            // Failsafe: garante silêncio haptico ao mostrar Game Over
            RumbleManager.Instance?.StopAllRumble();
        }
        /// <summary>
        /// Starts the respawn countdown visual sequence.
        /// </summary>
        /// <param name="countdownDuration">How long the countdown should last.</param>
        public IEnumerator StartRespawnCountdown(float countdownDuration) {
            // Garante que pais e CanvasGroups do painel de respawn estejam visíveis
            EnsureParentsActive(respawnPanel);
            EnsureCanvasGroupsVisible(respawnPanel);

            coinsPanel.SetActive(false);
            livesPanel.SetActive(false);
            respawnPanel.SetActive(true);

            // Failsafe: interrompe vibração ao iniciar o countdown de respawn
            RumbleManager.Instance?.StopAllRumble();

            float timer = countdownDuration;
            while (timer > 0) {
                respawnCountdownText.text = $"Reaparecendo em... {Mathf.CeilToInt(timer)}";
                timer -= Time.unscaledDeltaTime; // Use unscaled time to work even when game is paused
                yield return null;
            }

            respawnPanel.SetActive(false);
            coinsPanel.SetActive(true);
            livesPanel.SetActive(true);
        }
        
        private void OnDestroy() {
            // Failsafe: ao destruir a UI, garante que qualquer rumble ativo seja interrompido
            RumbleManager.Instance?.StopAllRumble();
        }

        // --- API pública para cinemática / sinais ---
        /// <summary>
        /// Exibe imediatamente a HUD de moedas e vidas, sincronizando com os valores atuais.
        /// Não inicia corrotinas de ocultação; mantém visível até ser trocada por outro fluxo.
        /// </summary>
        public void ShowHUDImmediate()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                // Sincroniza textos com estado atual
                coinsText.text = $"{gm.CollectedCoins}";
                livesText.text = $"{gm.Lives}";
            }

            // Garante que toda a cadeia de pais esteja ativa (CanvasHUD -> PanelHUD -> Panels)
            EnsureParentsActive(coinsPanel);
            EnsureParentsActive(livesPanel);
            // Garante CanvasGroups visíveis nos pais e nos próprios painéis
            EnsureCanvasGroupsVisible(coinsPanel);
            EnsureCanvasGroupsVisible(livesPanel);

            // Mostra painéis sem acionar temporizadores de ocultação
            coinsPanel.SetActive(true);
            livesPanel.SetActive(true);

            // Ativa animações da HUD e desabilita auto-hide
            HudAnimationsEnabled = true;
            hudAutoHideEnabled = false;
            // Inicia flutuação idle apenas após ativação explícita
            coinIdleAnimation?.EnableHudAnimations();

            // Opcional: pequeno pop para feedback visual
            if (HudAnimationsEnabled && coinsPopAnimation != null) coinsPopAnimation.PlayPop();
            if (HudAnimationsEnabled && livesPopAnimation != null) livesPopAnimation.PlayPop();
        }

        /// <summary>
        /// Oculta imediatamente a HUD (coins/lives) sem efeitos ou temporizadores.
        /// Para voltar a mostrar com sincronização, use <see cref="ShowHUDImmediate"/>.
        /// </summary>
        public void HideHUDImmediate()
        {
            // Interrompe quaisquer temporizadores de ocultação ainda pendentes
            if (_coinsCoroutine != null) { try { StopCoroutine(_coinsCoroutine); } catch { } _coinsCoroutine = null; }
            if (_livesCoroutine != null) { try { StopCoroutine(_livesCoroutine); } catch { } _livesCoroutine = null; }

            coinsPanel.SetActive(false);
            livesPanel.SetActive(false);

            // Desativa animações da HUD até próxima ativação explícita
            HudAnimationsEnabled = false;
        }

        /// <summary>
        /// Controla visibilidade bruta da HUD (coins/lives) sem efeitos ou temporizadores.
        /// </summary>
        public void SetHUDVisible(bool visible)
        {
            coinsPanel.SetActive(visible);
            livesPanel.SetActive(visible);
        }
        /// <summary>
        /// Controla se os painéis coins/lives devem se auto-ocultar ao aparecer.
        /// </summary>
        public void SetHudAutoHide(bool enabled)
        {
            hudAutoHideEnabled = enabled;
        }

        // Ativa recursivamente a cadeia de pais para garantir que o painel possa ser exibido
        private void EnsureParentsActive(GameObject leaf)
        {
            if (leaf == null) return;
            var t = leaf.transform.parent;
            // Sobe até a raiz, ativando cada pai que estiver desativado
            while (t != null)
            {
                if (!t.gameObject.activeSelf)
                    t.gameObject.SetActive(true);
                t = t.parent;
            }
        }

        // Assegura que quaisquer CanvasGroup na cadeia (inclusive no leaf) estejam visíveis/interagíveis
        private void EnsureCanvasGroupsVisible(GameObject leaf)
        {
            if (leaf == null) return;
            // Primeiro garante o próprio leaf
            var cgSelf = leaf.GetComponent<CanvasGroup>();
            if (cgSelf != null)
            {
                cgSelf.alpha = 1f;
                cgSelf.interactable = true;
                cgSelf.blocksRaycasts = true;
            }

            // Depois percorre pais
            var t = leaf.transform.parent;
            while (t != null)
            {
                var cg = t.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 1f;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
                t = t.parent;
            }
        }
    }
}