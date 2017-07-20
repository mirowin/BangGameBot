namespace BangGameBot
{
    public class Card
    {
        public int Id { get; }
        public CardName Name { get; }
        public CardType Type { get; }
        public bool IsOnTable { get; set; } = false;
        public int Number { get; }
        public CardSuit Suit { get; }
        public Card(CardName t, int n, CardSuit s)
        {
            Id = NextId++;
            Name = t;
            Number = n;
            Suit = s;
            Type = t.CompareTo(CardName.Saloon) > 0 ? (t.CompareTo(CardName.Mustang) > 0 ? CardType.Weapon : CardType.PermCard) : CardType.Normal;
        }

        private static int NextId = 0; //TODO this is going to be very big, if i don't make it per game!
    }
}