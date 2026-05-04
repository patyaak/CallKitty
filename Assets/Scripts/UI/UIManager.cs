using UnityEngine;
using CallKitty.Gameplay;

namespace CallKitty.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject biddingPanel;
        [SerializeField] private GameObject arrangementPanel;
        [SerializeField] private GameObject gameplayPanel;
        [SerializeField] private GameObject scoreboardPanel;

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
            if (newState != GameState.Bidding)
            {
                HideAllPanels();
            }

            if (arrangementPanel == null) Debug.LogError("[UIManager] arrangementPanel is NOT assigned in the Inspector!");

            switch (newState)
            {
                case GameState.Dealing:
                    if (arrangementPanel) arrangementPanel.SetActive(true);
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
                    gameplayPanel.SetActive(true);
                    break;
                case GameState.RoundScoring:
                    scoreboardPanel.SetActive(true);
                    break;
                case GameState.GameOver:
                    scoreboardPanel.SetActive(true);
                    break;
            }
        }

        private void HideAllPanels()
        {
            if (biddingPanel) biddingPanel.SetActive(false);
            if (arrangementPanel) arrangementPanel.SetActive(false);
            if (gameplayPanel) gameplayPanel.SetActive(false);
            if (scoreboardPanel) scoreboardPanel.SetActive(false);
        }
    }
}
