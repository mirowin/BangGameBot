using System;

namespace BangGameBot
{
    public class Card
    {
        public int Id { get; }
        public CardName Name { get; }
        public bool IsOnTable { get; set; } = false;
        public int Number { get; }
        public CardSuit Suit { get; }
        public Card(CardName t, int n, CardSuit s)
        {
            Id = NextId++;
            Name = t;
            Number = n;
            Suit = s;
        }

        private static int NextId = 0;

        public int GetReachDistance()
        {
            switch (Name)
            {
                case CardName.Volcanic:
                    return 1;
                case CardName.Schofield:
                    return 2;
                case CardName.Remington:
                    return 3;
                case CardName.RevCarabine:
                    return 4;
                case CardName.Winchester:
                    return 5;
                default:
                    throw new ArgumentException("Card should be a Weapon");
            }
        }


        public string GetDescription()
        {
            var numberstring = "";
            switch (Number)
            {
                case 11:
                    numberstring = "J";
                    break;
                case 12:
                    numberstring = "Q";
                    break;
                case 13:
                    numberstring = "K";
                    break;
                case 14:
                    numberstring = "A";
                    break;
                default:
                    numberstring = Number.ToString();
                    break;
            }
            return Name.GetString<CardName>() + "[" + numberstring + Suit.ToEmoji() + "]";
        }

        public string GetButtonText()
        {
            string emoji = "";
            switch (GetCardType())
            {
                case CardType.Weapon:
                    emoji = "🔫" + GetReachDistance().ToEmoji();
                    break;
                case CardType.PermCard:
                    emoji = "🔵";
                    break;
            }
            return emoji+GetDescription();
        }


        public string Encode()
        {
            return Id.ToString();
        }

        public CardType GetCardType()
        {
            switch (Name)
            {
                case CardName.Jail:
                case CardName.Dynamite:
                case CardName.Barrel:
                case CardName.Scope:
                case CardName.Mustang:
                    return CardType.PermCard;
                case CardName.Volcanic:
                case CardName.Schofield:
                case CardName.Remington:
                case CardName.RevCarabine:
                case CardName.Winchester:
                    return CardType.Weapon;
                default:
                    return CardType.Normal;
            }
        }
    }
}