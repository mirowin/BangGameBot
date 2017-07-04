using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace BangGameBot
{
    public class Game
    {
        private int MinPlayers = 2;
        public int Id = 0;
        public GameStatus Status = GameStatus.Joining;
        public List<Player> Players = new List<Player>();
        private Dealer Dealer = new Dealer();

        public Game (Message msg) {
            var i = 1;
            do {
                if (Handler.Games.Any(x => x.Id == i))
                    i++;
                else
                    Id = i;
            } while (Id == 0);
            
            AddPlayer(msg.From);
        }

        public void AddPlayer (User u) {
            Players.Add(new Player(u));
            if (Players.Count() == 8)
                StartGame();
            else
                UpdateJoinMessages();
            return;
        }

        public void PlayerLeave (User u) {
            var p = Players.FirstOrDefault(x => x.Id == u.Id);
            Players.Remove(p);
            Bot.Edit("You have been removed from the game.", p.JoinMsg);
            if (Players.Count() == 0) {
                Handler.Games.Remove(this);
                Players?.Clear();
                Players = null;
                return;
            }
            if (Players.Count < MinPlayers)
                Players.ForEach(x => x.VotedToStart = false);
            UpdateJoinMessages();
            return;
        }

        public void VoteStart (User u) {
            var p = Players.FirstOrDefault(x => x.Id == u.Id);
            p.VotedToStart = !p.VotedToStart;
            UpdateJoinMessages();
            if (Players.All(x => x.VotedToStart))
                StartGame();
            return;
        }

        private void UpdateJoinMessages (bool startinggame = false) {
            foreach (var p in Players) {
                string text = startinggame ? "Game started!" : "You have been added to a game.";
                if (Players.Count() >= MinPlayers || !startinggame)
                    text += p.VotedToStart ? "\nClick the Unvote button to remove your vote." : "\nClick the Start button to vote to start the game.";
                text += "\n\nPlayers:";
                text += Players.Aggregate("", (a, b) => a + "\n" + b.TelegramUser.FirstName + (b.VotedToStart ? " 👍" : ""));
                var buttons = new List<InlineKeyboardButton>();
                buttons.Add(new InlineKeyboardButton("Leave", $"{Id}|leave"));
                if (Players.Count() >= MinPlayers)
                    buttons.Add(new InlineKeyboardButton(p.VotedToStart ? "Unvote" : "Start", $"{Id}|start"));
                var menu = new InlineKeyboardMarkup(buttons.ToArray());
                if (p.JoinMsg == null)
                    p.JoinMsg = Bot.Send(text, p.Id, startinggame ? null : menu).Result;
                else
                    p.JoinMsg = Bot.Edit(text, p.JoinMsg, startinggame ? null : menu).Result;
            }
            return;
        }


        private void StartGame() {
            UpdateJoinMessages(true);
            Status = GameStatus.Running;


        }


        public void SendMessage(User u, string text) {
            foreach (var player in Players)
                if (player.Id != u.Id)
                    Bot.Send(u.FirstName + ":\n" + text, player.Id, parseMode: ParseMode.Default);
            return;
        }
    }
}

