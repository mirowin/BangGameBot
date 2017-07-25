﻿using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace BangGameBot
{
    public partial class Game
    {
        public readonly int MinPlayers = 4;
        public readonly int MaxPlayers = 7;
        public readonly string SheriffIndicator = " SHERIFF ";
        public int Id { get; } = 0;
        public static int NextId = 0;
        public GameStatus Status = GameStatus.Joining;
        public List<Player> Players = new List<Player>();
        public List<Player> DeadPlayers = new List<Player>();
        private Dealer Dealer = new Dealer();
        private int Turn = -1;

        public Game (Message msg) {
            Id = NextId++;

            AddPlayer(msg.From);
            return;
        }
        
        public void AddPlayer (User u) {
            Players.Add(new Player(u));
            UpdateJoinMessages(Players.Count() == MaxPlayers, true);
            if (Players.Count() == MaxPlayers)
                StartGame();
            return;
        }

        public void PlayerLeave (Player p) {
            Players.Remove(p);
            Bot.Edit("You have been removed from the game.", p.PlayerListMsg);
            if (Players.Count() == 0) {
                Handler.Games.Remove(this);
                Players?.Clear();
                Players = null;
                return;
            }
            if (Players.Count < MinPlayers)
                Players.ForEach(x => x.VotedToStart = false);
            UpdateJoinMessages(false, true);
            return;
        }

        public void VoteStart (Player p) {
            p.VotedToStart = !p.VotedToStart;
            UpdateJoinMessages(Players.All(x => x.VotedToStart), false);
            if (Players.All(x => x.VotedToStart))
                StartGame();
            return;
        }

       
        
    }
}

