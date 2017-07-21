using System;
using System.Collections.Generic;

namespace BangGameBot
{
    public class Card : IDisposable
    {
        public int Id { get; }
        public CardName Name { get; }
        public CardType Type { get; }
        public bool IsOnTable { get; set; } = false;
        public int Number { get; }
        public CardSuit Suit { get; }
        public Card(CardName t, int n, CardSuit s)
        {
            lock (Lock)
            {
                int nextIndex = GetAvailableIndex();
                if (nextIndex == -1)
                {
                    nextIndex = UsedCounter.Count;
                    UsedCounter.Add(true);
                }

                Id = nextIndex;
            }   
            Name = t;
            Number = n;
            Suit = s;
            Type = t.CompareTo(CardName.Saloon) > 0 ? (t.CompareTo(CardName.Mustang) > 0 ? CardType.Weapon : CardType.PermCard) : CardType.Normal;
        }

        private static List<bool> UsedCounter = new List<bool>();
        private static object Lock = new object();

        public void Dispose()
        {
            lock (Lock)
            {
                UsedCounter[Id] = false;
            }
        }

        private int GetAvailableIndex()
        {
            for (int i = 0; i < UsedCounter.Count; i++)
            {
                if (UsedCounter[i] == false)
                {
                    return i;
                }
            }

            // Nothing available.
            return -1;
        }
    }
}