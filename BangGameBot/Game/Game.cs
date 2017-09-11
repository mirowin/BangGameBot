using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace BangGameBot
{
    public partial class Game : IDisposable
    {
        public static readonly int MinPlayers = 4;
        public static readonly int MaxPlayers = 7;
        public GameStatus Status = GameStatus.Joining;
        public List<Player> Users = new List<Player>(); // The players that started the game 
        public List<Player> Players; // Players during current round
        public List<Player> AlivePlayers => Players.Where(x => !x.IsDead).ToList();
        public IEnumerable<Player> Watchers => Users.Where(x => !x.HasLeftGame);
        public Dealer Dealer = new Dealer();
        private int Turn = -1;

        public Game (Message msg) {
            AddPlayer(msg.From);
            return;
        }
        
        public void AddPlayer (User u) {
            Users.Add(new Player(u));
            UpdateJoinMessages(Users.Count() == MaxPlayers, true);
            if (Users.Count() == MaxPlayers)
                StartGame();
            return;
        }

        public void PlayerLeave (Player p, CallbackQuery q) {
            if (Status == GameStatus.Joining)
            {
                Bot.Edit("You have been removed from the game.", q.Message).Wait();
                Users.Remove(p);
                if (Users.Count() == 0)
                {
                    this.Dispose();
                    return;
                }
                if (Users.Count < MinPlayers)
                    Users.ForEach(x => x.VotedToStart = false);
                UpdateJoinMessages(false, true);
            }
            else
            {
                p.HasLeftGame = true;
                Bot.Edit("You have left this game. Start a new one with /newgame.", q.Message).Wait();
                if (Players.All(x => x.HasLeftGame))
                {
                    if (Watchers.Any())
                        foreach (var w in Watchers)
                            Bot.Send("Everyone left the game! The game is cancelled.", w.Id);
                    this.Dispose();
                }
            }
                
            return;

        }

        public void VoteStart (Player p) {
            p.VotedToStart = !p.VotedToStart;
            UpdateJoinMessages(Users.All(x => x.VotedToStart), false);
            if (Users.All(x => x.VotedToStart))
                Task.Delay(2000).Wait(); //wait for last second clicks of the buttons
            if (Users.All(x => x.VotedToStart))
                StartGame();
            else
                UpdateJoinMessages(false, false);
            return;
        }

        public void Dispose()
        {
            Status = GameStatus.Ending;
            Handler.Games.Remove(this);
            Users.Clear();
            Users = null;
            if (Players != null)
                Players.Clear();
            Players = null;
            Dealer = null;
            return;
        }
    }
}

