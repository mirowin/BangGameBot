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
        
        public GameStatus Status = GameStatus.Joining;
        public int Id = 1;
        public List<Player> Users = new List<Player>(); // The players that started the game 
        public List<Player> Players; // Players during current round
        public List<Player> AlivePlayers => Players.Where(x => !x.IsDead).ToList();
        public IEnumerable<Player> Watchers => Users.Where(x => !x.HasLeftGame);
        public Dealer Dealer = new Dealer();
        private int Turn = -1;
        private Queue<Tuple<Tuple<Player, CallbackQuery>, Request>> _requests = new Queue<Tuple<Tuple<Player, CallbackQuery>, Request>>(); //this thing is VERY ugly, but idc.

        public Game(Player player)
        {
            if (player == null)
            {
                var result = 0;
                do
                {
                    result = Program.R.Next(10000, 10000000);
                } while (Program.Games.Any(x => x.Id == result));
                Id = result;
            }
            else
            {
                Users.Add(player);
                player.PlayerListMsg = Bot.Send("You have been added to the wait list.\nWaiting for other players to join...", player.Id).Result;
            }
            new Thread(() => JoiningPhase()).Start();

            return;
        }

        private void JoiningPhase()
        {
            int inactivetime = 0;
            while (Status == GameStatus.Joining)
            {
                if (inactivetime > 60 * GameSettings.InactiveMinutes)
                {
                    Users?.ForEach(u =>
                    {
                        if (u.PlayerListMsg != null)
                            Bot.Edit("Inactive game, cancelling...", u.PlayerListMsg).Wait();
                        Bot.Send("The game was cancelled for inactivity. If you still want to play, please start a /newgame.", u.Id);
                    });
                    this.Dispose();
                    return;
                }
                if (!_requests.Any())
                {
                    inactivetime += 1;
                    Task.Delay(1000).Wait();
                    continue;
                }
                inactivetime = 0;
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
                if (Users == null)
                    return;
                if (!Users.Any())
                    continue;
                var startinggame = Users.All(x => x.VotedToStart) || Users.Count() == GameSettings.MaxPlayers;
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
            if (Users.Count() < GameSettings.MinPlayers)
                Users.ForEach(x => x.VotedToStart = false);

            return;
        }

        public void LeaveGame(Player p, CallbackQuery q = null)
        {
            p.HasLeftGame = true;
            if (q != null)
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
            Program.Games.Remove(this);
            Users?.Clear();
            Users = null;
            Players?.Clear();
            Players = null;
            Dealer = null;
            return;
        }


        private static class GameSettings
        {
            public static readonly int InactiveMinutes = 10;
            public static readonly int MinPlayers = 2;
            public static readonly int MaxPlayers = 7;

            //TIMES

            //bool choices
            public static readonly int AbilityPhaseOneTime = 45;
            public static readonly int SidKetchumAbilityPhaseThreeTime = 30;
            public static readonly int ChooseUseBarrelTime = 30;
            
            //misses
            public static readonly int MissGatlingTime = 45;
            public static readonly int MissBangTime = 45;
            public static readonly int MissDuelTime = 45;
            public static readonly int MissIndiansTime = 45;
            public static readonly int LethalHitTime = 45;

            //target choices
            public static readonly int ChooseBangTargetTime = 60;
            public static readonly int ChooseJailTargetTime = 60;
            public static readonly int ChooseDuelTargetTime = 60;
            public static readonly int ChoosePanicTargetTime = 60;

            //card choices
            public static readonly int PhaseTwoTime = 120;
            public static readonly int SidKetchumAbilityTime = 60;
            public static readonly int GeneralStoreTime = 90;
            public static readonly int PhaseThreeTime = 60;
            public static readonly int ChooseCardToStealTime = 75;
            public static readonly int SidKetchumLethalAbilityTime = 45;

            public static readonly int LuckyDukeAbilityTime = 45;
        }
    }
}

