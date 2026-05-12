using System.Collections.Generic;
using System.Linq;

namespace CallKitty.Core
{
    public class HandEvaluatedResult
    {
        public HandRank Rank { get; set; }
        public List<Card> SortedCards { get; set; } // Sorted for tie-breaking. For Pair, pair cards are first.

        public int CompareTo(HandEvaluatedResult other)
        {
            if (this.Rank != other.Rank)
            {
                return this.Rank.CompareTo(other.Rank);
            }

            // Same rank, compare cards
            for (int i = 0; i < SortedCards.Count; i++)
            {
                int cardComparison = this.SortedCards[i].Rank.CompareTo(other.SortedCards[i].Rank);
                if (cardComparison != 0)
                {
                    return cardComparison;
                }
            }

            return 0; // Exact tie
        }
    }

    public static class HandEvaluator
    {
        public static HandEvaluatedResult Evaluate3CardHand(List<Card> hand)
        {
            if (hand == null || hand.Count != 3)
            {
                throw new System.ArgumentException("Hand must contain exactly 3 cards.");
            }

            // Sort cards descending by rank for easier evaluation
            var sortedCards = hand.OrderByDescending(c => c.Rank).ToList();

            // Check A-2-3 special sequence case (where A acts as 1)
            bool isA23 = sortedCards[0].Rank == Rank.Ace && sortedCards[1].Rank == Rank.Three && sortedCards[2].Rank == Rank.Two;
            
            bool isFlush = sortedCards[0].Suit == sortedCards[1].Suit && sortedCards[1].Suit == sortedCards[2].Suit;
            
            bool isSequence = false;
            if (isA23)
            {
                isSequence = true;
                // Re-sort for tie-breaker: A, 3, 2
                // This makes A-2-3 the second strongest sequence after A-K-Q 
                // (Comparison: A-K-Q is 14,13,12 vs A-2-3 is 14,3,2. First card ties, second card 13 beats 3).
                sortedCards = new List<Card> { sortedCards[0], sortedCards[1], sortedCards[2] }; 
            }
            else
            {
                isSequence = (sortedCards[0].Rank - sortedCards[1].Rank == 1) && (sortedCards[1].Rank - sortedCards[2].Rank == 1);
            }

            bool isTrail = sortedCards[0].Rank == sortedCards[1].Rank && sortedCards[1].Rank == sortedCards[2].Rank;
            
            bool isPair = sortedCards[0].Rank == sortedCards[1].Rank || sortedCards[1].Rank == sortedCards[2].Rank || sortedCards[0].Rank == sortedCards[2].Rank;

            HandEvaluatedResult result = new HandEvaluatedResult();

            if (isTrail)
            {
                result.Rank = HandRank.Trail;
                result.SortedCards = sortedCards;
            }
            else if (isSequence && isFlush)
            {
                result.Rank = HandRank.PureSequence;
                result.SortedCards = sortedCards;
            }
            else if (isSequence)
            {
                result.Rank = HandRank.Sequence;
                result.SortedCards = sortedCards;
            }
            else if (isFlush)
            {
                result.Rank = HandRank.Color;
                result.SortedCards = sortedCards;
            }
            else if (isPair)
            {
                result.Rank = HandRank.Pair;
                // Ensure the pair cards are at the front of SortedCards for tie-breaking
                if (sortedCards[0].Rank == sortedCards[1].Rank)
                {
                    result.SortedCards = new List<Card> { sortedCards[0], sortedCards[1], sortedCards[2] };
                }
                else if (sortedCards[1].Rank == sortedCards[2].Rank)
                {
                    result.SortedCards = new List<Card> { sortedCards[1], sortedCards[2], sortedCards[0] };
                }
                else // 0 and 2 are pair
                {
                    result.SortedCards = new List<Card> { sortedCards[0], sortedCards[2], sortedCards[1] };
                }
            }
            else
            {
                result.Rank = HandRank.HighCard;
                result.SortedCards = sortedCards;
            }

            return result;
        }
    }
}
