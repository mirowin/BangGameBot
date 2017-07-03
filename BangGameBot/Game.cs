using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace BangGameBot
{
    public class Game
    {
        private int MinPlayers = 4;
        public int Id = 0;
        public GameStatus Status = GameStatus.Joining;
        public List<Player> Players = new List<Player>();

        public Game (Message msg) {
            do {
                var i = 1;
                if (Handler.Games.Any(x => x.Id == i))
                    i++;
                else
                    Id = i;
            } while (Id == 0);
            
            AddPlayer(msg.From);
        }

        public void AddPlayer (User u) {
            var p = new Player() { TelegramUser = u };
            Players.Add(p);
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
            if (Players.All(x => x.VotedToStart))
                StartGame();
            else
                UpdateJoinMessages();
            return;
        }

        private void UpdateJoinMessages () {
            foreach (var p in Players) {
                string text = "You have been added to a game.";
                if (Players.Count() >= MinPlayers)
                    text += p.VotedToStart ? "\nClick the Unvote button to remove your vote." : "\nClick the Start button to vote to start the game.";
                text += "\n\nPlayers:";
                text += Players.Aggregate("", (a, b) => a + "\n" + b.TelegramUser.FirstName + (b.VotedToStart ? " 👍" : ""));
                var buttons = new List<InlineKeyboardButton>();
                buttons.Add(new InlineKeyboardButton("Leave", $"{Id}|leave"));
                if (Players.Count() >= MinPlayers)
                    buttons.Add(new InlineKeyboardButton(p.VotedToStart ? "Unvote" : "Start", $"{Id}|start"));
                var menu = new InlineKeyboardMarkup(buttons.ToArray());
                if (p.JoinMsg == null)
                    p.JoinMsg = Bot.Send(text, p.Id, menu).Result;
                else
                    p.JoinMsg = Bot.Edit(text, p.JoinMsg, menu).Result;
            }
            return;
        }


        private void StartGame() {
            foreach (var p in Players)
                Bot.Send("Game started!", p.Id);
            Status = GameStatus.Running;
        }
    }
}

