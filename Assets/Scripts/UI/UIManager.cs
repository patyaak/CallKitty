using UnityEngine;
using CallKitty.Gameplay;

namespace CallKitty.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Panels")]
        [SerializeField] private GameObject biddingPanel;
        [SerializeField] private GameObject arrangementPanel;
        [SerializeField] private GameObject gameplayPanel;
        [SerializeField] private GameObject scoreboardPanel;
        [SerializeField] private GameObject gameOverPanel;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += HandleStateChanged;
                // Manually trigger for the initial state in case the event fired before we subscribed
                HandleStateChanged(GameManager.Instance.CurrentState);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(GameState newState)
        {
            Debug.Log($"[UIManager] HandleStateChanged: {newState}");
            
            // Only hide all panels if we are NOT entering bidding state.
            // This allows the cards (arrangementPanel) to stay visible while the bidding panel is shown.
            if (newState != GameState.Bidding && newState != GameState.PlayingRound)
            {
                HideAllPanels();
            }

            if (arrangementPanel == null) Debug.LogError("[UIManager] arrangementPanel is NOT assigned in the Inspector!");

            switch (newState)
            {
                case GameState.Dealing:
                    if (arrangementPanel) arrangementPanel.SetActive(true);
                    UIArrangementManager.Instance?.ExitGameplayMode();
                    break;
                case GameState.Bidding:
                    if (biddingPanel) biddingPanel.SetActive(true);
                    break;
                case GameState.Arranging:
                    if (arrangementPanel) arrangementPanel.SetActive(true);
                    // Tell ArrangementManager to populate cards for human player
                    var humanCards = GameManager.Instance.Players[0].DealtCards;
                    UIArrangementManager.Instance?.PopulateCards(humanCards);
                    break;
                case GameState.PlayingRound:
                    if (arrangementPanel) arrangementPanel.SetActive(true);
                    if (gameplayPanel) gameplayPanel.SetActive(true);
                    UIArrangementManager.Instance?.EnterGameplayMode();
                    break;
                case GameState.RoundScoring:
                    HideAllPanels();
                    if (scoreboardPanel) scoreboardPanel.SetActive(true);
                    break;
                case GameState.GameOver:
                    HideAllPanels();
                    if (scoreboardPanel) scoreboardPanel.SetActive(true);
                    if (gameOverPanel) gameOverPanel.SetActive(true);
                    break;
            }
        }

        public void ShowBiddingPanel(bool show)
        {
            if (biddingPanel) biddingPanel.SetActive(show);
        }

        private void HideAllPanels()
        {
            if (biddingPanel) biddingPanel.SetActive(false);
            if (arrangementPanel) arrangementPanel.SetActive(false);
            if (gameplayPanel) gameplayPanel.SetActive(false);
            if (scoreboardPanel) scoreboardPanel.SetActive(false);
            if (gameOverPanel) gameOverPanel.SetActive(false);
        }
    }
}
