using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace BangGameBot
{
    public class Game
    {
        public readonly int MinPlayers = 2;
        public readonly int MaxPlayers = 7;
        public readonly string SheriffIndicator = "S";
        public int Id = 0;
        public GameStatus Status = GameStatus.Joining;
        public List<Player> Players = new List<Player>();
        private Dealer Dealer = new Dealer();
        private int Turn = 0;

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


        private void StartGame() {
            UpdateJoinMessages(true);
            Status = GameStatus.Running;

            AssignRoles();
            AssignCharacters();
            DealCards();

            while (true) {
                SendPlayerList();
                if (Turn == Players.Count())
                    Turn = 0;
                var currentplayer = Players[Turn];
                //TODO Phase Zero: check for Dynamite and Jail
                PhaseOne(currentplayer);
                //TODO PhaseTwo(currentplayer);
                PhaseThree(currentplayer);

                Send("", currentplayer, null); //disable menu
                foreach (var p in Players) {
                    p.PlayerListMsg = null;
                    p.TurnMsg = null;
                    p.Choice = null; //just to be sure
                }
                Turn++;
            }
        }

        private void AssignRoles() {
            var rolesToAssign = new List<Role>();
            var count = Players.Count();
            rolesToAssign.Add(Role.Sheriff);
            rolesToAssign.Add(Role.Renegade);
            if (count >= 3) {
                rolesToAssign.Add(Role.Outlaw);
                rolesToAssign.Add(Role.Outlaw);
            }
            if (count >= 5)
                rolesToAssign.Add(Role.DepSheriff);
            if (count >= 6)
                rolesToAssign.Add(Role.Outlaw);
            if (count == 7)
                rolesToAssign.Add(Role.DepSheriff);

            Players.Shuffle();
            Players.Shuffle();
            rolesToAssign.Shuffle();
            rolesToAssign.Shuffle();

            if (Players.Count() != rolesToAssign.Count())
                throw new Exception("Players count != roles to assign");

            for (var i = 0; i < count; i++) {
                Players[i].Role = rolesToAssign[i];
            }

            //move sheriff to first place
            var sheriffindex = Players.IndexOf(Players.First((x => x.Role == Role.Sheriff)));
            Player temp = Players[0];
            Players[0] = Players[sheriffindex];
            Players[sheriffindex] = temp;

            return;
        }

        private void AssignCharacters() {
            var charsToAssign = new List<Character>();
            //charsToAssign.AddRange(Enum.GetValues(typeof(Character)).Cast<Character>().ToList());
            charsToAssign.AddRange(new []{Character.JesseJones, Character.BartCassidy});

            foreach (var p in Players) {
                //assign characters
                p.Character = charsToAssign[Program.R.Next(charsToAssign.Count())];
                charsToAssign.Remove(p.Character);
                //assign lives
                p.Lives = new[] { Character.PaulRegret, Character.ElGringo }.Contains(p.Character) ? 3 : 4;
                if (p.Role == Role.Sheriff)
                    p.Lives++;
            }
            return;
        }

        private void DealCards() {
            foreach (var p in Players) {
                Dealer.DrawCards(p.Lives, p);
            }
            return;
        }


        private void PhaseOne(Player curplayer) {
            //TODO Kit carlson

            if ((curplayer.Character == Character.JesseJones || curplayer.Character == Character.PedroRamirez) && CanUseAbility(curplayer)) {
                //ask them if they want to use the ability
                var str = curplayer.Character == Character.JesseJones ? 
                    "You are Jesse Jones: you can draw your first card from the hand of a player." :
                    $"You are Pedro Ramirez: you can draw your first card from the top of the graveyard. ({Dealer.Graveyard.Last().GetDescription()})";
                str += "\nDo you want to use your ability or do you want to draw from the deck?";
                Send(str, curplayer, MakeBoolMenu("Use ability", "Draw from deck"));

                //now let's see what they chose
                if (WaitForChoice(curplayer, 30)?.ChoseYes ?? DefaultChoice.UseAbilityPhaseOne) {
                    switch (curplayer.Character) {
                        case Character.JesseJones:
                            //steal from a player
                            UsePanic(curplayer, true);
                            break;
                        case Character.PedroRamirez:
                            var card = Dealer.DrawFromGraveyard(curplayer).GetDescription();
                            Send($"You drew {card} from the graveyard", curplayer);
                            SendToEveryone($"{curplayer.Name} drew {card} from the graveyard", curplayer);
                            break;
                    }
                    DrawCards(curplayer, 1);
                    return;
                }
                //if they chose no, it's exactly as another character.
            }

            DrawCards(curplayer, 2);
        }


        private void PhaseThree(Player curplayer) {
            bool firsttime = true;
            while (true) {
                var msg = "";
                var discard = curplayer.CardsInHand.Count() > curplayer.Lives;
                if (firsttime) {
                    if (discard)
                        msg = "You need to discard at least " + (curplayer.CardsInHand.Count() - curplayer.Lives).ToString() + " cards.\n";
                    Send(msg + "Select the cards you want to discard.", curplayer, MakeCardsInHandMenu(curplayer, true));
                }
                var choice = WaitForChoice(curplayer, 30);
                if (choice?.ChoseYes ?? false)
                    break;
                var cardchosen = choice?.CardChosen ?? DefaultChoice.ChooseCard;
                if (cardchosen != null || discard) {
                    var card = Dealer.Discard(curplayer, cardchosen);
                    Send("You discarded " + card.GetDescription(), curplayer, MakeCardsInHandMenu(curplayer, true));
                    SendToEveryone(curplayer.Name + " discarded " + card.GetDescription(), curplayer);
                }
                else
                    break;
                firsttime = false;
            }
        }

        private bool CanUseAbility(Player player) {
            switch (player.Character) {
                case Character.JesseJones:
                    return Players.Where(x => x.Lives > 0 && x.CardsInHand.Count() > 0).Any();
                case Character.PedroRamirez:
                    return Dealer.Graveyard.Any();
                default:
                    throw new NotImplementedException();
            }
        }
            
        private ErrorMessage CanUseCard(Player player, Card card) {
            switch (card.Name) {
                case CardName.Panic:
                    return Players.Where(x => x.Lives > 0 && x.Cards.Count() > 0 && player.DistanceSeen(x, Players) == 1).Any() ? ErrorMessage.NoError : ErrorMessage.NoPlayersToStealFrom;
                default:
                    throw new NotImplementedException();
            }
        }

        private void DrawCards(Player p, int n) {
            var result = Dealer.DrawCards(n, p);
            var listofcards = result.Item1;
            var reshuffled = result.Item2;
            if (reshuffled == -1) {
                Send("You drew " + listofcards.Aggregate("", (s, c) => s + c.GetDescription() + ", ").TrimEnd(' ', ',') + " from the deck.", p);
                SendToEveryone(p.Name + " drew " + listofcards.Count() + " cards from the deck.", p);
            } else {
                var msgforp = "You drew ";
                for (var i = 0; i < reshuffled; i++) {
                    msgforp += listofcards[i].GetDescription() + ", ";
                }
                msgforp = msgforp.TrimEnd(' ',',') + "from the deck.\nThe deck was reshuffled.\nYou drew ";
                for (var i = reshuffled; i < listofcards.Count(); i++) {
                    msgforp += listofcards[i].GetDescription() + ", ";
                }
                msgforp = msgforp.TrimEnd(' ', ',') + "from the deck.";
                Send(msgforp, p);
                SendToEveryone(p.Name + "drew " + (reshuffled + 1).ToString() + " cards from the deck.\nThe deck was reshuffled.\n" + p.Name + "drew " + (listofcards.Count()-reshuffled).ToString() + " cards from the deck.", p);
            }
        }

        private void UsePanic(Player curplayer, bool jessejonesability = false) {
            var possiblechoices = jessejonesability ? Players.Where(x => x != curplayer && x.Lives > 0 && x.CardsInHand.Count() > 0) : Players.Where(x => x.Lives > 0 && x.Cards.Count() > 0 && curplayer.DistanceSeen(x, Players) == 1);
            Player playerchosen;
            bool automatic = false;
            if (possiblechoices.Count() == 1) {
                playerchosen = possiblechoices.First();
                automatic = true;
            } else {
                var str = "Choose the player to steal from.\nThe number in parenthesis is the number of cards they have in their hand.";
                var buttonslist = new List<InlineKeyboardButton[]>();
                foreach (var p in possiblechoices)
                    buttonslist.Add(new[] { new InlineKeyboardButton(p.Name +$"({p.CardsInHand.Count()})",$"{Id}|player|{p.Id}")});
                Send(str, curplayer, new InlineKeyboardMarkup(buttonslist.ToArray()));
                SendToEveryone($"{curplayer.Name} has decided to steal their first card from a player's hand.", curplayer);
                playerchosen = WaitForChoice(curplayer, 30)?.PlayerChosen ?? DefaultChoice.ChoosePlayer(Players);
            }
            var msg = automatic ? $"The only player you can steal from is {playerchosen.Name}." : $"You chose to steal from {playerchosen.Name}.";
            if (jessejonesability || playerchosen.CardsOnTable.Count() == 0) {
                var card = curplayer.StealFrom(playerchosen).GetDescription();
                msg += $"\nYou stole {card} from {playerchosen.Name}'s hand.";
                Send(msg, curplayer);
                SendToEveryone($"{curplayer.Name} stole a card from {playerchosen.Name}'s hand.", curplayer);
            } else {
                msg += "\nChoose which card to steal.";
                var buttonslist = new List<InlineKeyboardButton[]>();
                foreach (var c in playerchosen.CardsOnTable)
                    buttonslist.Add(new[] { new InlineKeyboardButton(c.Name.GetString<CardName>(),$"{Id}|card|{c.Encode()}")});
                buttonslist.Add(new[] { new InlineKeyboardButton("Steal from hand", $"{Id}|bool|yes")});
                Send(msg, curplayer, new InlineKeyboardMarkup(buttonslist.ToArray()));
                SendToEveryone($"{curplayer.Name} chose to steal a card from {playerchosen.Name}.", curplayer);
                var choice = WaitForChoice(curplayer, 30);
                Card chosencard = null;
                if (!(choice?.ChoseYes ?? false))
                    chosencard = choice.CardChosen ?? DefaultChoice.ChooseCard;
                var card = curplayer.StealFrom(playerchosen, chosencard).GetDescription();
                Send("You stole {card} from {playerchosen.Name}.", curplayer);
                SendToEveryone($"{curplayer.Name} stole " + (chosencard == null ? $"a card from {playerchosen.Name}'s hand." : $"{card} from {playerchosen.Name}."), curplayer);
            }
            return;
        }

        private InlineKeyboardMarkup MakeBoolMenu (string yes, string no) {
            var buttons = new List<InlineKeyboardButton>();
            buttons.Add(new InlineKeyboardButton(yes, $"{Id}|bool|yes"));
            buttons.Add(new InlineKeyboardButton(no, $"{Id}|bool|no"));
            return new InlineKeyboardMarkup(buttons.ToArray());
        }

        private InlineKeyboardMarkup MakeCardsInHandMenu (Player p, bool phasethree = false) {
            var buttons = new List<InlineKeyboardButton[]>();
            foreach (var c in p.CardsInHand) {
                var err = phasethree ? ErrorMessage.NoError : CanUseCard(p, c);
                buttons.Add(new InlineKeyboardButton(c.GetDescription(), err == ErrorMessage.NoError ? $"{Id}|card|{c.Encode()}" : ("err" + (int)err)).ToSinglet());
            }
            if (phasethree && p.CardsInHand.Count() <= p.Lives) {
                buttons.Add(new InlineKeyboardButton("End of turn", $"{Id}|bool|yes").ToSinglet());
            }
            return new InlineKeyboardMarkup(buttons.ToArray());
        }

        private void SendPlayerList(Player p = null) {
            //TODO improve UI, add menu (to delete at end of turn!)
            if (p == null) {
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


        public void SendMessage(User u, string text) {
            foreach (var player in Players)
                if (player.Id != u.Id)
                    Bot.Send(u.FirstName.ToBold() + ":\n" + text.FormatHTML(), player.Id);
            return;
        }

        /// <summary>
        /// Send the specified text and menu to Player p. To be used only during Turn!
        /// </summary>
        private void Send(string text, Player p, IReplyMarkup menu = null) {
            if (p.TurnMsg == null)
                p.TurnMsg = Bot.Send(text, p.Id, menu).Result;
            else
                //TODO remove one of the \n's and choose each time if there should be some more space.
                p.TurnMsg = Bot.Edit(p.TurnMsg.Text + "\n\n" + text, p.TurnMsg, menu).Result;
        }


        /// <summary>
        /// Send the turn message to all the players except the ones in the list
        /// </summary>
        private void SendToEveryone (string text, List<Player> except, IReplyMarkup menu = null) {
            foreach (var p in Players)
                if (!except.Contains(p))
                    Send(text, p, menu);
        }

        /// <summary>
        /// Send the turn message to all the players except the specified
        /// </summary>
        private void SendToEveryone (string text, Player except = null, IReplyMarkup menu = null) {
            foreach (var p in Players)
                if (p != except)
                    Send(text, p, menu);
        }

        private Choice WaitForChoice(Player p, int maxseconds) {
            p.Choice = null;
            var timer = 0;
            while (p.Choice == null && timer < maxseconds) {
                Task.Delay(1000).Wait();
                timer++;
            }
            return p.Choice;
        }

        public void HandleChoice(Player p, string[] args, CallbackQuery q) {
            var type = args[0];
            var choice = args[1];
            switch (type) {
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

