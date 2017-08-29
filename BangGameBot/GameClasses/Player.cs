using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;

namespace BangGameBot
{
    public class Settings
    {
        public int Id { get; set; }
        public long TelegramId { get; set; }
        public bool HelpMode { get; set; }

        public Settings()
        {
        }
    }

    public class Player
    {
        //USER
        public long Id { get; }
        public string Name { get; }
        public User TelegramUser { get; }

        public bool HelpMode {
            get
            {
                using (var db = new LiteDatabase("BangDB.db"))
                {
                    var settings = db.GetCollection<Settings>("settings");
                    var playersettings = settings.FindOne(x => x.TelegramId == Id);
                    if (playersettings == null)
                    {
                        playersettings = new Settings { TelegramId = Id, HelpMode = false };
                        settings.Insert(playersettings);
                    }
                    return playersettings.HelpMode;
                }
            }
        }

        //MESSAGES
        public Message PlayerListMsg = null;
        public GameMessage QueuedMsg = new GameMessage();
        public Message CurrentMsg = null;

        //GAME
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
        public List<Card> CardsInHand => Cards.Where(x => !x.IsOnTable).ToList();
        public int MaxLives => (Role == Role.Sheriff ? 1 : 0) + (new[] { Character.PaulRegret, Character.ElGringo }.Contains(Character) ? 3 : 4);

        public Player(User u)
        {
            TelegramUser = u;
            Id = u.Id;
            Name = (u.FirstName.Length < 10 ? u.FirstName : u.FirstName.Substring(0, 10) + "...").FormatHTML();
        }

        public class GameMessage
        {
            public string Text = "";
            public List<CardName> CardsUsed = new List<CardName>();
            public List<Character> Characters = new List<Character>();
            public bool Blank => String.IsNullOrWhiteSpace(Text) && CardsUsed.Count() + Characters.Count() == 0;

            public void Clear()
            {
                Text = "";
                CardsUsed.Clear();
                Characters.Clear();
            }
        };


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