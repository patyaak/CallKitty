namespace CallKitty.Core
{
    public enum HandRank
    {
        HighCard = 1,
        Pair = 2,
        Color = 3,       // Flush (same suit, not consecutive)
        Sequence = 4,    // Mixed suits straight
        PureSequence = 5,// Same suit straight
        Trail = 6        // Trio (Three of a kind)
    }
}
