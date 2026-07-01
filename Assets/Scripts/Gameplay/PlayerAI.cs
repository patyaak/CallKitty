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
            float totalScore = 0f;
            List<List<Card>> handsToEvaluate = GetHandsForBidding();

            foreach (var hand in handsToEvaluate)
            {
                var eval = HandEvaluator.Evaluate3CardHand(hand);
                totalScore += EstimateWinValue(eval);
            }

            // 20% chance the bot plays it safe and bids 0 regardless of hand strength
            bool playingConservative = UnityEngine.Random.value < 0.20f;
            if (playingConservative || totalScore <= 0f)
            {
                CurrentCall = 0;
            }
            else
            {
                // Competition discount varies between 0.65x and 0.85x to account for other players
                float competitionDiscount = UnityEngine.Random.Range(0.65f, 0.85f);
                float discountedScore = totalScore * competitionDiscount;

                // Wider variance (-0.5 to +0.5) so mediocre hands often round down to 0
                float variance = UnityEngine.Random.Range(-0.5f, 0.5f);

                CurrentCall = Mathf.RoundToInt(discountedScore + variance);
                CurrentCall = Mathf.Clamp(CurrentCall, 0, 4);
            }

            Debug.Log($"AI {PlayerName} called {CurrentCall} (Raw Score: {totalScore:F2}, Conservative: {playingConservative})");
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

        private float EstimateWinValue(HandEvaluatedResult eval)
        {
            switch(eval.Rank)
            {
                case HandRank.Trail: return 1.0f;
                case HandRank.PureSequence: return 0.8f;
                case HandRank.Sequence: return 0.5f;
                case HandRank.Color: return 0.3f;
                case HandRank.Pair:
                    if (eval.SortedCards[0].Rank >= Rank.Jack) return 0.1f; // J, Q, K, A
                    return 0.0f; // low pairs have very little chance to win in standard rounds
                default:
                    return 0f;
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
