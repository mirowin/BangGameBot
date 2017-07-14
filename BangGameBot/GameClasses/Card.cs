namespace BangGameBot
{
    public class Card
    {
        public CardName Name { get; }
        public CardType Type { get; }
        public bool IsOnTable { get; set; } = false;
        public int Number { get; }
        public CardSuit Suit { get; }
        public Card(CardName t, int n, CardSuit s)
        {
            Name = t;
            Number = n;
            Suit = s;
            Type = t.CompareTo(CardName.Saloon) > 0 ? (t.CompareTo(CardName.Mustang) > 0 ? CardType.Weapon : CardType.PermCard) : CardType.Normal;
        }
    }
}