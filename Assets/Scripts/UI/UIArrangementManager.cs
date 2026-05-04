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

        [Header("UI Elements")]
        [SerializeField] private Button readyButton;
        [SerializeField] private GameObject uiCardPrefab;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            readyButton.onClick.AddListener(OnReadyClicked);
        }

        private void OnEnable()
        {
            // Disable button initially until cards are properly arranged
            readyButton.interactable = false;
        }

        public void PopulateCards(List<Card> cards)
        {
            // Cards are now instantiated and delivered directly into the unassignedPool by the VisualDealer.
            // We only need to check the initial validation state to ensure the Ready button is correct.
            OnCardMoved();
        }

        public void OnCardMoved()
        {
            if (GameManager.Instance.CurrentState == GameState.Dealing)
            {
                // In dealing state, we just want to proceed to bidding
                readyButton.interactable = true;
                readyButton.GetComponentInChildren<Text>().text = "Start Bidding";
            }
            else
            {
                readyButton.interactable = ValidateArrangement();
                readyButton.GetComponentInChildren<Text>().text = "Ready";
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
                GameManager.Instance.StartBidding();
                // Button will be updated/hidden when state changes
                return;
            }

            if (!ValidateArrangement()) return;

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
        }
    }
}
