using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        private List<InlineKeyboardButton[]> MakeMenuFromCards(List<Card> list)
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
            //TODO improve UI, add menu (to delete at end of turn!)
            if (p == null)
            {
                Players.ForEach(pl => SendPlayerList(pl));
                return;
            }
            var text = "Players".ToBold() + ":\n";
            text += Players.Aggregate("", (s, pl) =>
                s +
            (p != pl ? p.DistanceSeen(pl, Players).ToEmoji() : "") +
            pl.Name + " - " + pl.Character.GetString<Character>() +
            (pl.Role == Role.Sheriff ? SheriffIndicator : "") +
            pl.LivesString() +
            (Turn == Players.IndexOf(pl) ? "👈" : "") + "\n"
            );
            p.PlayerListMsg = Bot.Send(text, p.Id).Result;
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
            p.QueuedMsg += textforp + "\n";
            foreach (var pl in Players.Where(x => x.Id != p.Id))
                pl.QueuedMsg += textforothers + "\n";
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
            foreach (var p in Players)
            {
                if (p.TurnMsg == null || (p.TurnMsg.Text + p.QueuedMsg).Length > 4000)
                    p.TurnMsg = Bot.Send(p.QueuedMsg, p.Id, (menurecipients.Contains(p) ? menu : null)).Result;
                else
                    p.TurnMsg = Bot.Edit(p.TurnMsg.Text + p.QueuedMsg, p.TurnMsg, (menurecipients.Contains(p) ? menu : null)).Result;
                p.QueuedMsg = "";
            }
            return;
        }

        private void SendMessages(Player menurecipient, IReplyMarkup menu = null)
        {
            SendMessages(menurecipient.ToSinglet(), menu);
            return;
        }

        
        
        private Choice WaitForChoice(Player p, int maxseconds)
        {
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
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}