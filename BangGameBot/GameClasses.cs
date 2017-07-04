using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace BangGameBot
{
    public class Player
    {
        public long Id;
        public User TelegramUser;
        public Message JoinMsg;
        public bool VotedToStart;
        public List<Card> Cards;

        public Player (User u) {
            TelegramUser = u;
            Id = u.Id;
        }
    }

    public class Card 
    {
        public CardType Type { get; }
        public bool IsPermCard { get; }
        public bool IsOnTable { get; set; }
        public int Number { get; }
        public CardSuit Suit { get; }
        public Card (CardType t, int n, CardSuit s) {
            Type = t;
            Number = n;
            Suit = s;
            IsPermCard = t.CompareTo(CardType.Saloon) > 0;
        }
    }

    public class Dealer 
    {
        public List<Card> Deck = new List<Card>();
        public List<Card> Graveyard = new List<Card>();
        public Dealer () {
            for (int i = 2; i <= 14; i++) {

                //Bang!
                if (i <= 9)
                    Deck.Add(new Card(CardType.Bang, i, CardSuit.Clubs));
                Deck.Add(new Card(CardType.Bang, i, CardSuit.Diamonds));
                if (i >= 12)
                    Deck.Add(new Card(CardType.Bang, i, CardSuit.Hearts));
                if (i == 14)
                    Deck.Add(new Card(CardType.Bang, i, CardSuit.Spades));

                //Missed
                if (i <= 8)
                    Deck.Add(new Card(CardType.Missed, i, CardSuit.Spades));
                if (i >=10)
                    Deck.Add(new Card(CardType.Missed, i, CardSuit.Clubs));

                //Beer
                if (i >= 6 && i <= 11)
                    Deck.Add(new Card(CardType.Beer, i, CardSuit.Hearts));
            }

            Deck.Add(new Card(CardType.Panic, 11, CardSuit.Hearts));
            Deck.Add(new Card(CardType.Panic, 12, CardSuit.Hearts));
            Deck.Add(new Card(CardType.Panic, 14, CardSuit.Hearts));
            Deck.Add(new Card(CardType.Panic, 8, CardSuit.Diamonds));
            Deck.Add(new Card(CardType.CatBalou, 9, CardSuit.Diamonds));
            Deck.Add(new Card(CardType.CatBalou, 10, CardSuit.Diamonds));
            Deck.Add(new Card(CardType.CatBalou, 11, CardSuit.Diamonds));
            Deck.Add(new Card(CardType.CatBalou, 13, CardSuit.Hearts));
            Deck.Add(new Card(CardType.Stagecoach, 9, CardSuit.Spades));
            Deck.Add(new Card(CardType.Stagecoach, 9, CardSuit.Spades));
            Deck.Add(new Card(CardType.WellsFargo, 3, CardSuit.Hearts));
            Deck.Add(new Card(CardType.Gatling, 10, CardSuit.Hearts));
            Deck.Add(new Card(CardType.Duel, 12, CardSuit.Diamonds));
            Deck.Add(new Card(CardType.Duel, 12, CardSuit.Diamonds));
            Deck.Add(new Card(CardType.Duel, 11, CardSuit.Spades));
            Deck.Add(new Card(CardType.Duel, 8, CardSuit.Clubs));
            Deck.Add(new Card(CardType.Indians, 13, CardSuit.Diamonds));
            Deck.Add(new Card(CardType.Indians, 14, CardSuit.Diamonds));
            Deck.Add(new Card(CardType.GeneralStore, 9, CardSuit.Clubs));
            Deck.Add(new Card(CardType.GeneralStore, 12, CardSuit.Spades));
            Deck.Add(new Card(CardType.Saloon, 5, CardSuit.Hearts));


            Deck.Shuffle();
            Deck.Shuffle();
        }

        /// <summary>
        /// Player p draws n cards from deck.
        /// 
        /// Returns:
        /// 1. The cards drawn.
        /// 2. The position where the deck was reshuffled. (-1 if it wasn't)
        /// </summary>
        public Tuple<List<Card>,int> DrawCards (int n, Player p) {
            var index = -1;
            var cards = new List<Card>();
            for (var i = 0; i < n; i++) {
                var tuple = RemoveCard();
                if (tuple.Item2)
                    index = i+1;
                p.Cards.Add(tuple.Item1);
                cards.Add(tuple.Item1);
            }
            return new Tuple<List<Card>, int>(cards, index);
        }

        /// <summary>
        /// Draws a card, shows it, and moves it to the graveyard.
        /// 
        /// Returns:
        /// 1. The card drawn.
        /// 2. Whether the deck was reshuffled after drawing the card.
        /// </summary>
        /// <returns>The to graveyard.</returns>
        public Tuple<Card, bool> DrawToGraveyard () {
            Graveyard.Add(Deck[0]);
            return RemoveCard();
        }

        /// <summary>
        /// Player p discards card c.
        /// </summary>
        public void Discard (Player p, Card c) {
            p.Cards.Remove(c);
            Graveyard.Add(c);
        }


        private Tuple<Card,bool> RemoveCard () {
            var card = Deck[0];
            Deck.Remove(card);
            var deckshuffled = false;
            if (Deck.Count() == 0) {
                deckshuffled = true;
                Deck.AddRange(Graveyard);
                Graveyard.Clear();
                Deck.Shuffle();
                Deck.Shuffle();
            }
            return new Tuple<Card, bool>(card,deckshuffled);
        }
    }

    public enum CardType {
        //Cards
        Bang, Missed, Beer, Panic, CatBalou, Stagecoach, WellsFargo, Gatling, Duel, Indians, GeneralStore, Saloon,
        //PermCards
        Jail, Dynamite, Barrel, Scope, Mustang
    }

    public enum CardSuit {
        Hearts, Diamonds, Clubs, Spades
    }
}

