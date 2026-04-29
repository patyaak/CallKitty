using UnityEngine;
using CallKitty.Core;

namespace CallKitty.UI
{
    [CreateAssetMenu(fileName = "CardDatabase", menuName = "CallKitty/Card Database")]
    public class CardDatabase : ScriptableObject
    {
        [System.Serializable]
        public struct CardSpriteMapping
        {
            public Suit suit;
            public Rank rank;
            public Sprite sprite;
        }

        public CardSpriteMapping[] cardSprites;
        public Sprite cardBack;

        public Sprite GetSprite(Card card)
        {
            foreach (var mapping in cardSprites)
            {
                if (mapping.suit == card.Suit && mapping.rank == card.Rank)
                {
                    return mapping.sprite;
                }
            }
            return null;
        }
    }
}
