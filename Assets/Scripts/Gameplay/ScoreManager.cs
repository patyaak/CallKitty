using UnityEngine;
using System.Collections.Generic;
using CallKitty.Core;
using CallKitty.Gameplay;
using TMPro;

namespace CallKitty.Gameplay
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [Header("Prefab & Content")]
        [SerializeField] private GameObject roundScorePrefab;
        [SerializeField] private Transform scrollContent;

        [Header("Total Score TMPro Texts")]
        [SerializeField] private TMP_Text playerTotalScoreText;
        [SerializeField] private TMP_Text bot1TotalScoreText;
        [SerializeField] private TMP_Text bot2TotalScoreText;
        [SerializeField] private TMP_Text bot3TotalScoreText;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void ProcessRoundEnd()
        {
            var players = GameManager.Instance.Players;
            if (players == null || players.Count < 4) return;

            // Calculate round score for each player
            float[] roundScores = new float[4];
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                int call = player.CurrentCall;
                int won = player.HandsWonThisRound;
                float roundScore = 0f;

                if (call == 0)
                {
                    roundScore = 0f; // Bid 0 = opt out, no scoring regardless of wins
                }
                else
                {
                    if (won >= call)
                    {
                        // Exceeded or met call
                        roundScore = call + ((won - call) * 0.1f);
                    }
                    else
                    {
                        // Failed call
                        roundScore = -call;
                    }
                }

                roundScores[i] = roundScore;
                player.TotalScore += roundScore;
                Debug.Log($"[ScoreManager] {player.PlayerName}: Call {call}, Won {won}, Round Score {roundScore}, Total {player.TotalScore}");
            }

            // Instantiate RoundScore prefab in scrollContent
            if (roundScorePrefab != null && scrollContent != null)
            {
                GameObject roundScoreObj = Instantiate(roundScorePrefab, scrollContent);
                // Ensure scale/rotation are correct
                roundScoreObj.transform.localScale = Vector3.one;
                roundScoreObj.transform.localRotation = Quaternion.identity;

                // Find text components in prefab:
                // Structure:
                // - PlayerScore
                // - Bot1Score
                // - Bot2Score
                // - Bot3Score
                UpdateRoundScoreText(roundScoreObj.transform.Find("PlayerScore"), roundScores[0]);
                UpdateRoundScoreText(roundScoreObj.transform.Find("Bot1Score"), roundScores[1]);
                UpdateRoundScoreText(roundScoreObj.transform.Find("Bot2Score"), roundScores[2]);
                UpdateRoundScoreText(roundScoreObj.transform.Find("Bot3Score"), roundScores[3]);
            }
            else
            {
                Debug.LogWarning("[ScoreManager] roundScorePrefab or scrollContent is not assigned!");
            }

            // Update total score texts
            UpdateTotalScoreText(playerTotalScoreText, players[0].TotalScore);
            UpdateTotalScoreText(bot1TotalScoreText, players[1].TotalScore);
            UpdateTotalScoreText(bot2TotalScoreText, players[2].TotalScore);
            UpdateTotalScoreText(bot3TotalScoreText, players[3].TotalScore);
        }

        private void UpdateRoundScoreText(Transform parent, float score)
        {
            if (parent == null) return;
            TMP_Text tmp = parent.GetComponent<TMP_Text>();
            if (tmp == null) tmp = parent.GetComponentInChildren<TMP_Text>();
            if (tmp != null)
            {
                // Format positive scores without plus sign, keep negative signs for negative scores
                tmp.text = $"{score:F1}";
            }

            // If score is negative, activate the child circle; otherwise deactivate it
            Transform circle = parent.Find("circle");
            if (circle != null)
            {
                circle.gameObject.SetActive(score < 0);
            }
        }

        private void UpdateTotalScoreText(TMP_Text tmp, float score)
        {
            if (tmp != null)
            {
                tmp.text = $"{score:F1}";
            }
        }
    }
}
