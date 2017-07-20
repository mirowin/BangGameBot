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

        private void SendPlayerList(Player p = null)
        {
            if (p == null)
            {
                Players.ForEach(pl => {
                    Bot.Delete(pl.PlayerListMsg);
                    SendPlayerList(pl);
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
            p.PlayerListMsg = Bot.Send(text, p.Id, menu).Result;
        }

        private InlineKeyboardMarkup GetPlayerMenu(Player p)
        {
            var result = new List<InlineKeyboardButton[]>();
            result.AddRange(Players.Select(x => new[] { new InlineKeyboardButton(x.Name, $"{Id}|playerinfo|{x.Id}") }));
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
            p.QueuedMsg = p.QueuedMsg.TrimEnd('\n') + textforp + "\n";
            foreach (var pl in Players.Where(x => x.Id != p.Id))
                pl.QueuedMsg = pl.QueuedMsg.TrimEnd('\n') + textforothers + "\n";
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
            {
                Bot.EditMenu(menu, p.TurnMsg);
                p.CurrentMenu = menu;
            }
            p.QueuedMsg = "\n";
            return;
        }

        public void SendPlayerInfo(CallbackQuery q, Player choice, Player recipient)
        {
            var text = "";
            text = choice.Name + "\n" +
                choice.Character.GetString<Character>() + (choice.Role == Role.Sheriff ? " - SHERIFF" : "") + "\n" +
                choice.LivesString() + "\n\n" +
                "Cards in hand: " + (choice.Id == recipient.Id ? 
                    recipient.CardsOnTable.Aggregate("\n", (s, c) => s + "- " + c.GetDescription() + "\n") :
                    choice.CardsInHand.Count().ToString()
                ) +
                "Cards on table: \n" + choice.CardsOnTable.Aggregate("", (s, c) => s + "- " + c.GetDescription() + "\n");
            Bot.SendAlert(q, text);
        }


        private Dictionary<Player,Choice> WaitForChoice(IEnumerable<Player> list, int maxseconds)
        {
#if DEBUG
            maxseconds = int.MaxValue;
#endif
            foreach (var p in list)
                p.Choice = null;
            var timer = 0;
            while (list.Any(p => p.Choice == null) && timer < maxseconds)
            {
                Task.Delay(1000).Wait();
                timer++;
            }
            return list.ToDictionary(x => x, y => y.Choice);
        }

        private Choice WaitForChoice(Player p, int maxseconds)
        {
            return WaitForChoice(p.ToSinglet(), maxseconds).First().Value;
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