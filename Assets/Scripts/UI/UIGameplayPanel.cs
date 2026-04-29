using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CallKitty.Gameplay;
using CallKitty.Core;

namespace CallKitty.UI
{
    public class UIGameplayPanel : MonoBehaviour
    {
        [System.Serializable]
        public class PlayerTableUI
        {
            public Text playerNameText;
            public Transform cardContainer; // Where the 3 played cards are spawned
            public Text resultText; // E.g., "Trail", "Sequence"
            public Image backgroundHighlight;
        }

        [SerializeField] private PlayerTableUI[] playerUIs = new PlayerTableUI[4];
        [SerializeField] private GameObject uiCardPrefab;
        [SerializeField] private Text centerWinnerText;

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTurnPlayed += HandleTurnPlayed;
                GameManager.Instance.OnStateChanged += HandleStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTurnPlayed -= HandleTurnPlayed;
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(GameState state)
        {
            if (state == GameState.PlayingRound)
            {
                // Init UI with player names
                for (int i = 0; i < GameManager.Instance.Players.Count; i++)
                {
                    playerUIs[i].playerNameText.text = GameManager.Instance.Players[i].PlayerName;
                    ClearCards(i);
                }
                centerWinnerText.text = "Playing...";
            }
        }

        private void HandleTurnPlayed(int turnIndex, List<HandEvaluatedResult> hands, Player winner)
        {
            centerWinnerText.text = $"{winner.PlayerName} Wins Turn {turnIndex + 1}!";

            for (int i = 0; i < hands.Count; i++)
            {
                ClearCards(i);
                
                // Spawn cards
                foreach (var card in hands[i].SortedCards)
                {
                    var cardObj = Instantiate(uiCardPrefab, playerUIs[i].cardContainer);
                    cardObj.GetComponent<UICard>().Initialize(card);
                    // Disable drag on these cards
                    Destroy(cardObj.GetComponent<UICard>()); // Remove drag logic for display only
                }

                playerUIs[i].resultText.text = hands[i].Rank.ToString();
                
                // Highlight winner
                if (GameManager.Instance.Players[i] == winner)
                {
                    playerUIs[i].backgroundHighlight.color = Color.green;
                }
                else
                {
                    playerUIs[i].backgroundHighlight.color = Color.white; // Or default
                }
            }
        }

        private void ClearCards(int playerIndex)
        {
            foreach (Transform child in playerUIs[playerIndex].cardContainer)
            {
                Destroy(child.gameObject);
            }
            playerUIs[playerIndex].resultText.text = "";
            playerUIs[playerIndex].backgroundHighlight.color = Color.white;
        }
    }
}
