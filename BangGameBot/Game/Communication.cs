﻿using System;
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
        
        private List<InlineKeyboardCallbackButton[]> MakeBoolMenu(string yes, string no)
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

        private List<InlineKeyboardCallbackButton[]> MakeMenuFromCards(IEnumerable<Card> list, Player recipient)
        {
            var rows = new List<InlineKeyboardCallbackButton[]>();
            foreach (var c in list)
            {
                var button = new InlineKeyboardCallbackButton(c.GetDescription(), $"game|card|{c.Encode()}");
                rows.Add(recipient.HelpMode ? new[] { button, c.Name.ToHelpButton() } : button.ToSinglet());
            }
            return rows;
        }

        public List<InlineKeyboardCallbackButton[]> AddYesButton(List<InlineKeyboardCallbackButton[]> buttons, string str)
        {
            buttons.Add(new[] { new InlineKeyboardCallbackButton(str, $"game|bool|yes") });
            return buttons;
        }

        public void AddToHelp(Player p, List<Card> cards)
        {
            foreach (var c in cards.Select(x => x.Name))
                Tell("", p, c);
        }

        public void AddToHelp(List<Card> cards)
        {
            foreach (var p in Players)
                AddToHelp(p, cards);
        }
        
        public void Tell(string text, Player p, CardName cardused = CardName.None, Character character = Character.None, string textforothers = null)
        {
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

        public void TellEveryone(string text, CardName cardused = CardName.None, Character character = Character.None, IEnumerable<Player> except = null)
        {
            var recipients = Players.Union(DeadPlayers);
            if (except != null)
                recipients = recipients.Except(except);
            foreach (var p in recipients)
                Tell(text, p, cardused, character);
            return;
        }

        public void SendMessage(Player p, List<InlineKeyboardCallbackButton[]> menu = null)
        {
            try
            {
                var msg = p.QueuedMsg;
                if (String.IsNullOrWhiteSpace(msg.Text))
                    return;
                if (p.HelpMode)
                    msg.Text += Helpers.MakeHelpString(msg.CardsUsed, msg.Characters);
                if (menu == null)
                    menu = new List<InlineKeyboardCallbackButton[]>();
                menu.Add(new[] {
                    new InlineKeyboardCallbackButton("Players", $"game|players|new"),
                    new InlineKeyboardCallbackButton("Your cards", $"game|mycards")
                });
                if (p.CurrentMsg != null)
                    Bot.EditMenu(null, p.CurrentMsg).Wait();
                if (menu == null)
                    throw new Exception("MENU IS NULL!");

                p.CurrentMsg = Bot.Send(msg.Text, p.Id, menu.ToKeyboard()).Result;
                msg.Clear();
            }
            catch (Exception e)
            {
                Program.LogError(e);
            }
        }

        public void SendPlayerList()
        {
            for (var i = 1; i < Players.Count() + 1; i++)
                SendPlayerList(Players[(Turn + i) % Players.Count()]);
            return;
        }

        public void SendPlayerList(Player p, CallbackQuery q = null)
        {
            try
            {
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
                menu.Add(new[] { new InlineKeyboardCallbackButton("❌Delete this message", "delete") });
                if (q == null)
                    Bot.Send(text, p.Id, menu.ToKeyboard()).Wait();
                else
                    Bot.Edit(text, q.Message, menu.ToKeyboard()).Wait();
            }
            catch (Exception e)
            {
                Program.LogError(e);
            }
        }

        private List<InlineKeyboardCallbackButton[]> GetPlayerMenu(Player p)
        {
            var rows = new List<InlineKeyboardCallbackButton[]>();
            foreach (var pl in Players)
            {
                var button = new InlineKeyboardCallbackButton(pl.Name, $"game|playerinfo|{pl.Id}");
                rows.Add(p.HelpMode ? new[] { button, pl.Character.ToHelpButton($"{pl.Character.GetString<Character>()}") } : button.ToSinglet());
            }
            return rows;
        }

        public void SendMessages(Player menurecipient = null, IEnumerable<InlineKeyboardCallbackButton[]> menu = null)
        {
            foreach (var p in Players.Union(DeadPlayers))
                SendMessage(p, p.Id == (menurecipient?.Id ?? 0) ? menu.ToList() : null);
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
            Bot.SendAlert(q, "Cards in hand:\n" + string.Join(", ", p.CardsInHand.Select(x => x.GetDescription())) + "\n\nCards on table:\n" + string.Join(", ", p.CardsOnTable.Select(x => x.GetDescription())));
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
                var buttons = new List<InlineKeyboardCallbackButton> { new InlineKeyboardCallbackButton("Leave", $"game|leave") };
                if (Players.Count() >= MinPlayers)
                    buttons.Add(new InlineKeyboardCallbackButton(p.VotedToStart ? "Unvote" : "Start", $"game|start"));
                var menu = new InlineKeyboardMarkup(buttons.ToArray());

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