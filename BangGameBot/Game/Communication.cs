using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace BangGameBot
{
    public partial class Game
    {
        private InlineKeyboardMarkup MakeBoolMenu(string yes, string no)
        {
            var buttons = new List<InlineKeyboardButton>();
            buttons.Add(new InlineKeyboardButton(yes, $"{Id}|bool|yes"));
            buttons.Add(new InlineKeyboardButton(no, $"{Id}|bool|no"));
            return new InlineKeyboardMarkup(buttons.ToArray());
        }

        private List<InlineKeyboardButton[]> MakeCardsInHandMenu(Player p, Situation s)
        {
            var buttons = new List<InlineKeyboardButton[]>();
            foreach (var c in p.CardsInHand)
            {
                var err = (int)CanUseCard(p, c, s);
                buttons.Add(new InlineKeyboardButton(c.GetDescription(), err == 0 ? $"{Id}|card|{c.Encode()}" : ("err" + err)).ToSinglet());
            }
            return buttons;
        }

        private List<InlineKeyboardButton[]> MakeMenuFromCards(IEnumerable<Card> list)
        {
            var buttons = new List<InlineKeyboardButton[]>();
            foreach (var c in list)
                buttons.Add(new InlineKeyboardButton(c.GetDescription(), $"{Id}|card|{c.Encode()}").ToSinglet());
            return buttons;
        }

        public List<InlineKeyboardButton[]> AddYesButton(List<InlineKeyboardButton[]> buttons, string str)
        {
            buttons.Add(new[] { new InlineKeyboardButton(str, $"{Id}|bool|yes") });
            return buttons;
        }

        private void SendPlayerList(bool newturn, Player p = null)
        {
            if (p == null)
            {
                Players.ForEach(pl => {
                    SendPlayerList(newturn, pl);
                });
                return;
            }
            var text = "Players".ToBold() + ":\n";
            text += Players.Aggregate("", (s, pl) =>
                s +
                p.DistanceSeen(pl, Players).ToEmoji() +
                pl.Name + " - " + pl.Character.GetString<Character>() +
                (pl.Role == Role.Sheriff ? SheriffIndicator : "") +
                pl.LivesString() +
                (Turn == Players.IndexOf(pl) ? "👈" : "") + "\n"
            );
            var menu = GetPlayerMenu(p);
            if (p.PlayerListMsg != null && newturn)
            {
                Bot.Delete(p.PlayerListMsg);
                p.PlayerListMsg = Bot.Send(text, p.Id, menu).Result;
            }
            else if (p.PlayerListMsg == null)
                p.PlayerListMsg = Bot.Send(text, p.Id, menu).Result;
            else if (p.PlayerListMsg != null && !newturn)
                p.PlayerListMsg = Bot.Edit(text, p.PlayerListMsg, menu).Result;
        }

        private InlineKeyboardMarkup GetPlayerMenu(Player p)
        {
            var result = new List<InlineKeyboardButton[]>();
            result.AddRange(Players.Where(x => x.Id != p.Id).Select(x => new[] { new InlineKeyboardButton(x.Name, $"{Id}|playerinfo|{x.Id}") }));
            result.Add(new[] {
                new InlineKeyboardButton("Your cards", $"{Id}|playerinfo|{p.Id}"),
                InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Help")
            });
            return result.ToKeyboard();
        }

        public void SendMessage(User u, string text)
        {
            foreach (var player in Players)
                if (player.Id != u.Id)
                    Bot.Send(u.FirstName.ToBold() + ":\n" + text.FormatHTML(), player.Id);
            return;
        }

        private void Tell(string textforp, Player p, bool addextraspace, string textforothers = null)
        {
            if (addextraspace)
            {
                textforp = "\n" + textforp;
                textforothers = "\n" + textforothers;
            }
            if (!String.IsNullOrWhiteSpace(textforp))
                p.QueuedMsg = p.QueuedMsg + textforp + "\n";
            if (!String.IsNullOrWhiteSpace(textforothers))
                foreach (var pl in Players.Where(x => x.Id != p.Id))
                    pl.QueuedMsg = pl.QueuedMsg + textforothers + "\n";
            return;
        }

        private void TellEveryone(string text, bool addextraspace = true, Player[] except = null)
        {
            foreach (var p in Players.Where(x => !except?.Contains(x) ?? true))
                Tell(text, p, addextraspace, null);
            return;
        }

        private void SendMessages (Player[] menurecipients = null, IReplyMarkup menu = null)
        {
            menurecipients = menurecipients ?? Players.ToArray();
            foreach (var p in menurecipients)
                SendMessagesToSingle(p, menu);
            foreach (var p in Players.Where(x => !menurecipients.Contains(x)))
                SendMessagesToSingle(p, null);
            return;
        }
        
        private void SendMessages(Player menurecipient, IReplyMarkup menu = null)
        {
            SendMessages(menurecipient.ToSinglet(), menu);
            return;
        }

        private void SendMessagesToSingle(Player p, IReplyMarkup menu = null)
        {
            if (!String.IsNullOrWhiteSpace(p.QueuedMsg) && (p.TurnMsg == null || (p.TurnMsg.Text + p.QueuedMsg).Length > 4000))
                p.TurnMsg = Bot.Send(p.QueuedMsg, p.Id, menu).Result;
            else if (p.TurnMsg != null && !String.IsNullOrWhiteSpace(p.QueuedMsg))
                p.TurnMsg = Bot.Edit(p.TurnMsg.Text + p.QueuedMsg, p.TurnMsg, menu).Result;
            else if (p.TurnMsg != null && menu != p.CurrentMenu)
                Bot.EditMenu(menu, p.TurnMsg);
            p.QueuedMsg = "\n";
            p.CurrentMenu = menu;
            return;
        }

        public void SendPlayerInfo(CallbackQuery q, Player choice, Player recipient)
        {
            var text = "";
            text = choice.Name + "\n" +
                "CHARACTER: " + choice.Character.GetString<Character>() + "\n" +
                "ROLE: " + (choice.Role == Role.Sheriff ? " Sheriff" : (choice.Id == recipient.Id ? choice.Role.GetString<Role>() : "Unknown")) + "\n" +
                "LIFE POINTS: " + choice.LivesString() + "\n\n" +
                "Cards in hand: " + (choice.Id == recipient.Id ? 
                    ("\n" + string.Join(", ", recipient.CardsInHand.Select(x => x.GetDescription())) + "\n") :
                    (choice.CardsInHand.Count().ToString())
                ) + "\n" +
                "Cards on table:\n" + string.Join(", ", choice.CardsOnTable.Select(x => x.GetDescription()));
            Bot.SendAlert(q, text);
        }

        private void UpdateJoinMessages(bool startinggame = false, bool addingplayer = false)
        {
            foreach (var p in Players)
            {
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
                else if (addingplayer)
                {
                    Bot.Delete(p.PlayerListMsg);
                    p.PlayerListMsg = Bot.Send(text, p.Id, startinggame ? null : menu).Result;
                }
                else
                    p.PlayerListMsg = Bot.Edit(text, p.PlayerListMsg, startinggame ? null : menu).Result;

                if (startinggame)
                    p.PlayerListMsg = null;
            }
            return;
        }

        
        private Choice WaitForChoice(Player p, int maxseconds)
        {
#if DEBUG
            maxseconds = int.MaxValue;
#endif
            p.Choice = null;
            var timer = 0;
            while (p.Choice == null && timer < maxseconds)
            {
                Task.Delay(1000).Wait();
                timer++;
            }
            return p.Choice;
        }

        public void HandleChoice(Player p, string[] args, CallbackQuery q)
        {
            var type = args[0];
            var choice = args[1];
            switch (type)
            {
                case "bool":
                    p.Choice = new Choice(choice == "yes");
                    break;
                case "player":
                    p.Choice = new Choice(Players.First(x => x.Id == long.Parse(choice)));
                    break;
                case "card":
                    p.Choice = new Choice(choice.GetCard(Dealer, Players));
                    break;
                case "playerinfo":
                    
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}