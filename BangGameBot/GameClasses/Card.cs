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
    }
}