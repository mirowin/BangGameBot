using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        private Queue<Tuple<Tuple<Player, CallbackQuery>, Request>> _requests = new Queue<Tuple<Tuple<Player, CallbackQuery>, Request>>(); //this thing is VERY ugly, but idc.

        public Game(Player player)
        {
            Users.Add(player);
            player.PlayerListMsg = Bot.Send("You have been added to the wait list.\nWaiting for other players to join...", player.Id).Result;
            new Thread(() => JoiningPhase()).Start();

            return;
        }

        private void JoiningPhase()
        {
            while (Status == GameStatus.Joining)
            {
                if (!_requests.Any())
                {
                    Task.Delay(250).Wait();
                    continue;
                }

                var request = _requests.Dequeue();
                var p = request.Item1.Item1;
                var q = request.Item1.Item2;
                switch (request.Item2)
                {
                    case Request.Join:
                        Users.Add(p);
                        break;
                    case Request.Leave:
                        PlayerLeave(p, q);
                        break;
                    case Request.VoteStart:
                        p.VotedToStart = !p.VotedToStart;
                        break;
                }

                var startinggame = Users.All(x => x.VotedToStart);
                if (startinggame)
                    Status = GameStatus.Initialising;
                UpdateJoinMessages(startinggame, request.Item2 != Request.VoteStart);
            }


            StartGame();
        }

        internal void PlayerRequest(Player player, Request request, CallbackQuery q = null)
        {
            _requests.Enqueue(new Tuple<Tuple<Player, CallbackQuery>, Request>(new Tuple<Player, CallbackQuery>(player, q), request));
        }

        public void PlayerLeave(Player p, CallbackQuery q)
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

            return;
        }

        public void LeaveGame(Player p, CallbackQuery q)
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

