using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace BangGameBot
{
    public class Player
    {
        public long Id { get; }
        public string Name { get; }
        public User TelegramUser { get; }
        public Message PlayerListMsg = null;
        public Message TurnMsg = null;
        public string CurrentMsg = "";
        public IReplyMarkup CurrentMenu = null;
        public string QueuedMsg = "";
        public DateTime LastMessage = DateTime.MinValue;
        public bool VotedToStart = false;
        public bool UsedBang = false;
        public Choice Choice = null;
        public List<Card> Cards = new List<Card>();
        public int Lives { get; private set; }
        public Character Character;
        public Role Role;
        public Card Weapon = null;
        public List<Card> CardsOnTable
        {
            get
            {
                var r = Cards.Where(x => x.IsOnTable).ToList();
                if (r.Any(x => x.GetCardType() == CardType.Normal))
                    throw new Exception($"Card on table is normal");
                return r;
            }
        }
        public List<Card> CardsInHand
        {
            get { return Cards.Where(x => !x.IsOnTable).ToList(); }
        }
        public int MaxLives
        {
            get { return (Role == Role.Sheriff ? 1 : 0) + (new[] { Character.PaulRegret, Character.ElGringo }.Contains(Character) ? 3 : 4); }
        }

        public Player(User u)
        {
            TelegramUser = u;
            Id = u.Id;
            Name = (u.FirstName.Length < 10 ? u.FirstName : u.FirstName.Substring(0, 10) + "...").FormatHTML();
        }

        /// <summary>
        /// Steals card c from player p. If card is null, a random card from hand is chosen. Returns the stolen card
        /// </summary>
        public Card StealFrom(Player p, Card c = null)
        {
            if (c == null)
                c = p.ChooseCardFromHand();
            c.IsOnTable = false;
            p.Cards.Remove(c);
            this.Cards.Add(c);
            return c;
        }

        public void SetLives()
        {
            Lives = MaxLives;
        }

        public void AddLives(int n)
        {
            Lives += n;
            if (Lives > MaxLives)
                Lives = MaxLives;
        }
    }
}