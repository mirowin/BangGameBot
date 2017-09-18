using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineKeyboardButtons;

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
                var button = new InlineKeyboardCallbackButton(c.GetButtonText(), err == 0 ? $"game|card|{c.Encode()}" : ("err" + err));
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
            var recipients = new List<Player>(Watchers);
            if (except != null)
                recipients = recipients.Except(except).ToList();
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
            try
            {
                p.CurrentMsg = Bot.Send(msg.Text, p.Id, menu.ToKeyboard()).Result;
            }
            catch(AggregateException e)
            {
                if (e.InnerException.Message.Contains("blocked by the user"))
                    LeaveGame(p);
                else
                    throw;
            }
            msg.Clear();
        }
        
        private void SendPlayerList()
        {
            if (Status == GameStatus.Ending) return;
            foreach (var w in Watchers)
                SendPlayerList(w, null, false);
            return;
        }

        public void SendPlayerList(Player p, CallbackQuery q = null, bool expanded = true)
        {
            var text = "Players".ToBold() + ":\n";
            text += Users.Aggregate("", (s, pl) =>
                s +
                p.DistanceSeen(pl, AlivePlayers).ToEmoji() +
                pl.Name.ToBold() + " - " + pl.Character.GetString<Character>() +
                pl.LivesString() +
                (Turn == Users.IndexOf(pl) ? "👈" : "") + "\n"
            );
            List<InlineKeyboardCallbackButton[]> menu;
            if (expanded)
            {
                menu = GetPlayerMenu(p);
                menu.Add(new[] { new InlineKeyboardCallbackButton("📖Legend", "legend"), new InlineKeyboardCallbackButton("🗑Delete this message", "delete") });
            }
            else
            {
                menu = new List<InlineKeyboardCallbackButton[]>() {new[]
                {
                    new InlineKeyboardCallbackButton("More info", "game|players|expanded"),
                    new InlineKeyboardCallbackButton("🗑Delete this message", "delete")
                }};
            }
            try
            {
                if (q == null)
                    Bot.Send(text, p.Id, menu.ToKeyboard()).Wait();
                else
                    Bot.Edit(text, q.Message, menu.ToKeyboard()).Wait();
            }
            catch (AggregateException e)
            {
                if (!e.InnerExceptions.Any(x => x.Message.Contains("timed out")))
                    throw;
            }
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
                "ROLE: " + (choice.Role == Role.Sheriff ? " Sheriff" : (choice.Id == recipient.Id ? choice.Role.GetString<Role>() : "Unknown")) + "\n" +
                "CHARACTER: " + choice.Character.GetString<Character>() + "\n" +
                "LIFE POINTS: " + choice.LivesString() + "\n\n" +
                "Cards in hand: " + (choice.Id == recipient.Id ?
                    ("\n" + string.Join(", ", recipient.CardsInHand.Select(x => x.GetDescription())) + "\n") :
                    (choice.CardsInHand.Count().ToString())
                ) + "\n" +
                "Cards on table:\n" + string.Join(", ", choice.CardsOnTable.Select(x => x.GetDescription()));
            if (recipient.HelpMode)
                text += Helpers.MakeHelpString((choice.Id == recipient.Id ? choice.Cards : choice.CardsOnTable).Select(x => x.Name).ToList(), choice.Character.ToSinglet().ToList());

            Bot.Edit(text, q.Message, new[] { new InlineKeyboardCallbackButton("🔙 All the players", $"game|players|edit"),  new InlineKeyboardCallbackButton("🗑Delete this message", "delete") }.ToSinglet().ToKeyboard());
        }

        public void ShowMyCards(CallbackQuery q, Player p)
        {
            if (p.IsDead)
            {
                Bot.SendAlert(q);
                return;
            }

            var text = "ROLE: " + p.Role.GetString<Role>() +
                "\nCHARACTER: " + p.Character.GetString<Character>() +
                "\nLIFE POINTS: " + p.LivesString() + 
                "\n\nCards in hand:\n" + string.Join(", ", p.CardsInHand.Select(x => x.GetDescription())) + 
                "\n\nCards on table:\n" + string.Join(", ", p.CardsOnTable.Select(x => x.GetDescription()));
            
            Bot.SendAlert(q, text);
            return;
        }
        
        private void NotifyRoles()
        {
            foreach (var p in Users)
            {
                string msg;
                
                //role
                switch (p.Role)
                {
                    case Role.Sheriff:
                        msg = "You are the <b>Sheriff</b>! You get one more life point than your character has. You have to kill all the Outlaws and the Renegade!";
                        if (Users.Count() >= 7)
                            msg += " But be careful not to hit your two Deputies... they are your allies!";
                        else if (Users.Count() >= 5)
                            msg += " But be careful not to hit your Deputy... he is your ally!";
                        msg += "\nRemember you are the only role publicly known. All other roles are covered.";
                        break;
                    case Role.Outlaw:
                        msg = "You are an <b>Outlaw</b>! You have to kill the Sheriff before the others kill you!";
                        if (Users.Count() >= 6)
                            msg += " Note that there are other two Outlaws that share the same goal as you!";
                        else
                            msg += " Note that there is another Outlaw that shares the same goal as you!";
                        break;
                    case Role.Renegade:
                        msg = "You are the <b>Renegade</b>! You have to be the last one standing! Don't let the Outlaws kill the Sheriff and win!";
                        break;
                    case Role.DepSheriff:
                        msg = "You are a <b>Deputy Sheriff</b>! You have to protect the Sheriff and kill all the Outlaws and the Renegade!";
                        if (Users.Count() >= 7)
                            msg += " But be careful not to hit the other Deputy... he is your ally!";
                        break;
                    default:
                        throw new IndexOutOfRangeException("What role is that?");
                }
                if (p.Role != Role.Sheriff)
                    msg += "\nRemember that only the Sheriff's role is publicly known (he is the first one to play, and is always on top in the players list). All other roles are covered.";

                //speak about the character
                var chardesc = Helpers.Cards.FirstOrDefault(x => x.Name.ToLower() == p.Character.GetString<Character>().ToLower()).Description;
                CardName helpcard;
                switch(p.Character)
                {
                    case Character.PaulRegret:
                        helpcard = CardName.Mustang;
                        break;
                    case Character.Jourdounnais:
                        helpcard = CardName.Barrel;
                        break;
                    case Character.RoseDoolan:
                        helpcard = CardName.Scope;
                        break;
                    default:
                        helpcard = CardName.None;
                        break;
                }

                //tell everything (cards in hand too)
                Tell(msg + "\n\n"
                    + $"You have been given the character {p.Character.GetString<Character>()}.\n{chardesc}"
                    + $"\n\nYou drew {string.Join(", ", p.CardsInHand.Select(x => x.GetDescription()))}", p, helpcard);
                AddToHelp(p, p.CardsInHand);
            }
            SendMessages();
            return;
        }
        private void UpdateJoinMessages(bool startinggame = false, bool addingplayer = false)
        {
            //these are the same for all players.
            var starttext = startinggame ? "Game started!" : "You have been added to a game.";
            var playerlist = "Players:".ToBold() + Users.Aggregate("", (a, b) => a + "\n" + b.Name + (b.VotedToStart || startinggame ? " 👍" : ""));
            if (startinggame)
                playerlist += "\n\nShuffling the deck and assigning roles and characters...";

            foreach (var p in Users) //now create the message for each player
            {
                var text = starttext;
                if (Users.Count() >= GameSettings.MinPlayers && !startinggame)
                    text += p.VotedToStart ? "\nClick the Unvote button to remove your vote." : "\nClick the Start button to vote to start the game.";
                text += "\n\n" + playerlist;

                //menu
                List<InlineKeyboardCallbackButton> buttons = null;
                if (!startinggame)
                {
                    buttons = new List<InlineKeyboardCallbackButton>() { new InlineKeyboardCallbackButton("Leave", $"game|leave") };
                    if (Users.Count() >= GameSettings.MinPlayers)
                        buttons.Add(new InlineKeyboardCallbackButton(p.VotedToStart ? "Unvote" : "Start", $"game|start"));
                }
                var menu = startinggame ? null : buttons.ToArray().ToSinglet().ToKeyboard();


                if (p.PlayerListMsg == null)
                    p.PlayerListMsg = Bot.Send(text, p.Id, menu).Result;
                else if (addingplayer)
                {
                    Bot.Delete(p.PlayerListMsg).Wait();
                    p.PlayerListMsg = Bot.Send(text, p.Id, menu).Result;
                }
                else if (!text.IsHTMLEqualTo(p.PlayerListMsg.Text))
                    p.PlayerListMsg = Bot.Edit(text, p.PlayerListMsg, menu).Result;

                if (startinggame)
                    p.PlayerListMsg = null;

            }
            return;
        }
        
        private Choice WaitForChoice(Player p, int maxseconds)
        {
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
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}