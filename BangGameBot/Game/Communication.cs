using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;

namespace BangGameBot
{
    public partial class Game
    {
        
        private static List<InlineKeyboardCallbackButton[]> MakeBoolMenu(string yes, string no)
        {
            return new List<InlineKeyboardCallbackButton[]>() {
                new []
                {
                    new InlineKeyboardCallbackButton(yes, $"game|bool|yes"),
                    new InlineKeyboardCallbackButton(no, $"game|bool|no")
                }
            };
        }

        private List<InlineKeyboardCallbackButton[]> MakeCardsInHandMenu(Player p, Situation s)
        {
            var rows = new List<InlineKeyboardCallbackButton[]>();
            foreach (var c in p.CardsInHand)
            {
                var err = (int)CanUseCard(p, c, s);
                var button = new InlineKeyboardCallbackButton(c.GetDescription(), err == 0 ? $"game|card|{c.Encode()}" : ("err" + err));
                rows.Add(p.HelpMode ? new[] { button, c.Name.ToHelpButton() } : button.ToSinglet());
            }
            return rows;
        }
        
        private void AddToHelp(Player p, List<Card> cards)
        {
            foreach (var c in cards.Select(x => x.Name))
                Tell("", p, c);
        }

        private void AddToHelp(List<Card> cards)
        {
            foreach (var c in cards)
                TellEveryone(cardused: c.Name);
        }
        
        private void Tell(string text, Player p, CardName cardused = CardName.None, Character character = Character.None, string textforothers = null)
        {
            if (p.HasLeftGame)
                return;
            if (!String.IsNullOrWhiteSpace(text))
                p.QueuedMsg.Text += "\n" + text;
            if (character != Character.None && !p.QueuedMsg.Characters.Contains(character))
                p.QueuedMsg.Characters.Add(character);
            if (cardused != CardName.None && !p.QueuedMsg.CardsUsed.Contains(cardused))
                p.QueuedMsg.CardsUsed.Add(cardused);
            if (textforothers != null)
                TellEveryone(textforothers, cardused, character, p.ToSinglet());
            return;
        }

        private void TellEveryone(string text = "", CardName cardused = CardName.None, Character character = Character.None, IEnumerable<Player> except = null)
        {
            var recipients = Watchers;
            if (except != null)
                recipients = recipients.Except(except);
            foreach (var p in recipients)
                Tell(text, p, cardused, character);
            return;
        }

        private void SendMessages(Player menurecipient = null, IEnumerable<InlineKeyboardCallbackButton[]> menu = null)
        {
            foreach (var p in Watchers)
                SendMessage(p, p.Id == (menurecipient?.Id ?? 0) ? menu.ToList() : null);
        }

        private void SendMessage(Player p, List<InlineKeyboardCallbackButton[]> menu = null)
        {
            if (p.HasLeftGame)
                return;
            var msg = p.QueuedMsg;
            if (String.IsNullOrWhiteSpace(msg.Text))
                return;
            if (p.HelpMode)
                msg.Text += Helpers.MakeHelpString(msg.CardsUsed, msg.Characters);
            if (menu == null)
                menu = new List<InlineKeyboardCallbackButton[]>();
            var lastrow = new List<InlineKeyboardCallbackButton>() { new InlineKeyboardCallbackButton("Players", $"game|players|new") };
            if (!p.IsDead)
                lastrow.Add(new InlineKeyboardCallbackButton("Your cards", $"game|mycards"));
            menu.AddRange(lastrow.ToArray().ToSinglet());
            if (p.CurrentMsg != null)
                Bot.EditMenu(null, p.CurrentMsg).Wait();
            if (menu == null)
                throw new Exception("MENU IS NULL!");

            p.CurrentMsg = Bot.Send(msg.Text, p.Id, menu.ToKeyboard()).Result;
            msg.Clear();
        }
        
        private void SendPlayerList()
        {
            foreach (var w in Watchers)
                SendPlayerList(w);
            return;
        }

        public void SendPlayerList(Player p, CallbackQuery q = null)
        {
            var text = "Players".ToBold() + ":\n";
            text += Users.Aggregate("", (s, pl) =>
                s +
                (p.IsDead ? "" : p.DistanceSeen(pl, AlivePlayers).ToEmoji()) +
                pl.Name + " - " + pl.Character.GetString<Character>() +
                (pl.Role == Role.Sheriff ? SheriffIndicator : "") +
                pl.LivesString() +
                (Players.Contains(pl) && Turn == Players.IndexOf(pl) ? "👈" : "") + "\n"
            );
            var menu = GetPlayerMenu(p);
            menu.Add(new[] { new InlineKeyboardCallbackButton("❌Delete this message", "delete") });
            if (q == null)
                Bot.Send(text, p.Id, menu.ToKeyboard()).Wait();
            else
                Bot.Edit(text, q.Message, menu.ToKeyboard()).Wait();
        }

        private List<InlineKeyboardCallbackButton[]> GetPlayerMenu(Player p)
        {
            var rows = new List<InlineKeyboardCallbackButton[]>();
            foreach (var pl in AlivePlayers)
            {
                var button = new InlineKeyboardCallbackButton(pl.Name, $"game|playerinfo|{pl.Id}");
                rows.Add(p.HelpMode ? new[] { button, pl.Character.ToHelpButton($"{pl.Character.GetString<Character>()}") } : button.ToSinglet());
            }
            return rows;
        }
        
        public void SendPlayerInfo(CallbackQuery q, Player choice, Player recipient)
        {
            var text = "";
            text = choice.Name.ToBold() + "\n\n" +
                "CHARACTER: " + choice.Character.GetString<Character>() + "\n" +
                "ROLE: " + (choice.Role == Role.Sheriff ? " Sheriff" : (choice.Id == recipient.Id ? choice.Role.GetString<Role>() : "Unknown")) + "\n" +
                "LIFE POINTS: " + choice.LivesString() + "\n\n" +
                "Cards in hand: " + (choice.Id == recipient.Id ?
                    ("\n" + string.Join(", ", recipient.CardsInHand.Select(x => x.GetDescription())) + "\n") :
                    (choice.CardsInHand.Count().ToString())
                ) + "\n" +
                "Cards on table:\n" + string.Join(", ", choice.CardsOnTable.Select(x => x.GetDescription()));
            if (recipient.HelpMode)
                text += Helpers.MakeHelpString((choice.Id == recipient.Id ? choice.Cards : choice.CardsOnTable).Select(x => x.Name).ToList(), choice.Character.ToSinglet().ToList());

            Bot.Edit(text, q.Message, new[] { new InlineKeyboardCallbackButton("🔙 All the players", $"game|players|edit"),  new InlineKeyboardCallbackButton("❌Delete this message", "delete") }.ToSinglet().ToKeyboard());
        }

        public void ShowMyCards(CallbackQuery q, Player p)
        {
            if (!p.IsDead)
                Bot.SendAlert(q, "Cards in hand:\n" + string.Join(", ", p.CardsInHand.Select(x => x.GetDescription())) + "\n\nCards on table:\n" + string.Join(", ", p.CardsOnTable.Select(x => x.GetDescription())));
            else
                Bot.SendAlert(q);
            return;
        }
        
        private void UpdateJoinMessages(bool startinggame = false, bool addingplayer = false)
        {
            foreach (var p in Users)
            {
                //"title"
                string text = startinggame ? "Game started!" : "You have been added to a game.";
                //help for the button
                if (Users.Count() >= MinPlayers && !startinggame)
                    text += p.VotedToStart ? "\nClick the Unvote button to remove your vote." : "\nClick the Start button to vote to start the game.";
                //playerlist
                text += "\n\n" + "Players:".ToBold();
                text += Users.Aggregate("", (a, b) => a + "\n" + b.TelegramUser.FirstName + (b.VotedToStart || startinggame ? " 👍" : ""));

                text += startinggame ? "\n\nShuffling the deck and assigning roles and characters..." : "";

                //menu
                var buttons = new List<InlineKeyboardCallbackButton> { new InlineKeyboardCallbackButton("Leave", $"game|leave") };
                if (Users.Count() >= MinPlayers)
                    buttons.Add(new InlineKeyboardCallbackButton(p.VotedToStart ? "Unvote" : "Start", $"game|start"));
                var menu = buttons.ToArray().ToSinglet().ToKeyboard();

                if (p.PlayerListMsg == null)
                    p.PlayerListMsg = Bot.Send(text, p.Id, startinggame ? null : menu).Result;
                else if (addingplayer)
                {
                    Bot.Delete(p.PlayerListMsg).Wait();
                    p.PlayerListMsg = Bot.Send(text, p.Id, startinggame ? null : menu).Result;
                }
                else
                    p.PlayerListMsg = Bot.Edit(text, p.PlayerListMsg, startinggame ? null : menu).Result;

                if (startinggame)
                    p.PlayerListMsg = null;
            }
            return;
        }
        
        private Choice WaitForChoice(Player p)
        {
            int maxseconds = int.MaxValue; //TODO
            p.Choice = null;
            var timer = 0;
            while (p.Choice == null && timer < maxseconds && !p.HasLeftGame)
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
                    p.Choice = new Choice(choice.GetCard(this));
                    break;
                case "playerinfo":
                    
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}