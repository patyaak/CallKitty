using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CallKitty.Gameplay;

namespace CallKitty.UI
{
    public class UIBiddingPanel : MonoBehaviour
    {
        [SerializeField] private Button[] callButtons; // Index 0-4
        [SerializeField] private Button okButton;
        [SerializeField] private TMPro.TextMeshProUGUI[] playerBidTexts; // 0: Player, 1-3: Bots
        [SerializeField] private GameObject[] winValuePanels; // 0: Player, 1-3: Bots

        private int selectedBid = -1;

        private void Awake()
        {
            for (int i = 0; i < callButtons.Length; i++)
            {
                int callValue = i; // capture for closure
                callButtons[i].onClick.AddListener(() => OnCallButtonClicked(callValue));
            }

            if (okButton != null)
            {
                okButton.onClick.AddListener(OnOkButtonClicked);
                okButton.interactable = false; // Disable until a bid is selected
            }
        }

        private void OnCallButtonClicked(int callValue)
        {
            selectedBid = callValue;
            
            // Visual feedback for selection (optional, but good for UX)
            for (int i = 0; i < callButtons.Length; i++)
            {
                var colors = callButtons[i].colors;
                colors.normalColor = (i == callValue) ? Color.yellow : Color.white;
                callButtons[i].colors = colors;
            }

            if (okButton != null) okButton.interactable = true;
        }

        private void OnOkButtonClicked()
        {
            // Start the routine on the GameManager so it continues even if this panel is deactivated
            GameManager.Instance.StartCoroutine(ConfirmBiddingRoutine());
            gameObject.SetActive(false); // Hide the selection panel immediately
        }

        private IEnumerator ConfirmBiddingRoutine()
        {
            // Disable card shifting in arrangement panel
            UIArrangementManager.Instance?.SetAllCardsInteractable(false);

            // 1. Update Human Bid
            Player humanPlayer = GameManager.Instance.Players[0];
            humanPlayer.CurrentCall = selectedBid;
            humanPlayer.IsReady = true;
            
            // Show all winValue panels (bid displays)
            if (winValuePanels.Length > 0 && winValuePanels[0] != null) winValuePanels[0].SetActive(true);
            if (playerBidTexts.Length > 0 && playerBidTexts[0] != null)
            {
                playerBidTexts[0].text = selectedBid.ToString();
            }

            // 2. Generate and Update Bot Bids
            for (int i = 1; i < GameManager.Instance.Players.Count; i++)
            {
                Player bot = GameManager.Instance.Players[i];
                if (bot.IsAI)
                {
                    ((PlayerAI)bot).PerformBidding();
                    if (winValuePanels.Length > i && winValuePanels[i] != null) winValuePanels[i].SetActive(true);
                    if (playerBidTexts.Length > i && playerBidTexts[i] != null)
                    {
                        playerBidTexts[i].text = bot.CurrentCall.ToString();
                    }
                }
            }

            // 3. Trigger Card Throw Animation
            if (VisualDealer.Instance != null)
            {
                List<Vector3> startPositions = new List<Vector3>();
                
                // Human discard position
                if (UIArrangementManager.Instance != null)
                {
                    startPositions.Add(UIArrangementManager.Instance.DiscardZonePosition);
                }

                // Bot positions
                foreach (var botPos in VisualDealer.Instance.BotPositions)
                {
                    if (botPos != null) startPositions.Add(botPos.position);
                }

                bool animationDone = false;
                yield return VisualDealer.Instance.StartCoroutine(VisualDealer.Instance.AnimateAllDiscardsThrow(startPositions, () => animationDone = true));
                yield return new WaitUntil(() => animationDone);
            }

            // 4. Transition State
            GameManager.Instance.StartBidding();
        }

        private void OnEnable()
        {
            selectedBid = -1;
            if (okButton != null) okButton.interactable = false;

            // Hide winValue panels until bid is confirmed
            foreach (var panel in winValuePanels)
            {
                if (panel != null) panel.SetActive(false);
            }

            // Re-enable buttons and reset colors
            foreach (var btn in callButtons)
            {
                btn.interactable = true;
                var colors = btn.colors;
                colors.normalColor = Color.white;
                btn.colors = colors;
            }
        }
    }
}
