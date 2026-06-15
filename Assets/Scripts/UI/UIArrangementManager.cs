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
    }
}
