using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types;

namespace BangGameBot
{
    public class PlayerSettings
    {
        public int Id { get; set; }
        public long TelegramId { get; set; }
        public bool HelpMode { get; set; }

        public PlayerSettings()
        {
        }
    }

    public class Player
    {
        //USER
        public long Id { get; }
        public string Name { get; }
        //public User TelegramUser { get; }

        public bool HelpMode;

        //MESSAGES
        public Message PlayerListMsg = null;
        public GameMessage QueuedMsg = new GameMessage();
        public Message CurrentMsg = null;

        //GAME
        public bool VotedToStart = false;
        public bool UsedBang = false;
        public bool IsDead = false;
        public Choice Choice = null;
        public List<Card> Cards = new List<Card>();
        public int Lives { get; private set; }
        public Character Character;
        public Role Role;
        public Card Weapon = null;
        public bool HasLeftGame = false;
        public bool Won = false;

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
            //TelegramUser = u;
            Id = u.Id;
            Name = (u.FirstName.Length < 50 ? u.FirstName : u.FirstName.Substring(0, 50) + "...").FormatHTML();

            //set helpmode
            using (var db = new LiteDatabase(Program.LiteDBConnectionString))
            {
                var settings = db.GetCollection<PlayerSettings>("settings");
                var playersettings = settings.FindOne(x => x.TelegramId == Id);
                if (playersettings == null)
                {
                    playersettings = new PlayerSettings { TelegramId = Id, HelpMode = false };
                    settings.Insert(playersettings);
                }

                HelpMode = playersettings.HelpMode;
            }
        }

        public class GameMessage
        {
            public string Text = "";
            public List<CardName> CardsUsed = new List<CardName>();
            public List<Character> Characters = new List<Character>();

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


        public int DistanceSeen(Player target, List<Player> players)
        {
            if (target.IsDead)
                return 0;
            if (Id == target.Id)
                return -1;
            var i = players.IndexOf(this);
            var j = players.IndexOf(target);
            //direct distance
            var dist1 = Math.Abs(j - i);
            //cycling distance
            var dist2 = players.Count() - Math.Max(i, j) + Math.Min(i, j);
            var distance = Math.Min(dist1, dist2);
            //take characters & cards into account!
            if (target.Character == Character.PaulRegret)
                distance++;
            if (target.CardsOnTable.Any(x => x.Name == CardName.Mustang))
                distance++;
            if (Character == Character.RoseDoolan)
                distance--;
            if (CardsOnTable.Any(x => x.Name == CardName.Scope))
                distance--;
            return distance < 1 ? 1 : distance;
        }

        public bool IsReachableBy(Player source, List<Player> players)
        {
            if (Id == source.Id)
                return false;
            return source.DistanceSeen(this, players) <= (source.Weapon?.GetReachDistance() ?? 1);
        }

        public Card ChooseCardFromHand()
        {
            return CardsInHand.Random();
        }


        public string LivesString()
        {
            string r = " ";
            for (var i = 0; i < Lives; i++)
            {
                r += "❤️";
            }
            if (IsDead)
                r = "💀 - " + Role.GetString<Role>();
            return r;
        }
    }
}