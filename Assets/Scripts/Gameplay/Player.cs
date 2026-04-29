using System.Collections.Generic;
using UnityEngine;
using CallKitty.Core;

namespace CallKitty.Gameplay
{
    public class Player : MonoBehaviour
    {
        public int PlayerID { get; set; }
        public string PlayerName { get; set; }
        public bool IsAI { get; set; }
        
        public float TotalScore { get; set; }
        
        // Round specific data
        public List<Card> DealtCards { get; private set; } = new List<Card>();
        public List<List<Card>> ArrangedHands { get; private set; } = new List<List<Card>>(); // Should have exactly 4 elements, each 3 cards
        public Card DiscardCard { get; private set; }
        
        public int CurrentCall { get; set; } = -1; // -1 means no call made yet
        public int HandsWonThisRound { get; set; } = 0;
        
        public bool IsReady { get; set; } = false;

        public virtual void ResetForNewRound()
        {
            DealtCards.Clear();
            ArrangedHands.Clear();
            CurrentCall = -1;
            HandsWonThisRound = 0;
            IsReady = false;
        }

        public void ReceiveCards(List<Card> cards)
        {
            DealtCards.AddRange(cards);
        }

        // Called by UI when player finishes arranging
        public void SetArrangement(List<List<Card>> hands, Card discard)
        {
            if (hands.Count != 4)
            {
                Debug.LogError("Must provide exactly 4 hands.");
                return;
            }

            foreach (var hand in hands)
            {
                if (hand.Count != 3)
                {
                    Debug.LogError("Each hand must contain exactly 3 cards.");
                    return;
                }
            }

            ArrangedHands = new List<List<Card>>(hands);
            DiscardCard = discard;
            IsReady = true;
        }
        
        public List<Card> GetHandForTurn(int turnIndex)
        {
            if (turnIndex >= 0 && turnIndex < 4)
            {
                return ArrangedHands[turnIndex];
            }
            return null;
        }
    }
}
