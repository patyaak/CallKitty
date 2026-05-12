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
            float totalScore = 0f;

            for (int i = 0; i < 4; i++)
            {
                var bestHand = FindBestHand(tempCards);
                if (bestHand != null && bestHand.Count == 3)
                {
                    var eval = HandEvaluator.Evaluate3CardHand(bestHand);
                    
                    // Assign probabilities of winning for each hand type
                    switch(eval.Rank)
                    {
                        case HandRank.Trail: totalScore += 1.0f; break;
                        case HandRank.PureSequence: totalScore += 0.9f; break;
                        case HandRank.Sequence: totalScore += 0.7f; break;
                        case HandRank.Color: totalScore += 0.5f; break;
                        case HandRank.Pair:
                            if (eval.SortedCards[0].Rank >= Rank.Ace) totalScore += 0.4f;
                            else if (eval.SortedCards[0].Rank >= Rank.Jack) totalScore += 0.3f;
                            else if (eval.SortedCards[0].Rank >= Rank.Eight) totalScore += 0.15f;
                            else totalScore += 0.05f;
                            break;
                        default: // High Card
                            if (eval.SortedCards[0].Rank >= Rank.Ace) totalScore += 0.1f;
                            break;
                    }

                    // Remove these cards to evaluate the rest
                    foreach (var card in bestHand)
                    {
                        tempCards.Remove(card);
                    }
                }
            }

            // Round the total score to get the bid, clamping between 1 and 4
            // (In most variations, AI bids at least 1)
            CurrentCall = Mathf.RoundToInt(totalScore);
            CurrentCall = Mathf.Clamp(CurrentCall, 1, 4); 
            
            Debug.Log($"AI {PlayerName} called {CurrentCall} (Calculated Score: {totalScore:F2})");
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
