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
        private void StartGame()
        {
            UpdateJoinMessages(true);
            Status = GameStatus.Running;

            AssignRoles();
            AssignCharacters();
            DealCards();

            while (true)
            {
                Turn = (Turn + 1) % Players.Count();
                SendPlayerList();
                if (Turn == Players.Count())
                    Turn = 0;
                var currentplayer = Players[Turn];
                CheckDynamiteAndJail(currentplayer);
                if (currentplayer.CardsOnTable.All(x => x.Name != CardName.Jail))
                {
                    PhaseOne(currentplayer);
                    //TODO PhaseTwo(currentplayer);
                    PhaseThree(currentplayer);
                    SendMessages(); //do a last send and disable menus
                }
                else
                {
                    Discard(currentplayer, currentplayer.CardsOnTable.First(x => x.Name == CardName.Jail));
                }
                foreach (var p in Players)
                {
                    p.PlayerListMsg = null;
                    p.TurnMsg = null;
                    p.Choice = null; //just to be sure
                }
            }
        }

        private void AssignRoles()
        {
            var rolesToAssign = new List<Role>();
            var count = Players.Count();
            rolesToAssign.Add(Role.Sheriff);
            rolesToAssign.Add(Role.Renegade);
            if (count >= 3)
            {
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

            for (var i = 0; i < count; i++)
            {
                Players[i].Role = rolesToAssign[i];
            }

            //move sheriff to first place
            var sheriffindex = Players.IndexOf(Players.First((x => x.Role == Role.Sheriff)));
            Player temp = Players[0];
            Players[0] = Players[sheriffindex];
            Players[sheriffindex] = temp;

            return;
        }

        private void AssignCharacters()
        {
            var charsToAssign = new List<Character>();
            //charsToAssign.AddRange(Enum.GetValues(typeof(Character)).Cast<Character>().ToList());
            charsToAssign.AddRange(new[] { Character.JesseJones, Character.BartCassidy });

            foreach (var p in Players)
            {
                //assign characters
                p.Character = charsToAssign[Program.R.Next(charsToAssign.Count())];
                charsToAssign.Remove(p.Character);
                //assign lives
                p.SetLives();
            }
            return;
        }

        private void DealCards()
        {
            foreach (var p in Players)
                Dealer.DrawCards(p.Lives, p);
            return;
        }

        private void CheckDynamiteAndJail(Player curplayer)
        {
            if (curplayer.CardsOnTable.Any(x => x.Name == CardName.Dynamite))
            {
                TellEveryone($"{curplayer.Name} has the Dynamite!");
                var dynamite = curplayer.CardsOnTable.First(x => x.Name == CardName.Dynamite);
                var card = Draw(curplayer);
                if (card.Number < 10 && card.Suit == CardSuit.Spades)
                {
                    TellEveryone("The dynamite explodes!", false);
                    HitPlayer(curplayer, 3);
                    Discard(curplayer, dynamite);
                }
                else
                {
                    Player nextplayer = Players[(Turn + 1) % Players.Count()];
                    TellEveryone($"The dynamite passes to {nextplayer.Name}.", false);
                    nextplayer.StealFrom(curplayer, dynamite);
                    Dealer.PutPermCardOnTable(nextplayer, dynamite);
                }
            }
            if (curplayer.CardsOnTable.Any(x => x.Name == CardName.Jail))
            {
                TellEveryone($"{curplayer.Name} is in jail!");
                var jail = curplayer.CardsOnTable.First(x => x.Name == CardName.Jail);
                var card = Draw(curplayer);
                if (card.Suit == CardSuit.Hearts)
                {
                    TellEveryone($"The Jail is discarded and {curplayer.Name} plays their turn.", false);
                    Discard(curplayer, jail);
                }
                else
                {
                    TellEveryone($"{curplayer.Name} skips this turn. The Jail is discarded.", false);
                    //StartGame() will discard jail
                }
                return;
            }
            SendMessages();
        }

        private void PhaseOne(Player curplayer)
        {
            List<Card> cardsdrawn;
            switch (curplayer.Character)
            {
                case Character.KitCarlson:
                    Tell("You are Kit Carlson. You draw 3 cards from the deck, then choose one to put back at the top of the deck.", curplayer, true);
                    cardsdrawn = DrawCards(curplayer, 3);
                    Tell("Choose the card to put back into the deck.", curplayer, false, null);
                    SendMessages(curplayer, new InlineKeyboardMarkup(MakeMenuFromCards(cardsdrawn).ToArray()));
                    var cardchosen = (WaitForChoice(curplayer, 30)?.CardChosen ?? DefaultChoice.ChooseCardFrom(cardsdrawn));
                    Dealer.PutIntoDeck(curplayer, cardchosen);
                    Tell($"You put {cardchosen.GetDescription()} back at the top of the deck.", curplayer, false, $"{curplayer.Name} put a card back at the top the deck");
                    break;
                case Character.BlackJack:
                    Tell("You are Black Jack. You show the second card you draw; on Hearts or Diamonds, you draw one more card.", curplayer, true);
                    var secondcard = DrawCards(curplayer, 2)[1];
                    var heartsordiamonds = secondcard.Suit == CardSuit.Hearts || secondcard.Suit == CardSuit.Diamonds;
                    Tell($"The second card was {secondcard.Suit.ToEmoji()}, so you " + (heartsordiamonds ? "" : "can't ") + "draw another card", curplayer, false, $"{curplayer.Name} drew {secondcard.GetDescription()}, so they " + (heartsordiamonds ? "" : "can't ") + "draw another card");
                    if (heartsordiamonds)
                        DrawCards(curplayer, 1);
                    break;
                default:
                    //Jesse Jones & Pedro Ramirez can choose.
                    if ((curplayer.Character == Character.JesseJones || curplayer.Character == Character.PedroRamirez) && CanUseAbility(curplayer))
                    {
                        //ask them if they want to use the ability
                        Tell((
                            curplayer.Character == Character.JesseJones ?
                                "You are Jesse Jones: you can draw your first card from the hand of a player." :
                                $"You are Pedro Ramirez: you can draw your first card from the top of the graveyard. ({Dealer.Graveyard.Last().GetDescription()})") +
                            "\nDo you want to use your ability or do you want to draw from the deck?",
                            curplayer, true);
                        SendMessages(curplayer, MakeBoolMenu("Use ability", "Draw from deck"));

                        //now let's see what they chose
                        if (WaitForChoice(curplayer, 30)?.ChoseYes ?? DefaultChoice.UseAbilityPhaseOne)
                        {
                            if (curplayer.Character == Character.JesseJones)
                            {
                                //steal from a player
                                UsePanic(curplayer, true);
                            }
                            else
                            {
                                var card = Dealer.DrawFromGraveyard(curplayer).GetDescription();
                                Tell($"You drew {card} from the graveyard", curplayer, false, $"{curplayer.Name} drew {card} from the graveyard");
                                break;
                            }
                            DrawCards(curplayer, 1);
                            break;
                        }
                        //if they chose no, exit from the if block and behave like other players (draw 2 cards)
                    }
                    DrawCards(curplayer, 2);
                    break;
            }
            SendMessages();
        }

        private void PhaseThree(Player curplayer)
        {
            bool firsttime = true;
            var discarded = 0;
            while (true)
            {
                var msg = "";
                var discard = curplayer.CardsInHand.Count() > curplayer.Lives; //do they have to discard?

                if (firsttime)
                {
                    if (discard)
                        //tell how many cards they have to discard
                        msg = "You need to discard at least " + (curplayer.CardsInHand.Count() - curplayer.Lives).ToString() + " cards.\n";
                    Tell(msg + "Select the cards you want to discard.", curplayer, firsttime, null);
                }
                //send the menu
                SendMessages(curplayer, MakeCardsInHandMenu(curplayer, true));
                var choice = WaitForChoice(curplayer, 30);
                //yes = end of turn
                if (choice?.ChoseYes ?? false)
                    break;
                var cardchosen = choice?.CardChosen ?? DefaultChoice.ChooseCard;
                if (cardchosen != null || discard) //even if they are afk they need to discard anyway
                {
                    var card = Discard(curplayer, cardchosen).GetDescription();
                    Tell($"You discarded {card}" + card, curplayer, false, $"{curplayer.Name} discarded {card}");
                }
                else
                    break;

                firsttime = false;
                discarded++;

                //TODO: Fix sid ketchum! at ANY time he can do this.
                //sid ketchum can regain a life by discarding two cards
                if (curplayer.Character == Character.SidKetchum && discarded % 2 == 0 && curplayer.Lives < curplayer.MaxLives)
                {
                    Tell("You discarded two cards. Do you want to use your ability and regain one life point?", curplayer, true);
                    SendMessages(curplayer, MakeBoolMenu("Yes", "No"));
                    if (WaitForChoice(curplayer, 30)?.ChoseYes ?? DefaultChoice.UseAblityPhaseThree)
                        curplayer.AddLives(1);
                }
            }
            return;
        }

        private void UsePanic(Player curplayer, bool jessejonesability = false)
        {
            var possiblechoices = jessejonesability ? 
                Players.Where(x => x != curplayer && x.Lives > 0 && x.CardsInHand.Count() > 0) : 
                Players.Where(x => x.Lives > 0 && x.Cards.Count() > 0 && curplayer.DistanceSeen(x, Players) == 1);
            Player playerchosen;

            if (possiblechoices.Count() > 1)
            {
                Tell(
                    "Choose the player to steal from.\nThe number in parenthesis is the number of cards they have in their hand.", curplayer,
                    false
, $"{curplayer.Name} has decided to steal their first card from a player's hand.");
                //make the menu and send
                var buttonslist = new List<InlineKeyboardButton[]>();
                foreach (var p in possiblechoices)
                    buttonslist.Add(new[] { new InlineKeyboardButton(p.Name + $"({p.CardsInHand.Count()})", $"{Id}|player|{p.Id}") });
                SendMessages(curplayer, new InlineKeyboardMarkup(buttonslist.ToArray()));

                playerchosen = WaitForChoice(curplayer, 30)?.PlayerChosen ?? DefaultChoice.ChoosePlayer(Players);
            }
            else
                playerchosen = possiblechoices.First(); //if possiblechoices.Any() is false, this is gonna throw an exception I like.
            
            //tell the player who is the target
            Tell(possiblechoices.Count() == 1 ? $"The only player you can steal from is {playerchosen.Name}." : $"You chose to steal from {playerchosen.Name}.", curplayer, false, null);

            if (jessejonesability || playerchosen.CardsOnTable.Count() == 0)
            {
                //steal from hand
                
            }
            Card chosencard = null;
            if (!jessejonesability && playerchosen.CardsOnTable.Count() > 0)
            {
                //choose the card
                Tell("Choose which card to steal.", curplayer, false, $"{curplayer.Name} chose to steal a card from {playerchosen.Name}.");
                //make menu and send
                var buttonslist = MakeMenuFromCards(playerchosen.CardsOnTable);
                buttonslist.Add(new[] { new InlineKeyboardButton("Steal from hand", $"{Id}|bool|yes") });
                SendMessages(curplayer, new InlineKeyboardMarkup(buttonslist.ToArray()));

                //see what they chose
                var choice = WaitForChoice(curplayer, 30);
                //yes = card from hand
                if (!choice?.ChoseYes ?? true)
                    chosencard = choice?.CardChosen ?? DefaultChoice.ChooseCard;
            }
            //steal the card
            var card = curplayer.StealFrom(playerchosen, chosencard).GetDescription();
            if (chosencard == null)
            {
                //was from hand
                Tell($"You stole {card} from {playerchosen.Name}'s hand.", curplayer, false, null);
                Tell($"{curplayer.Name} stole you {card}", playerchosen, false, null);
                TellEveryone($"{curplayer.Name} stole a card from {playerchosen.Name}'s hand.", false, new[] { curplayer, playerchosen });
            }
            else
            {
                //was from table
                Tell($"You stole {card} from {playerchosen.Name}.", curplayer, false, $"{curplayer.Name} stole {card} from {playerchosen.Name}.");
            }
            SendMessages();
            return;
        }
        
        private bool CanUseAbility(Player player)
        {
            switch (player.Character)
            {
                case Character.JesseJones:
                    return Players.Where(x => x.Lives > 0 && x.CardsInHand.Count() > 0).Any();
                case Character.PedroRamirez:
                    return Dealer.Graveyard.Any();
                default:
                    throw new NotImplementedException();
            }
        }
        
        private ErrorMessage CanUseCard(Player player, Card card)
        {
            switch (card.Name)
            {
                case CardName.Panic:
                    return Players.Where(x => x.Lives > 0 && x.Cards.Count() > 0 && player.DistanceSeen(x, Players) == 1).Any() ? ErrorMessage.NoError : ErrorMessage.NoPlayersToStealFrom;
                default:
                    throw new NotImplementedException();
            }
        }
        
        private List<Card> DrawCards(Player p, int n)
        {
            var result = Dealer.DrawCards(n, p);
            var listofcards = result.Item1;
            var reshuffled = result.Item2;
            if (reshuffled == -1)
            {
                Tell($"You drew {string.Join(", ", listofcards.Select(x => x.GetDescription()))} from the deck.", p, false, $"{p.Name} drew {listofcards.Count()} cards from the deck.");
            }
            else
            {
                var cardsbefore = listofcards.Take(reshuffled);
                var cardsafter = listofcards.Skip(reshuffled);
                var msgforp = $"You drew {string.Join(", ", cardsbefore.Select(x => x.GetDescription()))} from the deck, then reshuffled the deck";
                var msgforothers = $"{p.Name} drew {cardsbefore.Count()} cards from the deck, then reshuffled the deck";
                if (cardsafter.Any())
                {
                    msgforp += $", then drew {string.Join(", ", cardsafter.Select(x => x.GetDescription()))}";
                    msgforothers += $", then drew {cardsafter.Count()} more cards";
                }
                Tell(msgforp + ".", p, false, msgforothers + ".");
            }
            return listofcards;
        }

        private Card Draw(Player player)
        {
            if (player.Character == Character.LuckyDuke)
            {
                Tell("You are Lucky Duke. You draw two cards, then choose one.", player, false, null);
                //draw the cards, then send a menu to choose
                var cards = DrawCards(player, 2);
                Tell("Choose a card.", player, false, null);
                SendMessages(player, new InlineKeyboardMarkup(MakeMenuFromCards(cards).ToArray()));

                //tell people the two cards
                var cardchosen = WaitForChoice(player, 30).CardChosen ?? DefaultChoice.ChooseCardFrom(cards);
                var carddiscarded = cards.First(x => x != cardchosen);
                Tell($"You choose {cardchosen.GetDescription()} and discard {carddiscarded.GetDescription()}.", player, false, $"{player.Name} chose {cardchosen.GetDescription()} and discarded {carddiscarded.GetDescription()}.");
                
                //discard the cards
                Discard(player, carddiscarded);
                Discard(player, cardchosen);
                return cardchosen;
            }
            else
            {
                var result = Dealer.DrawToGraveyard();
                var card = result.Item1;
                var reshuffled = result.Item2;
                Tell($"You drew {card.GetDescription()}" + (reshuffled ? ", then reshuffled the deck." : ""), player, false, $"{player.Name} drew {card.GetDescription()}" + (reshuffled ? ", then reshuffled the deck." : ""));
                return card;
            }
        }

        /// <summary>
        /// Player p discards card c. If Player is Suzy Lafayette, she draws a card.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        private Card Discard(Player p, Card c)
        {
            var result = Dealer.Discard(p, c);
            if (p.Character == Character.SuzyLafayette && p.Lives > 0 && p.CardsInHand.Count() == 0)
            {
                DrawCards(p, 1);
            }
            return result;
        }
        
        private void HitPlayer(Player target, int lives, Player attacker = null)
        {
            target.AddLives(-lives);
            Tell($"You lose {lives} lives.", target, true, textforothers: $"{target.Name} loses {lives} lives.");
            //TODO Deal with beers here!!
            if (target.Lives == 0)
            {
                Tell($"You're out of lives! You died.", target, true, $"{target.Name} died! {target.Name} was {target.Role.GetString<Role>()}");
                Players.Remove(target);
                DeadPlayers.Add(target);

                var vulturesam = Players.FirstOrDefault(x => x.Character == Character.VultureSam);
                if (vulturesam != null)
                {
                    foreach (var c in target.Cards)
                        vulturesam.StealFrom(target, c);
                    Tell($"You take in hand all {target.Name}'s cards.", vulturesam, false, $"{vulturesam.Name} takes in hand all {target.Name}'s cards.");
                }
                else
                {
                    if (attacker != null && target.Role == Role.Outlaw)
                    {
                        Tell("You draw three cards as a reward.", attacker, false, $"{attacker.Name} draws three cards as a reward.");
                        DrawCards(attacker, 3);
                    }
                    TellEveryone($"{target.Name} discards all the cards: " + string.Join(", ", target.Cards.Select(x => x.GetDescription())));
                    foreach (var c in target.Cards)
                        Discard(target, c);
                }
            }
            else
            {
                switch (target.Character)
                {
                    case Character.BartCassidy:
                        DrawCards(target, lives);
                        break;
                    case Character.ElGringo:
                        if (attacker != null)
                        {
                            var card = target.StealFrom(attacker).GetDescription();
                            Tell($"You stole {card} from {attacker.Name}'s hand.", target, false, null);
                            Tell($"{target.Name} stole you {card}", attacker, false, null);
                            TellEveryone($"{target.Name} stole a card from {attacker.Name}'s hand.", false, new[] { attacker, target });
                        }
                        break;
                    default:
                        break;
                }
            }
            SendMessages();
            return;
        }
        
    }

}