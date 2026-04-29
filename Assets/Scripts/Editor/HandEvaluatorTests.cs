using System.Collections.Generic;
using NUnit.Framework;
using CallKitty.Core;

namespace CallKitty.Tests
{
    public class HandEvaluatorTests
    {
        [Test]
        public void Trail_Beats_PureSequence()
        {
            var trailHand = new List<Card> { new Card(Suit.Spades, Rank.Two), new Card(Suit.Hearts, Rank.Two), new Card(Suit.Clubs, Rank.Two) };
            var pSeqHand = new List<Card> { new Card(Suit.Spades, Rank.Ace), new Card(Suit.Spades, Rank.King), new Card(Suit.Spades, Rank.Queen) };

            var res1 = HandEvaluator.Evaluate3CardHand(trailHand);
            var res2 = HandEvaluator.Evaluate3CardHand(pSeqHand);

            Assert.AreEqual(HandRank.Trail, res1.Rank);
            Assert.AreEqual(HandRank.PureSequence, res2.Rank);
            Assert.IsTrue(res1.CompareTo(res2) > 0);
        }

        [Test]
        public void HigherTrail_Beats_LowerTrail()
        {
            var highTrail = new List<Card> { new Card(Suit.Spades, Rank.Three), new Card(Suit.Hearts, Rank.Three), new Card(Suit.Clubs, Rank.Three) };
            var lowTrail = new List<Card> { new Card(Suit.Spades, Rank.Two), new Card(Suit.Hearts, Rank.Two), new Card(Suit.Clubs, Rank.Two) };

            var res1 = HandEvaluator.Evaluate3CardHand(highTrail);
            var res2 = HandEvaluator.Evaluate3CardHand(lowTrail);

            Assert.IsTrue(res1.CompareTo(res2) > 0);
        }

        [Test]
        public void Pair_TieBreaker_Works()
        {
            // Pair of 8s, Ace kicker
            var pair1 = new List<Card> { new Card(Suit.Spades, Rank.Eight), new Card(Suit.Hearts, Rank.Eight), new Card(Suit.Clubs, Rank.Ace) };
            // Pair of 8s, King kicker
            var pair2 = new List<Card> { new Card(Suit.Diamonds, Rank.Eight), new Card(Suit.Clubs, Rank.Eight), new Card(Suit.Spades, Rank.King) };

            var res1 = HandEvaluator.Evaluate3CardHand(pair1);
            var res2 = HandEvaluator.Evaluate3CardHand(pair2);

            Assert.AreEqual(HandRank.Pair, res1.Rank);
            Assert.AreEqual(HandRank.Pair, res2.Rank);
            Assert.IsTrue(res1.CompareTo(res2) > 0);
        }

        [Test]
        public void A23_IsSequence()
        {
            var hand = new List<Card> { new Card(Suit.Spades, Rank.Ace), new Card(Suit.Hearts, Rank.Two), new Card(Suit.Clubs, Rank.Three) };
            var res = HandEvaluator.Evaluate3CardHand(hand);

            Assert.AreEqual(HandRank.Sequence, res.Rank);
            // In A-2-3, 3 is the highest card in the sequence
            Assert.AreEqual(Rank.Three, res.SortedCards[0].Rank);
        }
        
        [Test]
        public void HighCard_TieBreaker()
        {
            // A, K, 9
            var hand1 = new List<Card> { new Card(Suit.Spades, Rank.Ace), new Card(Suit.Hearts, Rank.King), new Card(Suit.Clubs, Rank.Nine) };
            // A, K, 8
            var hand2 = new List<Card> { new Card(Suit.Diamonds, Rank.Ace), new Card(Suit.Clubs, Rank.King), new Card(Suit.Spades, Rank.Eight) };

            var res1 = HandEvaluator.Evaluate3CardHand(hand1);
            var res2 = HandEvaluator.Evaluate3CardHand(hand2);

            Assert.AreEqual(HandRank.HighCard, res1.Rank);
            Assert.AreEqual(HandRank.HighCard, res2.Rank);
            Assert.IsTrue(res1.CompareTo(res2) > 0);
        }
    }
}
