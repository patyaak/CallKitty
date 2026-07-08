using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CallKitty.Core;
using CallKitty.Gameplay;

namespace CallKitty.UI
{
    public class UIArrangementManager : MonoBehaviour
    {
        public static UIArrangementManager Instance { get; private set; }

        [Header("Slots")]
        [SerializeField] private UICardSlot unassignedPool;
        [SerializeField] private UICardSlot[] handZones = new UICardSlot[4];
        [SerializeField] private UICardSlot discardZone;
        public Vector3 DiscardZonePosition => discardZone.transform.position;

        [Header("UI Elements")]
        [SerializeField] private Button readyButton;
        [SerializeField] private Button arrangeButton;
        [SerializeField] private Button scoreButton;
        [SerializeField] private GameObject uiCardPrefab;

        private class RankedUIHand
        {
            public int OriginalIndex;
            public List<UICard> Cards;
            public HandEvaluatedResult Evaluation;
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            readyButton.onClick.AddListener(OnReadyClicked);
            if (arrangeButton != null) arrangeButton.onClick.AddListener(OnArrangeClicked);
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTurnStarted += SetActiveHandZone;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTurnStarted -= SetActiveHandZone;
            }
        }

        private void OnArrangeClicked()
        {
            if (!ValidateArrangement())
            {
                Debug.LogWarning("[UIArrangementManager] Arrange ranking needs 4 complete hands and 1 discard card.");
                return;
            }

            // Disable arrange button permanently to prevent multiple clicks
            if (arrangeButton != null)
            {
                arrangeButton.interactable = false;
            }

            List<RankedUIHand> rankedHands = new List<RankedUIHand>();

            for (int i = 0; i < handZones.Length; i++)
            {
                List<UICard> uiCards = new List<UICard>();
                List<Card> cards = new List<Card>();

                foreach (Transform child in handZones[i].transform)
                {
                    UICard uiCard = child.GetComponent<UICard>();
                    if (uiCard == null) continue;

                    uiCards.Add(uiCard);
                    cards.Add(uiCard.CardData);
                }

                rankedHands.Add(new RankedUIHand
                {
                    OriginalIndex = i,
                    Cards = uiCards,
                    Evaluation = HandEvaluator.Evaluate3CardHand(cards)
                });
            }

            rankedHands.Sort((left, right) =>
            {
                int rankComparison = right.Evaluation.CompareTo(left.Evaluation);
                if (rankComparison != 0) return rankComparison;

                return left.OriginalIndex.CompareTo(right.OriginalIndex);
            });

            for (int handIndex = 0; handIndex < rankedHands.Count; handIndex++)
            {
                List<UICard> uiCards = rankedHands[handIndex].Cards;
                for (int cardIndex = 0; cardIndex < uiCards.Count; cardIndex++)
                {
                    UICard uiCard = uiCards[cardIndex];
                    uiCard.transform.SetParent(handZones[handIndex].transform, true);
                    uiCard.transform.SetSiblingIndex(cardIndex);
                }
            }

            Debug.Log("[UIArrangementManager] Ranked existing hands from strongest to weakest.");
        }

        private void OnEnable()
        {
            // Disable buttons initially until cards are properly dealt
            readyButton.gameObject.SetActive(false);
            readyButton.interactable = false;
            if (arrangeButton != null)
            {
                arrangeButton.gameObject.SetActive(false);
                arrangeButton.interactable = false;
            }
            if (scoreButton != null)
            {
                scoreButton.gameObject.SetActive(false);
                scoreButton.interactable = false;
            }
        }

        public void PopulateCards(List<Card> cards)
        {
            SetAllCardsInteractable(true);
            // Cards are now instantiated and delivered directly into the unassignedPool by the VisualDealer.
            // We only need to check the initial validation state to ensure the Ready button is correct.
            OnCardMoved();
        }

        public void SetAllCardsInteractable(bool interactable)
        {
            UICard[] allCards = GetComponentsInChildren<UICard>(true);
            foreach (var card in allCards)
            {
                if (card != null) card.IsInteractable = interactable;
            }
        }

        public void OnCardMoved()
        {
            if (GameManager.Instance.CurrentState == GameState.Arranging)
            {
                readyButton.gameObject.SetActive(true);
                readyButton.interactable = ValidateArrangement();
                 if (scoreButton != null)
                {
                    scoreButton.gameObject.SetActive(true);
                    scoreButton.interactable = true;
                }
                // Also activate arrange button when cards are dealt
                if (arrangeButton != null)
                {
                    arrangeButton.gameObject.SetActive(true);
                    arrangeButton.interactable = ValidateArrangement();
                }
            }
            else
            {
                readyButton.gameObject.SetActive(false);
                readyButton.interactable = false;
                
                if (arrangeButton != null)
                {
                    arrangeButton.gameObject.SetActive(false);
                    arrangeButton.interactable = false;
                }

               
            }
        }

        private bool ValidateArrangement()
        {
            if (GameManager.Instance.CurrentState != GameState.Arranging) return false;

            if (unassignedPool.transform.childCount > 0) return false;
            
            if (discardZone.transform.childCount != 1) return false;

            foreach (var zone in handZones)
            {
                if (zone.transform.childCount != 3) return false;
            }

            return true;
        }

        private void OnReadyClicked()
        {
            if (GameManager.Instance.CurrentState != GameState.Arranging) return;
            if (!ValidateArrangement()) return;

            OnArrangeClicked();
            StartCoroutine(FinishArrangementRoutine());
        }

        private IEnumerator FinishArrangementRoutine()
        {
            // Disable buttons to prevent multiple clicks
            readyButton.interactable = false;
            readyButton.gameObject.SetActive(false);
            if (arrangeButton != null)
            {
                arrangeButton.interactable = false;
                arrangeButton.gameObject.SetActive(false);
            }
            if (scoreButton != null)
            {
                scoreButton.interactable = false;
                scoreButton.gameObject.SetActive(false);
            }
            SetAllCardsInteractable(false);

            // Extract the arrangement
            List<List<Card>> arrangedHands = new List<List<Card>>();
            foreach (var zone in handZones)
            {
                List<Card> hand = new List<Card>();
                foreach (Transform child in zone.transform)
                {
                    hand.Add(child.GetComponent<UICard>().CardData);
                }
                arrangedHands.Add(hand);
            }

            Card discard = discardZone.transform.GetChild(0).GetComponent<UICard>().CardData;

            // Send to the human player
            Player humanPlayer = GameManager.Instance.Players[0];
            humanPlayer.SetArrangement(arrangedHands, discard);

            yield break;
        }

        public void HideDiscardCard()
        {
            if (discardZone != null && discardZone.transform.childCount > 0)
            {
                discardZone.transform.GetChild(0).gameObject.SetActive(false);
            }
        }

        // Called when entering gameplay to prepare the UI for turn-based play
        public void EnterGameplayMode()
        {
            // Hide arrangement buttons
            readyButton.gameObject.SetActive(false);
            readyButton.interactable = false;
            
            if (arrangeButton != null)
            {
                arrangeButton.gameObject.SetActive(false);
                arrangeButton.interactable = false;
            }

            // Show score button during gameplay
            if (scoreButton != null)
            {
                scoreButton.gameObject.SetActive(false);
                scoreButton.interactable = false;
            }

            // Hide discard zone during gameplay
            if (discardZone != null)
            {
                discardZone.gameObject.SetActive(false);
            }

            // Make cards non-draggable during gameplay
            SetAllCardsInteractable(false);

            // Initially disable all hand zones
            for (int i = 0; i < handZones.Length; i++)
            {
                SetHandZoneInteractable(i, false);
                SetHandZoneHighlight(i, false);
            }
        }

        // Highlight and enable the current turn's hand zone
        public void SetActiveHandZone(int turnIndex)
        {
            if (turnIndex < 0 || turnIndex >= handZones.Length)
            {
                Debug.LogWarning($"[UIArrangementManager] Invalid turn index: {turnIndex}");
                return;
            }

            // Disable all hand zones first
            for (int i = 0; i < handZones.Length; i++)
            {
                SetHandZoneInteractable(i, false);
                SetHandZoneHighlight(i, false);
            }

            // Enable and highlight the current turn's hand zone
            SetHandZoneInteractable(turnIndex, true);
            SetHandZoneHighlight(turnIndex, true);

            Debug.Log($"[UIArrangementManager] Active hand zone set to Turn {turnIndex + 1}");
        }

        public void HideHandZoneCards(int zoneIndex)
        {
            if (zoneIndex < 0 || zoneIndex >= handZones.Length) return;

            foreach (Transform child in handZones[zoneIndex].transform)
            {
                child.gameObject.SetActive(false);
            }
            SetHandZoneHighlight(zoneIndex, false);
        }

        public List<Vector3> GetHandZoneCardWorldPositions(int zoneIndex)
        {
            List<Vector3> positions = new List<Vector3>();
            if (zoneIndex < 0 || zoneIndex >= handZones.Length) return positions;

            foreach (Transform child in handZones[zoneIndex].transform)
            {
                positions.Add(child.position);
            }

            return positions;
        }

        // Enable/disable interaction for a specific hand zone
        private void SetHandZoneInteractable(int zoneIndex, bool interactable)
        {
            foreach (Transform child in handZones[zoneIndex].transform)
            {
                UICard card = child.GetComponent<UICard>();
                if (card != null)
                {
                    card.IsInteractable = interactable;
                }
            }
        }

        // Highlight a hand zone to show it's the active turn
        private void SetHandZoneHighlight(int zoneIndex, bool highlight)
        {
            Image zoneImage = handZones[zoneIndex].GetComponent<Image>();
            if (zoneImage != null)
            {
                zoneImage.color = highlight ? new Color(1f, 1f, 0.5f, 0.3f) : new Color(1f, 1f, 1f, 0.1f);
            }
        }

        // Get all cards from a specific hand zone (useful for showing current hand)
        public List<Card> GetHandZoneCards(int zoneIndex)
        {
            List<Card> cards = new List<Card>();
            if (zoneIndex < 0 || zoneIndex >= handZones.Length) return cards;

            foreach (Transform child in handZones[zoneIndex].transform)
            {
                UICard uiCard = child.GetComponent<UICard>();
                if (uiCard != null)
                {
                    cards.Add(uiCard.CardData);
                }
            }
            return cards;
        }

        // Exit gameplay mode and return to normal state
        public void ExitGameplayMode()
        {
            // Re-enable all cards as interactable if in arrangement mode
            if (GameManager.Instance.CurrentState == GameState.Arranging)
            {
                SetAllCardsInteractable(true);
            }

            // Hide score button when leaving gameplay
            if (scoreButton != null)
            {
                scoreButton.gameObject.SetActive(false);
                scoreButton.interactable = false;
            }

            // Show discard zone
            if (discardZone != null)
            {
                discardZone.gameObject.SetActive(true);
            }

            // Reset all hand zone highlights
            for (int i = 0; i < handZones.Length; i++)
            {
                SetHandZoneHighlight(i, false);
                SetHandZoneInteractable(i, false);
            }
        }
    }
}
