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
            float totalStrength = 0f;
            List<List<Card>> handsToEvaluate = GetHandsForBidding();

            foreach (var hand in handsToEvaluate)
            {
                var eval = HandEvaluator.Evaluate3CardHand(hand);
                totalStrength += EstimateHandStrength(eval);
            }

            float averageStrength = totalStrength / Mathf.Max(1, handsToEvaluate.Count);
            float bidScore = averageStrength + UnityEngine.Random.Range(-0.45f, 0.15f);

            if (averageStrength < 0.6f)
            {
                CurrentCall = 0;
            }
            else if (averageStrength < 1.0f)
            {
                CurrentCall = UnityEngine.Random.value < 0.8f ? 0 : 1;
            }
            else if (averageStrength < 1.6f)
            {
                CurrentCall = UnityEngine.Random.value < 0.7f ? 1 : 0;
            }
            else if (averageStrength < 2.2f)
            {
                CurrentCall = UnityEngine.Random.value < 0.6f ? 2 : 1;
            }
            else if (averageStrength < 2.8f)
            {
                CurrentCall = UnityEngine.Random.value < 0.7f ? 3 : 2;
            }
            else
            {
                CurrentCall = UnityEngine.Random.value < 0.8f ? 4 : 3;
            }

            Debug.Log($"AI {PlayerName} called {CurrentCall} (Average Strength: {averageStrength:F2}, Bid Score: {bidScore:F2})");
        }

        public static int EstimateBidFromScore(float strengthScore)
        {
            if (strengthScore < 0.6f) return 0;
            if (strengthScore < 1.0f) return 1;
            if (strengthScore < 1.6f) return 1;
            if (strengthScore < 2.2f) return 2;
            if (strengthScore < 2.8f) return 3;
            return 4;
        }

        private List<List<Card>> GetHandsForBidding()
        {
            if (ArrangedHands.Count == 4)
            {
                return ArrangedHands;
            }

            List<Card> tempCards = new List<Card>(DealtCards);
            List<List<Card>> hands = new List<List<Card>>();

            for (int i = 0; i < 4; i++)
            {
                var bestHand = FindBestHand(tempCards);
                if (bestHand == null) break;

                hands.Add(bestHand);
                foreach (var card in bestHand)
                {
                    tempCards.Remove(card);
                }
            }

            return hands;
        }

        private float EstimateHandStrength(HandEvaluatedResult eval)
        {
            switch (eval.Rank)
            {
                case HandRank.Trail:
                    return 3.2f;
                case HandRank.PureSequence:
                    return 2.6f;
                case HandRank.Sequence:
                    return 1.8f;
                case HandRank.Color:
                    return 1.2f;
                case HandRank.Pair:
                    int pairRank = (int)eval.SortedCards[0].Rank;
                    if (pairRank >= (int)Rank.Jack) return 0.95f;
                    if (pairRank >= (int)Rank.Seven) return 0.6f;
                    return 0.3f;
                default:
                    int topCard = (int)eval.SortedCards[0].Rank;
                    return Mathf.Clamp01((topCard - 8) / 10f) * 0.35f;
            }
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

            // Sort cards within each hand by rank (Ace-low: A, 2, 3, ... K)
            foreach (var hand in arranged)
            {
                hand.Sort((a, b) =>
                {
                    int rankA = a.Rank == Rank.Ace ? 1 : (int)a.Rank;
                    int rankB = b.Rank == Rank.Ace ? 1 : (int)b.Rank;
                    return rankA.CompareTo(rankB);
                });
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
