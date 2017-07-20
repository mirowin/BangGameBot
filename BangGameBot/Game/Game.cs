using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace BangGameBot
{
    public partial class Game
    {
        public readonly int MinPlayers = 2;
        public readonly int MaxPlayers = 7;
        public readonly string SheriffIndicator = "S";
        public int Id { get; } = 0;
        public GameStatus Status = GameStatus.Joining;
        public List<Player> Players = new List<Player>();
        public List<Player> DeadPlayers = new List<Player>();
        private Dealer Dealer = new Dealer();
        private int Turn = -1;
        private static int NextId = 0;

        public Game (Message msg) {
            Id = NextId++; //TODO reuse ids
            
            AddPlayer(msg.From);
            return;
        }

        public void AddPlayer (User u) {
            Players.Add(new Player(u));
            if (Players.Count() == MaxPlayers)
                StartGame();
            else 
                UpdateJoinMessages();
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
            UpdateJoinMessages();
            return;
        }

        public void VoteStart (Player p) {
            p.VotedToStart = !p.VotedToStart;
            if (Players.All(x => x.VotedToStart))
                StartGame();
            else
                UpdateJoinMessages();
            return;
        }

        private void UpdateJoinMessages (bool startinggame = false) {
            foreach (var p in Players) {
                //"title"
                string text = startinggame ? "Game started!" : "You have been added to a game.";
                //help for the button
                if (Players.Count() >= MinPlayers && !startinggame)
                    text += p.VotedToStart ? "\nClick the Unvote button to remove your vote." : "\nClick the Start button to vote to start the game.";
                //playerlist
                text += "\n\n" + "Players:".ToBold();
                text += Players.Aggregate("", (a, b) => a + "\n" + b.TelegramUser.FirstName + (b.VotedToStart || startinggame ? " 👍" : ""));

                text += startinggame ? "\n\nShuffling the deck and assigning roles and characters..." : "";

                //menu
                var buttons = new List<InlineKeyboardButton>();
                buttons.Add(new InlineKeyboardButton("Leave", $"{Id}|leave"));
                if (Players.Count() >= MinPlayers)
                    buttons.Add(new InlineKeyboardButton(p.VotedToStart ? "Unvote" : "Start", $"{Id}|start"));
                var menu = new InlineKeyboardMarkup(buttons.ToArray());

                if (p.PlayerListMsg == null)
                    p.PlayerListMsg = Bot.Send(text, p.Id, startinggame ? null : menu).Result;
                else
                    p.PlayerListMsg = Bot.Edit(text, p.PlayerListMsg, startinggame ? null : menu).Result;
            }
            return;
        }
        
    }
}

