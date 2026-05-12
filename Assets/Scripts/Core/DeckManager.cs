using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CallKitty.Core
{
    public class DeckManager : MonoBehaviour
    {
        private List<Card> _deck = new List<Card>();

        public void InitializeDeck()
        {
            _deck.Clear();
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank rank in Enum.GetValues(typeof(Rank)))
                {
                    _deck.Add(new Card(suit, rank));
                }
            }
        }

//fisher-yates algorithm
        public void ShuffleDeck()
        {
            int n = _deck.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                Card value = _deck[k];
                _deck[k] = _deck[n];
                _deck[n] = value;
            }
        }

        public List<Card> DealHand(int numberOfCards)
        {
            if (_deck.Count < numberOfCards)
            {
                Debug.LogError("Not enough cards in the deck to deal.");
                return new List<Card>();
            }

            List<Card> hand = _deck.Take(numberOfCards).ToList();
            _deck.RemoveRange(0, numberOfCards);
            return hand;
        }

        public int CardsRemaining => _deck.Count;
    }
}
