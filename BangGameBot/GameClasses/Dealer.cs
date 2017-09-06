using System;
using System.Collections.Generic;
using System.Linq;

namespace BangGameBot
{
    public class Dealer
    {   
        public List<Card> Deck { get; private set; } = new List<Card>();
        public List<Card> Graveyard { get; private set; } = new List<Card>();
        public List<Card> PeekedCards { get; private set; } = new List<Card>();
        public Dealer()
        {
            MakeNewDeck();

            Deck.Shuffle();
            Deck.Shuffle();
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
        public Tuple<List<Card>, int> DrawCards(int n, Player p)
        {
            var index = -1;
            var cards = new List<Card>();
            for (var i = 0; i < n; i++)
            {
                var tuple = RemoveCard();
                if (tuple.Item2)
                    index = i + 1;
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
        public Tuple<Card, bool> DrawToGraveyard()
        {
            SendToGraveyard(Deck.Last());
            return RemoveCard();
        }

        /// <summary>
        /// Player p discards card c. If c is null, discards a random card from hand. Returns the discarded card
        /// </summary>
        public Card Discard(Player p, Card c = null)
        {
            if (c == null)
                c = p.ChooseCardFromHand();
            p.Cards.Remove(c);
            SendToGraveyard(c);
            return c;
        }

        /// <summary>
        /// Player p discards all his cards
        /// </summary>
        public void DiscardAll(Player p)
        {
            foreach (var c in p.Cards)
                SendToGraveyard(c);
            p.Cards.Clear();
            return;
        }

        public void PutIntoDeck(Player p, Card c)
        {
            if (p.Character != Character.KitCarlson)
                throw new ArgumentException("Someone is putting a card into the deck!");
            p.Cards.Remove(c);
            c.IsOnTable = false;
            Deck.Add(c);
            return;
        }

        /// <summary>
        /// Take the first n cards from the deck, put them in PeekedCards, reshuffling if needed
        /// </summary>
        public Tuple<List<Card>,bool> PeekCards(int n)
        {
            var list = new List<Card>();
            bool reshuffled = false;
            for (var i = 0; i < n; i++)
            {
                var tuple = RemoveCard();
                reshuffled = tuple.Item2;
                list.Add(tuple.Item1);
            }
            PeekedCards = list;
            return new Tuple<List<Card>, bool>(list, reshuffled);
        }

        /// <summary>
        /// Player p draws card c from the previously peeked cards.
        /// </summary>
        /// <returns></returns>
        public void DrawFromPeeked(Player p, Card c)
        {
            if (!PeekedCards.Remove(c))
                throw new ArgumentException("Player is trying to draw a non-peeked card.");
            c.IsOnTable = false;
            p.Cards.Add(c);
            return;
        }
        
        /// <summary>
        /// Puts permcard on table in front of player. If a card is returned, it means the permcard was a weapon and the returned card is the discarded weapon.
        /// </summary>
        /// <returns>The perm card on table.</returns>
        /// <param name="p">P.</param>
        /// <param name="card">Card.</param>
        public Card PutPermCardOnTable(Player p, Card card)
        {
            if (card.GetCardType() == CardType.Normal)
                throw new ArgumentException("Player is putting non-permcard on table");
            if (card.IsOnTable)
                throw new ArgumentException("Card already on table");
            card.IsOnTable = true;
            Card result = null;
            if (card.GetCardType() == CardType.Weapon)
            {
                if (p.Weapon != null)
                {
                    result = p.Weapon;
                    Discard(p, p.Weapon);
                }
                p.Weapon = card;
            }
            return result;
        }

        /// <summary>
        /// Pedro Ramirez draws the top graveyard card. Returns the card drawn.
        /// </summary>
        /// <returns>The from graveyard.</returns>
        /// <param name="p">P.</param>
        public Card DrawFromGraveyard(Player p)
        {
            //only pedro ramirez can!
            if (p.Character != Character.PedroRamirez)
                throw new ArgumentException("Someone is stealing from graveyard!");
            var card = Graveyard.Last();
            p.Cards.Add(card);
            Graveyard.Remove(card);
            return card;
        }

        /// <summary>
        /// Removes a card from the Deck object, and reshuffles the deck if needed.
        /// </summary>
        private Tuple<Card, bool> RemoveCard()
        {
            var card = Deck.Last();
            Deck.Remove(card);
            var deckshuffled = false;
            if (Deck.Count() == 0)
            {
                deckshuffled = true;
                Deck.AddRange(Graveyard);
                Graveyard.Clear();
                Deck.Shuffle();
                Deck.Shuffle();
            }
            return new Tuple<Card, bool>(card, deckshuffled);
        }

        private void SendToGraveyard(Card c)
        {
            c.IsOnTable = false;
            Graveyard.Add(c);
        }

        private void MakeNewDeck()
        {
            for (int i = 2; i <= 14; i++)
            {
                //Bang!
                if (i <= 9)
                    Deck.Add(new Card(CardName.Bang, i, CardSuit.Clubs));
                Deck.Add(new Card(CardName.Bang, i, CardSuit.Diamonds));
                if (i >= 12)
                    Deck.Add(new Card(CardName.Bang, i, CardSuit.Hearts));
                if (i == 14)
                    Deck.Add(new Card(CardName.Bang, i, CardSuit.Spades));

                //Missed
                if (i <= 8)
                    Deck.Add(new Card(CardName.Missed, i, CardSuit.Spades));
                if (i >= 10)
                    Deck.Add(new Card(CardName.Missed, i, CardSuit.Clubs));

                //Beer
                if (i >= 6 && i <= 11)
                    Deck.Add(new Card(CardName.Beer, i, CardSuit.Hearts));
            }

            //normal cards
            Deck.Add(new Card(CardName.Panic, 11, CardSuit.Hearts));
            Deck.Add(new Card(CardName.Panic, 12, CardSuit.Hearts));
            Deck.Add(new Card(CardName.Panic, 14, CardSuit.Hearts));
            Deck.Add(new Card(CardName.Panic, 8, CardSuit.Diamonds));
            Deck.Add(new Card(CardName.CatBalou, 9, CardSuit.Diamonds));
            Deck.Add(new Card(CardName.CatBalou, 10, CardSuit.Diamonds));
            Deck.Add(new Card(CardName.CatBalou, 11, CardSuit.Diamonds));
            Deck.Add(new Card(CardName.CatBalou, 13, CardSuit.Hearts));
            Deck.Add(new Card(CardName.Stagecoach, 9, CardSuit.Spades));
            Deck.Add(new Card(CardName.Stagecoach, 9, CardSuit.Spades));
            Deck.Add(new Card(CardName.WellsFargo, 3, CardSuit.Hearts));
            Deck.Add(new Card(CardName.Gatling, 10, CardSuit.Hearts));
            Deck.Add(new Card(CardName.Duel, 12, CardSuit.Diamonds));
            Deck.Add(new Card(CardName.Duel, 11, CardSuit.Spades));
            Deck.Add(new Card(CardName.Duel, 8, CardSuit.Clubs));
            Deck.Add(new Card(CardName.Indians, 13, CardSuit.Diamonds));
            Deck.Add(new Card(CardName.Indians, 14, CardSuit.Diamonds));
            Deck.Add(new Card(CardName.GeneralStore, 9, CardSuit.Clubs));
            Deck.Add(new Card(CardName.GeneralStore, 12, CardSuit.Spades));
            Deck.Add(new Card(CardName.Saloon, 5, CardSuit.Hearts));

            //permcards
            Deck.Add(new Card(CardName.Jail, 10, CardSuit.Spades));
            Deck.Add(new Card(CardName.Jail, 11, CardSuit.Spades));
            Deck.Add(new Card(CardName.Jail, 4, CardSuit.Hearts));
            Deck.Add(new Card(CardName.Dynamite, 2, CardSuit.Hearts));
            Deck.Add(new Card(CardName.Barrel, 12, CardSuit.Spades));
            Deck.Add(new Card(CardName.Barrel, 13, CardSuit.Spades));
            Deck.Add(new Card(CardName.Scope, 14, CardSuit.Spades));
            Deck.Add(new Card(CardName.Mustang, 8, CardSuit.Hearts));
            Deck.Add(new Card(CardName.Mustang, 9, CardSuit.Hearts));

            //weapons
            Deck.Add(new Card(CardName.Volcanic, 10, CardSuit.Clubs));
            Deck.Add(new Card(CardName.Volcanic, 10, CardSuit.Spades));
            Deck.Add(new Card(CardName.Schofield, 11, CardSuit.Clubs));
            Deck.Add(new Card(CardName.Schofield, 12, CardSuit.Clubs));
            Deck.Add(new Card(CardName.Schofield, 13, CardSuit.Spades));
            Deck.Add(new Card(CardName.Remington, 13, CardSuit.Clubs));
            Deck.Add(new Card(CardName.RevCarabine, 14, CardSuit.Clubs));
            Deck.Add(new Card(CardName.Winchester, 8, CardSuit.Spades));
        }
    }
}