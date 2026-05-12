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
        [SerializeField] private GameObject uiCardPrefab;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            readyButton.onClick.AddListener(OnReadyClicked);
            if (arrangeButton != null) arrangeButton.onClick.AddListener(OnArrangeClicked);
        }

        private void OnArrangeClicked()
        {
            List<UICard> allUICards = new List<UICard>();
            
            // 1. Collect all 13 cards from all possible slots
            // Pool
            foreach (Transform child in unassignedPool.transform)
            {
                var card = child.GetComponent<UICard>();
                if (card != null) allUICards.Add(card);
            }
            // Zones
            foreach (var zone in handZones)
            {
                foreach (Transform child in zone.transform)
                {
                    var card = child.GetComponent<UICard>();
                    if (card != null) allUICards.Add(card);
                }
            }
            // Discard
            foreach (Transform child in discardZone.transform)
            {
                var card = child.GetComponent<UICard>();
                if (card != null) allUICards.Add(card);
            }

            // Safety check: ensure we have the full hand
            if (allUICards.Count < 13)
            {
                Debug.LogWarning($"[UIArrangementManager] Only found {allUICards.Count} cards. Need 13 to auto-arrange.");
                return;
            }

            // 2. Greedy algorithm to find 4 best hands sequentially
            List<UICard> tempUICards = new List<UICard>(allUICards);
            List<List<UICard>> bestUIHands = new List<List<UICard>>();

            for (int h = 0; h < 4; h++)
            {
                List<UICard> bestHand = FindBestUIHand(tempUICards);
                if (bestHand != null)
                {
                    bestUIHands.Add(bestHand);
                    foreach (var card in bestHand) tempUICards.Remove(card);
                }
            }

            // 3. The remaining card goes to the discard zone
            UICard discardUICard = tempUICards[0];

            // 4. Update UI parenting and positions
            // Hand Zones (Priority 1 to 4)
            for (int h = 0; h < 4; h++)
            {
                if (h < bestUIHands.Count)
                {
                    foreach (var uiCard in bestUIHands[h])
                    {
                        uiCard.transform.SetParent(handZones[h].transform, false);
                        uiCard.transform.localPosition = Vector3.zero; // Reset position in case of drag residue
                    }
                }
            }
            // Discard Zone
            discardUICard.transform.SetParent(discardZone.transform, false);
            discardUICard.transform.localPosition = Vector3.zero;

            OnCardMoved(); // Update Ready button state
            Debug.Log("[UIArrangementManager] Auto-arranged cards into 4 sets + 1 discard.");
        }

        private List<UICard> FindBestUIHand(List<UICard> availableUICards)
        {
            if (availableUICards.Count < 3) return null;

            List<UICard> bestHand = null;
            HandEvaluatedResult bestEval = null;

            int n = availableUICards.Count;
            // Brute force search for the best 3-card combination (N choose 3)
            for (int i = 0; i < n - 2; i++)
            {
                for (int j = i + 1; j < n - 1; j++)
                {
                    for (int k = j + 1; k < n; k++)
                    {
                        var cards = new List<Card> { 
                            availableUICards[i].CardData, 
                            availableUICards[j].CardData, 
                            availableUICards[k].CardData 
                        };
                        var eval = HandEvaluator.Evaluate3CardHand(cards);

                        if (bestEval == null || eval.CompareTo(bestEval) > 0)
                        {
                            bestEval = eval;
                            bestHand = new List<UICard> { availableUICards[i], availableUICards[j], availableUICards[k] };
                        }
                    }
                }
            }
            return bestHand;
        }

        private void OnEnable()
        {
            // Disable button initially until cards are properly dealt
            readyButton.gameObject.SetActive(false);
            readyButton.interactable = false;
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
            if (GameManager.Instance.CurrentState == GameState.Dealing)
            {
                // In dealing state, show the button to proceed
                readyButton.gameObject.SetActive(true);
                readyButton.interactable = true;
               // readyButton.GetComponentInChildren<Text>().text = "Start Bidding";
            }
            else
            {
                readyButton.interactable = ValidateArrangement();
               // readyButton.GetComponentInChildren<Text>().text = "Ready";
            }
        }

        private bool ValidateArrangement()
        {
            if (GameManager.Instance.CurrentState == GameState.Dealing) return true;

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
            if (GameManager.Instance.CurrentState == GameState.Dealing)
            {
                // Hide button immediately
                readyButton.gameObject.SetActive(false);

                // Handle discard animation
                if (discardZone.transform.childCount > 0)
                {
                    discardZone.transform.GetChild(0).gameObject.SetActive(false);
                }

                // Trigger the bidding panel instead of throw
                UIManager.Instance?.ShowBiddingPanel(true);
                // GameManager.Instance.StartBidding();
                // Button will be updated/hidden when state changes
                return;
            }

            if (!ValidateArrangement()) return;

            StartCoroutine(FinishArrangementRoutine());
        }

        private IEnumerator FinishArrangementRoutine()
        {
            // Disable button to prevent multiple clicks
            readyButton.interactable = false;

            // Handle discard animation (Hiding the card immediately, animation is handled by GameManager)
            if (discardZone.transform.childCount > 0)
            {
                discardZone.transform.GetChild(0).gameObject.SetActive(false);
            }

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

            // Hide this UI (GameManager will handle state change when all are ready)
            gameObject.SetActive(false);
            yield break;
        }
    }
}
