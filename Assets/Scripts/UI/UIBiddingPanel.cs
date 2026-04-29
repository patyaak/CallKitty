using UnityEngine;
using UnityEngine.UI;
using CallKitty.Gameplay;

namespace CallKitty.UI
{
    public class UIBiddingPanel : MonoBehaviour
    {
        [SerializeField] private Button[] callButtons; // Index 0-4

        private void Awake()
        {
            for (int i = 0; i < callButtons.Length; i++)
            {
                int callValue = i; // capture for closure
                callButtons[i].onClick.AddListener(() => OnCallButtonClicked(callValue));
            }
        }

        private void OnCallButtonClicked(int callValue)
        {
            Player humanPlayer = GameManager.Instance.Players[0];
            humanPlayer.CurrentCall = callValue;
            humanPlayer.IsReady = true;

            // Disable buttons to prevent double-clicking
            foreach (var btn in callButtons)
            {
                btn.interactable = false;
            }
        }

        private void OnEnable()
        {
            // Re-enable buttons when panel shows
            foreach (var btn in callButtons)
            {
                btn.interactable = true;
            }
        }
    }
}
