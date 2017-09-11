using System;
using System.Collections.Generic;

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
            int reachdistance;
            switch (Name)
            {
                case CardName.Schofield:
                    reachdistance = 2;
                    break;
                case CardName.Remington:
                    reachdistance = 3;
                    break;
                case CardName.RevCarabine:
                    reachdistance = 4;
                    break;
                case CardName.Winchester:
                    reachdistance = 5;
                    break;
                default:
                    throw new ArgumentException("c should be a Weapon");
            }
            return reachdistance;
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
            string description = "";
            switch (GetCardType())
            {
                case CardType.Weapon:
                    description = "🔫" + GetReachDistance().ToEmoji();
                    break;
                case CardType.PermCard:
                    description = "🔵";
                    break;
            }
            return GetDescription() + description;
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