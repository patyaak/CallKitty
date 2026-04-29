using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CallKitty.Core;

namespace CallKitty.Gameplay
{
    public class PlayerAI : Player
    {
        public void PerformBidding()
        {
            // Basic AI: Greedy search for strong hands
            List<Card> tempCards = new List<Card>(DealtCards);
            int estimatedWins = 0;

            for (int i = 0; i < 4; i++)
            {
                var bestHand = FindBestHand(tempCards);
                if (bestHand != null && bestHand.Count == 3)
                {
                    var eval = HandEvaluator.Evaluate3CardHand(bestHand);
                    // Consider Pair of 10s or better, or any Sequence/Flush/Trail as a "potential win"
                    if (eval.Rank >= HandRank.Color || 
                       (eval.Rank == HandRank.Pair && eval.SortedCards[0].Rank >= Rank.Ten))
                    {
                        estimatedWins++;
                    }

                    // Remove these cards to evaluate the rest
                    foreach (var card in bestHand)
                    {
                        tempCards.Remove(card);
                    }
                }
            }

            // Cap the call to 4 (max hands)
            CurrentCall = Mathf.Clamp(estimatedWins, 0, 4);
            Debug.Log($"AI {PlayerName} called {CurrentCall}");
            IsReady = true;
        }

        public void PerformArrangement()
        {
            List<Card> tempCards = new List<Card>(DealtCards);
            List<List<Card>> arranged = new List<List<Card>>();

            for (int i = 0; i < 4; i++)
            {
                var bestHand = FindBestHand(tempCards);
                if (bestHand != null)
                {
                    arranged.Add(bestHand);
                    foreach (var card in bestHand)
                    {
                        tempCards.Remove(card);
                    }
                }
            }

            Card discard = tempCards.Count > 0 ? tempCards[0] : new Card();
            
            // Reorder arranged hands to put strongest first or distribute them?
            // Usually, playing strong hands early secures wins, but order doesn't matter too much for a basic AI.
            SetArrangement(arranged, discard);
            Debug.Log($"AI {PlayerName} finished arranging cards.");
        }

        private List<Card> FindBestHand(List<Card> availableCards)
        {
            if (availableCards.Count < 3) return null;

            List<Card> bestHand = null;
            HandEvaluatedResult bestEval = null;

            // Generate all combinations of 3 cards
            // N choose 3. For 13 cards, 13! / (3! * 10!) = 286 combinations. Very small, can do brute force.
            int n = availableCards.Count;
            for (int i = 0; i < n - 2; i++)
            {
                for (int j = i + 1; j < n - 1; j++)
                {
                    for (int k = j + 1; k < n; k++)
                    {
                        var hand = new List<Card> { availableCards[i], availableCards[j], availableCards[k] };
                        var eval = HandEvaluator.Evaluate3CardHand(hand);

                        if (bestEval == null || eval.CompareTo(bestEval) > 0)
                        {
                            bestEval = eval;
                            bestHand = hand;
                        }
                    }
                }
            }

            return bestHand;
        }
    }
}
