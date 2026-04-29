using UnityEngine;
using UnityEngine.UI;
using CallKitty.Gameplay;

namespace CallKitty.UI
{
    public class UIScoreboardPanel : MonoBehaviour
    {
        [System.Serializable]
        public class PlayerScoreRow
        {
            public Text nameText;
            public Text callText;
            public Text wonText;
            public Text scoreText;
        }

        [SerializeField] private PlayerScoreRow[] rows = new PlayerScoreRow[4];
        [SerializeField] private GameObject nextRoundButton; // To proceed from RoundScoring to Dealing
        [SerializeField] private Text gameOverText;

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += HandleStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(GameState state)
        {
            if (state == GameState.RoundScoring || state == GameState.GameOver)
            {
                UpdateScoreboard();

                if (state == GameState.GameOver)
                {
                    nextRoundButton.SetActive(false);
                    gameOverText.gameObject.SetActive(true);
                    
                    // Find winner
                    string winner = "";
                    float maxScore = float.MinValue;
                    foreach (var p in GameManager.Instance.Players)
                    {
                        if (p.TotalScore > maxScore)
                        {
                            maxScore = p.TotalScore;
                            winner = p.PlayerName;
                        }
                    }
                    gameOverText.text = $"Game Over! {winner} Wins!";
                }
                else
                {
                    nextRoundButton.SetActive(true);
                    gameOverText.gameObject.SetActive(false);
                }
            }
        }

        private void UpdateScoreboard()
        {
            var players = GameManager.Instance.Players;
            for (int i = 0; i < players.Count; i++)
            {
                rows[i].nameText.text = players[i].PlayerName;
                rows[i].callText.text = players[i].CurrentCall.ToString();
                rows[i].wonText.text = players[i].HandsWonThisRound.ToString();
                rows[i].scoreText.text = players[i].TotalScore.ToString("F1"); // Show decimal for bonus
            }
        }

        // Hook this up to the next round button in inspector
        public void OnNextRoundClicked()
        {
            GameManager.Instance.ChangeState(GameState.Dealing);
        }
    }
}
