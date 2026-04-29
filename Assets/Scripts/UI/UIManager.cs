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
            }
            
            HideAllPanels();
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
            HideAllPanels();

            switch (newState)
            {
                case GameState.Bidding:
                    biddingPanel.SetActive(true);
                    break;
                case GameState.Arranging:
                    arrangementPanel.SetActive(true);
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
