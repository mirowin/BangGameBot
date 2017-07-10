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
        public List<Player> DeadPlayers = new List<Player>();
        private Dealer Dealer = new Dealer();
        private int Turn = -1;

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
                Turn++;
                SendPlayerList();
                if (Turn == Players.Count())
                    Turn = 0;
                var currentplayer = Players[Turn];
                CheckDynamiteAndJail(currentplayer);
                if (currentplayer.CardsOnTable.All(x => x.Name != CardName.Jail)) {
                    PhaseOne(currentplayer);
                    //TODO PhaseTwo(currentplayer);
                    PhaseThree(currentplayer);
                    Send("", currentplayer, null); //disable menu
                } else {
                    Discard(currentplayer, currentplayer.CardsOnTable.First(x => x.Name == CardName.Jail));
                }
                foreach (var p in Players) {
                    p.PlayerListMsg = null;
                    p.TurnMsg = null;
                    p.Choice = null; //just to be sure
                }
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
                p.SetLives();
            }
            return;
        }

        private void DealCards() {
            foreach (var p in Players) {
                Dealer.DrawCards(p.Lives, p);
            }
            return;
        }

        private void CheckDynamiteAndJail(Player curplayer) {
            if (curplayer.CardsOnTable.Any(x => x.Name == CardName.Dynamite)) {
                var dynamite = curplayer.CardsOnTable.First(x => x.Name == CardName.Dynamite);
                var card = Draw(curplayer);
                if (card.Number < 10 && card.Suit == CardSuit.Spades) {
                    SendToEveryone("The dynamite explodes!");
                    HitPlayer(curplayer, 3);
                    Discard(curplayer, dynamite);
                } else {
                    Player nextplayer = Players[(Turn+1) % Players.Count()];
                    SendToEveryone("The dynamite passes to " + nextplayer.Name);
                    nextplayer.StealFrom(curplayer, dynamite);
                    Dealer.PutPermCardOnTable(nextplayer, dynamite);
                }
            }
            if (curplayer.CardsOnTable.Any(x => x.Name == CardName.Jail)) {
                var jail = curplayer.CardsOnTable.First(x => x.Name == CardName.Jail);
                var card = Draw(curplayer);
                if (card.Suit == CardSuit.Hearts) {
                    SendToEveryone($"The Jail is discarded and {curplayer.Name} play their turn.");
                    Discard(curplayer, jail);
                } else {
                    SendToEveryone("{curplayer.Name} skips this turn. The Jail is discarded.");
                    //StartGame() will discard jail
                }
                return;
            }
        }

        private void PhaseOne(Player curplayer) {
            Card card;
            List<Card> cardsdrawn;
            switch (curplayer.Character) {
                case Character.KitCarlson:
                    Send("You are Kit Carlson. You draw 3 cards from the deck, then choose one to discard.", curplayer);
                    cardsdrawn = DrawCards(curplayer, 3);
                    Send("Choose the card to discard", curplayer, MakeMenuFromCards(cardsdrawn));
                    var cardchosen = WaitForChoice(curplayer, 30)?.CardChosen ?? DefaultChoice.ChooseCard;
                    card = Discard(curplayer, cardchosen);
                    Send("You discarded " + card.GetDescription(), curplayer);
                    SendToEveryone(curplayer.Name + " discarded " + card.GetDescription(), curplayer);
                    return;
                case Character.BlackJack:
                    Send("You are Black Jack. You show the second card you draw; on Hearts or Diamonds, you draw one more card.", curplayer);
                    var secondcard = DrawCards(curplayer, 2)[1];
                    switch (secondcard.Suit) {
                        case CardSuit.Hearts:
                        case CardSuit.Diamonds:
                            Send("The second card was " + secondcard.Suit.ToEmoji() + ", so you draw another card.", curplayer);
                            SendToEveryone(curplayer.Name + "drew " + secondcard.GetDescription() + ", so they draw another card.", curplayer);
                            DrawCards(curplayer, 1);
                            return;
                        case CardSuit.Clubs:
                        case CardSuit.Spades:
                            SendToEveryone(curplayer.Name + "drew " + secondcard.GetDescription() + ", so they can't draw another card.", curplayer);
                            return;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                default:
                    //Jesse Jones & Pedro Ramirez can choose.
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
                                    var carddesc = Dealer.DrawFromGraveyard(curplayer).GetDescription();
                                    Send($"You drew {carddesc} from the graveyard", curplayer);
                                    SendToEveryone($"{curplayer.Name} drew {carddesc} from the graveyard", curplayer);
                                    break;
                            }
                            DrawCards(curplayer, 1);
                            return;
                        }
                        //if they chose no, it's exactly other characters.
                    }

                    DrawCards(curplayer, 2);
                    return;
            }
        }


        private void PhaseThree(Player curplayer) {
            bool firsttime = true;
            var discarded = 0;
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
                    var card = Discard(curplayer, cardchosen);
                    Send("You discarded " + card.GetDescription(), curplayer, MakeCardsInHandMenu(curplayer, true));
                    SendToEveryone(curplayer.Name + " discarded " + card.GetDescription(), curplayer);
                }
                else
                    break;
                firsttime = false;
                discarded++;
                if (curplayer.Character == Character.SidKetchum && discarded == 2 && curplayer.Lives < curplayer.MaxLives) {
                    Send("You discarded two cards. Do you want to use your ability and regain one life point?", curplayer, MakeBoolMenu("Yes", "No"));
                    if (WaitForChoice(curplayer, 30)?.ChoseYes ?? DefaultChoice.UseAblityPhaseThree) {
                        curplayer.AddLives(1);
                    }
                }
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

        private List<Card> DrawCards(Player p, int n) {
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
                msgforp = msgforp.TrimEnd(' ', ',') + "from the deck.\nThe deck was reshuffled.";
                if (reshuffled < listofcards.Count()) {
                    msgforp += "\nYou drew ";
                    for (var i = reshuffled; i < listofcards.Count(); i++) {
                        msgforp += listofcards[i].GetDescription() + ", ";
                    }
                    msgforp = msgforp.TrimEnd(' ', ',') + "from the deck.";
                    Send(msgforp, p);
                    SendToEveryone(p.Name + "drew " + (reshuffled + 1).ToString() + " cards from the deck.\nThe deck was reshuffled."
                        + (
                            listofcards.Count() - reshuffled > 0 ?
                            ("\n" + p.Name + "drew " + (listofcards.Count() - reshuffled).ToString() + " cards from the deck.") : 
                            ""
                        ), p);
                }
            }
            return listofcards;
        }

        private Card Draw(Player player) {
            if (player.Character == Character.LuckyDuke) {
                var msg = "You are Lucky Duke. You draw two cards, then choose one.\n";
                var result = Dealer.DrawCards(2, player);
                var part2 = " drew " + result.Item1[0].GetDescription() + " and " + result.Item1[1].GetDescription() + (result.Item2 > -1 ? ", and reshuffled the deck." : ".");
                Send(msg + "You" + part2 + " Choose a card.", player.Name + part2, player, MakeMenuFromCards(result.Item1));
                var cardchosen = WaitForChoice(player, 30).CardChosen ?? DefaultChoice.ChooseCardFrom(result.Item1);
                Send("You chose " + cardchosen.GetDescription(), player.Name + " chose " + cardchosen.GetDescription(), player);
                Discard(player, result.Item1.First(x => x != cardchosen));
                Discard(player, cardchosen);
                return cardchosen;
            } else {
                var result = Dealer.DrawToGraveyard();
                var card = result.Item1;
                var reshuffled = result.Item2;
                Send("You drew " + card.GetDescription() + (reshuffled ? ", then reshuffled the deck." : ""), player.Name + " drew " + card.GetDescription() + (reshuffled ? ", then reshuffled the deck." : ""), player);
                return card;
            }
        }

        private Card Discard(Player p, Card c) {
            var result = Dealer.Discard(p, c);
            if (p.Character == Character.SuzyLafayette && p.Lives > 0 && p.CardsInHand.Count() == 0) {
                DrawCards(p, 1);
            }
        }

        private void HitPlayer(Player target, int lives, Player attacker = null) {
            target.AddLives(-lives);
            var msgfortarget = "You lose " + lives + " lives.";
            var msgforothers = target.Name + " loses " + lives + "lives.";
            if (target.Lives == 0) {
                msgfortarget += "\n\nYou are out of lives! You died.";
                msgforothers += "\n\n" + target.Name + " is out of lives! " + target.Name + " was " + target.Role;
                Send(msgfortarget, msgforothers, target);
                Players.Remove(target);
                DeadPlayers.Add(target);
                if (Players.Any(x => x.Character == Character.VultureSam)) {
                    var vulturesam = Players.First(x => x.Character == Character.VultureSam);
                    foreach (var c in target.Cards)
                        vulturesam.StealFrom(target, c);
                    var msgforvs = msgforothers + "\nYou take in hand all of " + target.Name + "'s cards.";
                    msgforothers = vulturesam.Name + " takes in hand all of " + target.Name + "'s cards.";
                    Send(msgforvs, msgforothers, vulturesam);
                } else {
                    if (attacker != null && target.Role == Role.Outlaw) {
                        Send("You draw three cards as a reward.", attacker.Name + " draws three cards as a reward.", attacker);
                        DrawCards(attacker, 3);
                    }
                    SendToEveryone(target.Name + " discards all the cards.\n" + target.Cards.Aggregate("", (s, c) => s + c.GetDescription() + ", ").TrimEnd(',', ' ') + " go into the graveyard.");
                    foreach (var c in target.Cards)
                        Discard(target, c);
                }
                return;
            } else {
                Send(msgfortarget, msgforothers, target);
                switch (target.Character) {
                    case Character.BartCassidy:
                        DrawCards(target, lives);
                        return;
                    case Character.ElGringo:
                        if (attacker != null) {
                            var card = target.StealFrom(attacker).GetDescription();
                            Send("You stole {card} from {attacker.Name}'s hand.", $"{target.Name} stole a card from {attacker.Name}'s hand.", target);
                        }
                        return;
                    default:
                        return;
                }
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

        private InlineKeyboardMarkup MakeMenuFromCards (List<Card> list) {
            var buttons = new List<InlineKeyboardButton[]>();
            foreach (var c in list) 
                buttons.Add(new InlineKeyboardButton(c.GetDescription(), $"{Id}|card|{c.Encode()}").ToSinglet());
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
        private void Send(string text, Player p, IReplyMarkup menu = null, bool addextraspace = true) {
            //TODO complete refactoring of this. The Send methods are a real mess.
            throw new NotImplementedException();
//            if (p.TurnMsg == null)
//                p.TurnMsg = Bot.Send(p.QueuedMsg, p.Id, menu).Result;
//            else {
//                p.QueuedMsg += "\n" + (addextraspace ? "\n" : "") + text;
//                if ((menu == null && p.HasMenuActive) || (menu != null && !p.HasMenuActive)) {
//                    //TODO remove one of the \n's and choose each time if there should be some more space.
//                    p.TurnMsg = Bot.Edit(p.TurnMsg.Text + p.QueuedMsg, p.TurnMsg, menu).Result;
//                    p.HasMenuActive = menu != null;
//                    p.QueuedMsg = "";
//                }
//            }
        }


        /// <summary>
        /// Send the turn message to all the players except the ones in the list
        /// </summary>
        private void SendToEveryone (string text, List<Player> except, IReplyMarkup menu = null) {
            foreach (var p in Players.Union(DeadPlayers))
                if (!except.Contains(p))
                    Send(text, p, menu);
        }

        /// <summary>
        /// Send the turn message to all the players except the specified
        /// </summary>
        private void SendToEveryone (string text, Player except = null, IReplyMarkup menu = null) {
            SendToEveryone(text, except.ToSinglet().ToList(), menu);
        }

        private void Send (string textforplayer, string textforothers, Player p, IReplyMarkup menu = null) {
            Send(textforplayer, p, menu);
            SendToEveryone(textforothers, p);
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

