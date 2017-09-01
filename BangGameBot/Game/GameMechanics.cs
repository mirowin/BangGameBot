﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;

namespace BangGameBot
{
    public partial class Game
    {
        private void StartGame()
        {
            Status = GameStatus.PhaseZero;
            AssignRoles();
            AssignCharacters();
            DealCards();

            while (Status != GameStatus.Ending)
            {
                Status = GameStatus.PhaseZero;
                Turn = (Turn + 1) % Players.Count();
                SendPlayerList();
                if (Turn == Players.Count())
                    Turn = 0;
                var currentplayer = Players[Turn];
                CheckDynamiteAndJail(currentplayer);
                if (!currentplayer.CardsOnTable.Any(x => x.Name == CardName.Jail))
                {
                    PhaseOne(currentplayer);
                    PhaseTwo(currentplayer);
                    PhaseThree(currentplayer);
                    if (Status != GameStatus.Ending)
                        SendMessages(); //do a last send and disable menus
                }
                else if (Status != GameStatus.Ending)
                    Discard(currentplayer, currentplayer.CardsOnTable.First(x => x.Name == CardName.Jail));

                ResetPlayers();
            }

            Players.Clear();
            Handler.Games.Remove(this);
            return;
        }

        private void ResetPlayers()
        {
            foreach (var p in Players)
            {
                p.UsedBang = false;
                p.Choice = null; //just to be sure
            }
        }

        private void AssignRoles()
        {
            var rolesToAssign = new List<Role>();
            var count = Players.Count();
            rolesToAssign.Add(Role.Sheriff);
            rolesToAssign.Add(Role.Renegade);
            if (count >= 3)
                rolesToAssign.Add(Role.Outlaw);
            if (count >= 4)
                rolesToAssign.Add(Role.Outlaw);
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
            charsToAssign.AddRange(Enum.GetValues(typeof(Character)).Cast<Character>().Where(x => x != Character.None).ToList());
            
            foreach (var p in Players)
            {
                //assign characters
                p.Character = charsToAssign.Random();
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
                TellEveryone($"{curplayer.Name} has the Dynamite!", CardName.Dynamite);
                var dynamite = curplayer.CardsOnTable.First(x => x.Name == CardName.Dynamite);
                var card = Draw(curplayer);
                if (card.Number < 10 && card.Suit == CardSuit.Spades)
                {
                    TellEveryone("The dynamite explodes!", CardName.Dynamite);
                    HitPlayer(curplayer, 3);
                    if (Players.Count() == 0) return;

                    Discard(curplayer, dynamite);
                }
                else
                {
                    Player nextplayer = Players[(Turn + 1) % Players.Count()];
                    TellEveryone($"The dynamite passes to {nextplayer.Name}.", CardName.Dynamite);
                    nextplayer.StealFrom(curplayer, dynamite);
                    Dealer.PutPermCardOnTable(nextplayer, dynamite);
                }
            }
            if (curplayer.CardsOnTable.Any(x => x.Name == CardName.Jail))
            {
                TellEveryone($"{curplayer.Name} is in jail!", CardName.Jail);
                var jail = curplayer.CardsOnTable.First(x => x.Name == CardName.Jail);
                var card = Draw(curplayer);
                if (card.Suit == CardSuit.Hearts)
                {
                    TellEveryone($"The Jail is discarded and {curplayer.Name} plays their turn.", CardName.Jail);
                    Discard(curplayer, jail);
                }
                else
                {
                    TellEveryone($"{curplayer.Name} skips this turn. The Jail is discarded.", CardName.Jail);
                    //StartGame() will discard jail
                }
                return;
            }
            SendMessages();
        }

        private void PhaseOne(Player curplayer)
        {
            if (Status == GameStatus.Ending) return;
            Status = GameStatus.PhaseOne;

            List<Card> cardsdrawn;
            switch (curplayer.Character)
            {
                case Character.KitCarlson:
                    Tell("You are Kit Carlson. You draw 3 cards from the deck, then choose one to put back at the top of the deck.", curplayer, character: Character.KitCarlson);
                    cardsdrawn = DrawCards(curplayer, 3);
                    Tell("Choose the card to put back into the deck.", curplayer);
                    SendMessages(curplayer, MakeMenuFromCards(cardsdrawn, curplayer));
                    var cardchosen = (WaitForChoice(curplayer, 30)?.CardChosen ?? DefaultChoice.ChooseCardFrom(cardsdrawn));
                    Dealer.PutIntoDeck(curplayer, cardchosen);
                    Tell($"You put {cardchosen.GetDescription()} back at the top of the deck.", curplayer, cardchosen.Name, Character.KitCarlson, textforothers: $"{curplayer.Name} put a card back at the top the deck");
                    break;
                case Character.BlackJack:
                    Tell("You are Black Jack. You show the second card you draw; on Hearts or Diamonds, you draw one more card.", curplayer, character: Character.BlackJack);
                    var secondcard = DrawCards(curplayer, 2)[1];
                    var heartsordiamonds = secondcard.Suit == CardSuit.Hearts || secondcard.Suit == CardSuit.Diamonds;
                    Tell($"The second card was {secondcard.Suit.ToEmoji()}, so you " + (heartsordiamonds ? "" : "can't ") + "draw another card", curplayer, secondcard.Name, Character.BlackJack, $"{curplayer.Name} drew {secondcard.GetDescription()}, so they " + (heartsordiamonds ? "" : "can't ") + "draw another card");
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
                            curplayer, curplayer.Character == Character.JesseJones ? CardName.None : Dealer.Graveyard.Last().Name, curplayer.Character);
                        SendMessages(curplayer, MakeBoolMenu("Use ability", "Draw from deck"));

                        //now let's see what they chose
                        if (WaitForChoice(curplayer, 30)?.ChoseYes ?? DefaultChoice.UseAbilityPhaseOne)
                        {
                            if (curplayer.Character == Character.JesseJones)
                            {
                                //steal from a player
                                UsePanicOrCatBalou(curplayer);
                            }
                            else //pedro ramirez
                            {
                                var card = Dealer.DrawFromGraveyard(curplayer);
                                var desc = card.GetDescription();
                                Tell($"You drew {desc} from the graveyard", curplayer, card.Name, Character.PedroRamirez, textforothers: $"{curplayer.Name} drew {desc} from the graveyard");
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

        private void PhaseTwo(Player curplayer)
        {
            if (Status == GameStatus.Ending) return;
            Status = GameStatus.PhaseTwo;

            bool firsttime = true;
            while (curplayer.CardsInHand.Count() > 0)
            { 
                //ask them what they want to do
                Tell("Select the card you want to use.", curplayer);
                var menu = AddYesButton(MakeCardsInHandMenu(curplayer, Situation.Standard), "Discard cards »");
                if (curplayer.Character == Character.SidKetchum)
                {
                    //sid ketchum can discard two cards
                    if (firsttime)
                    {
                        Tell("At any time, you can use your ability to discard two cards and regain one life point.", curplayer, character: Character.SidKetchum);
                        firsttime = false;
                    }
                    if (CanUseAbility(curplayer))
                        menu.Add(new[] { new InlineKeyboardCallbackButton("Use ability", $"game|bool|no") });
                }
                SendMessages(curplayer, menu);

                //see what they chose
                var choice = WaitForChoice(curplayer, 60);
                if (choice == null)
                {   //afk
                    return;
                }
                if (choice.ChoseYes == true)
                { //discard cards
                    return;
                }
                if (choice.ChoseYes == false)
                {
                    if (curplayer.Character != Character.SidKetchum)
                        throw new Exception("Someone chose no during Phase Two...");
                    //it was sid ketchum! he wants to discard two cards and regain one life point.
                    Tell($"Choose the cards to discard.", curplayer);
                    SendMessages(curplayer, MakeMenuFromCards(curplayer.CardsInHand, curplayer));
                    var chosencard = WaitForChoice(curplayer, 30)?.CardChosen ?? curplayer.ChooseCardFromHand();
                    Discard(curplayer, choice.CardChosen);
                    Tell($"You discarded {choice.CardChosen.GetDescription()}. Select another card to discard.", curplayer, choice.CardChosen.Name);
                    SendMessages(curplayer, MakeMenuFromCards(curplayer.CardsInHand, curplayer));
                    var secondchosencard = WaitForChoice(curplayer, 30)?.CardChosen ?? curplayer.ChooseCardFromHand();
                    Discard(curplayer, secondchosencard);
                    Tell(
                        $"You discarded {secondchosencard.GetDescription()}, and regained a life point.",
                        curplayer, chosencard.Name,
                        textforothers: $"{curplayer.Name} discarded {chosencard.GetDescription()} and {secondchosencard.GetDescription()}, and regained a life point!");
                    TellEveryone("", secondchosencard.Name, Character.SidKetchum);
                    curplayer.AddLives(1);
                    //SendMessages();
                    continue;
                }
            

                //get the card
                var cardchosen = choice.CardChosen;
                if (cardchosen.GetCardType() == CardType.PermCard || cardchosen.GetCardType() == CardType.Weapon)
                {
                    if (cardchosen.Name == CardName.Jail)
                    {
                        var possiblechoices = Players.Where(x => x.Role != Role.Sheriff && !x.CardsOnTable.Any(c => c.Name == CardName.Jail) && x.Id != curplayer.Id);
                        Player chosenplayer;
                        if (possiblechoices.Count() == 1)
                            chosenplayer = possiblechoices.First();
                        else
                        {
                            Tell($"Choose a player to put in jail.", curplayer, CardName.Jail);
                            SendMessages(curplayer, possiblechoices.Select(x => new[] { new InlineKeyboardCallbackButton(x.Name, $"game|player|{x.Id}") }));
                            chosenplayer = WaitForChoice(curplayer, 30)?.PlayerChosen ?? possiblechoices.Random();
                        }
                        chosenplayer.StealFrom(curplayer, cardchosen);
                        Dealer.PutPermCardOnTable(chosenplayer, cardchosen);
                        Tell($"You put {chosenplayer.Name} in jail!", curplayer, CardName.Jail);
                        Tell($"{curplayer.Name} put you in jail!", chosenplayer, CardName.Jail);
                        TellEveryone($"{curplayer.Name} put {chosenplayer.Name} in jail!", CardName.Jail, except: new[] { chosenplayer, curplayer });
                    }
                    else
                    {
                        var discardweapon = Dealer.PutPermCardOnTable(curplayer, cardchosen);
                        var msg = "";
                        if (discardweapon != null)
                            msg = $", and discarded {discardweapon.GetDescription()}";
                        msg += ".";
                        Tell($"You put {cardchosen.GetDescription()} in play" + msg, curplayer, cardchosen.Name, textforothers: $"{curplayer.Name} put {cardchosen.GetDescription()} in play" + msg);
                    }
                    //SendMessages();
                    continue;
                }

                Tell($"You used {cardchosen.GetDescription()}", curplayer, cardchosen.Name, textforothers: $"{curplayer.Name} used {cardchosen.GetDescription()}.");
                Discard(curplayer, cardchosen);
                switch (cardchosen.Name)
                {
                    case CardName.Bang:
                        UseBang(curplayer);
                        break;
                    case CardName.Missed:
                        if (curplayer.Character != Character.CalamityJanet)
                            throw new Exception("Someone is using Missed! during their turn!");
                        TellEveryone("", character: Character.CalamityJanet);
                        UseBang(curplayer);
                        break;
                    case CardName.Beer:
                        if (Players.Count() == 2) 
                            throw new Exception("Someone is using a Beer in the final duel!");
                        curplayer.AddLives(1);
                        Tell($"You regained one life point.", curplayer, CardName.Beer, textforothers: $"{curplayer.Name} regained one life point.");
                        break;
                    case CardName.CatBalou:
                    case CardName.Panic:
                        UsePanicOrCatBalou(curplayer, cardchosen.Name == CardName.CatBalou);
                        break;
                    case CardName.Duel:
                        UseDuel(curplayer);
                        break;
                    case CardName.Gatling:
                        TellEveryone($"{curplayer.Name} shot everyone!", CardName.Gatling, except: curplayer.ToSinglet());
                        for (var i = Turn + 1; i != Turn; i = ++i % Players.Count())
                        {
                            var target = Players[i];
                            if (!Missed(curplayer, target, true))
                            {
                                HitPlayer(target, 1, curplayer);
                                if (Status == GameStatus.Ending) return;
                            }
                        }
                        break;
                    case CardName.GeneralStore:
                        var reshuffled = Dealer.PeekCards(Players.Count()).Item2;
                        TellEveryone(Dealer.PeekedCards.Aggregate($"{curplayer.Name} draws the following cards from the deck" + (reshuffled ? " reshuffling it" : " ") + ":\n", (s, c) => s + c.GetDescription() + "\n"));
                        foreach (var card in Dealer.PeekedCards.Select(x => x.Name))
                            TellEveryone("", card);
                        SendMessages();
                        for (var i = 0; i < Players.Count(); i++)
                        {
                            var index = (i + Turn) % Players.Count();
                            var player = Players[index];
                            Tell("Choose the card to take in hand.", player);
                            SendMessages(player, MakeMenuFromCards(Dealer.PeekedCards, player));
                            var chosencard = WaitForChoice(player, 30)?.CardChosen ?? DefaultChoice.ChooseCardFrom(Dealer.PeekedCards);
                            Tell($"You take {chosencard.GetDescription()} in hand.", player, textforothers: $"{player.Name} took {chosencard.GetDescription()} in hand");
                            Dealer.DrawFromPeeked(player, chosencard);
                        }
                        break;
                    case CardName.Indians:
                        UseIndians(curplayer);
                        break;
                    case CardName.Saloon:
                        foreach (var p in Players)
                            p.AddLives(1);
                        TellEveryone($"Everyone regained a life point.", CardName.Saloon);
                        break;
                    case CardName.Stagecoach:
                        DrawCards(curplayer, 2);
                        break;
                    case CardName.WellsFargo:
                        DrawCards(curplayer, 3);
                        break;
                }
                SendMessages();
            }
            return;
        }

        private void UseDuel(Player curplayer)
        {
            var possiblechoices = Players.Where(x => x.Id != curplayer.Id);
            Player target;
            if (possiblechoices.Count() == 1)
            {
                target = possiblechoices.First();
            }
            else
            {
                Tell("Choose a player to challenge to duel.", curplayer, CardName.Duel);
                SendMessages(curplayer, possiblechoices.Select(x => (new[] { new InlineKeyboardCallbackButton(x.Name, $"game|player|{x.Id}") })));
                target = WaitForChoice(curplayer, 30)?.PlayerChosen ?? DefaultChoice.ChoosePlayer(possiblechoices);
                Tell($"You chose to challenge {target.Name} to duel.", curplayer, CardName.Duel);
            }

            Tell($"You have been challenged to duel!", target, CardName.Duel);
            var duelling = new[] { target, curplayer };
            TellEveryone($"{curplayer.Name} challenged {target.Name} to duel.", CardName.Duel, except: duelling);
            for (var i = 0; true; i = 1 - i)
            {
                var player = duelling[i];
                if (player.CardsInHand.Any(c => c.Name == CardName.Bang) || (player.CardsInHand.Any(c => c.Name == CardName.Missed) && player.Character == Character.CalamityJanet))
                {
                    Tell("You may discard a Bang! card, or lose a life point.", player, character: player.Character == Character.CalamityJanet ? Character.CalamityJanet : Character.None);
                    SendMessages(player, AddYesButton(MakeCardsInHandMenu(player, Situation.DiscardBang), "Lose a life point"));
                    WaitForChoice(player, 30);
                    if (player.Choice?.CardChosen != null)
                    {
                        if (player.Choice.CardChosen.Name != CardName.Bang && (player.Choice.CardChosen.Name != CardName.Missed || player.Character != Character.CalamityJanet))
                            throw new Exception("The player was meant to discard a Bang! card.");
                        Discard(player, player.Choice.CardChosen);
                        Tell($"You discarded {player.Choice.CardChosen.GetDescription()}.", player, player.Choice.CardChosen.Name, textforothers: $"{player.Name} discarded {player.Choice.CardChosen.GetDescription()}");
                        continue;
                    }
                }
                HitPlayer(player, 1, duelling[1 - i]);
                if (Status == GameStatus.Ending) return;
                break;
            }
            return;
        }

        private void UseIndians(Player curplayer)
        {
            var candiscard = Players.Where(x => x.Id != curplayer.Id && (x.CardsInHand.Any(c => c.Name == CardName.Bang) || (x.Character == Character.CalamityJanet && x.CardsInHand.Any(c => c.Name == CardName.Missed))));
            foreach (var p in candiscard)
            {
                Tell("You have a Bang! card! You may discard it, or lose a life point.", p, character: curplayer.Character == Character.CalamityJanet ? Character.CalamityJanet : Character.None);
                SendMessages(p, AddYesButton(MakeCardsInHandMenu(p, Situation.DiscardBang), "Lose a life point"));
            }
            var tasks = new List<Task>();
            foreach (var p in candiscard)
            {
                var task = new Task(() => {
                    var choice = WaitForChoice(p, 30)?.CardChosen;
                    Tell("You chose to " + (choice != null ? "discard a Bang! card." : "lose a life point.") + "\nWaiting for the other players to choose...", p);
                    SendMessage(p);
                });
                tasks.Add(task);
                task.Start();
            }
            while (tasks.Any(x => !x.IsCompleted))
                Task.Delay(1000).Wait();
            
            var missedplayers = candiscard.Where(x => x.Choice?.CardChosen != null);
            foreach (var p in missedplayers)
            {
                if (p.Choice.CardChosen.Name != CardName.Bang && (p.Choice.CardChosen.Name != CardName.Missed || p.Character != Character.CalamityJanet))
                    throw new Exception("The player was meant to discard a Bang! card.");
                Discard(p, p.Choice.CardChosen);
                Tell($"You discarded {p.Choice.CardChosen.GetDescription()}.", p, p.Choice.CardChosen.Name, textforothers: $"{p.Name} discarded {p.Choice.CardChosen.GetDescription()}");
            }
            foreach (var p in Players.Where(x => !missedplayers.Contains(x) && x.Id != curplayer.Id))
            {
                HitPlayer(p, 1, curplayer);
                if (Status == GameStatus.Ending) return;
            }
            return;
        }

        private void UseBang(Player attacker)
        {
            if (attacker.UsedBang && attacker.Character == Character.WillyTheKid)
                TellEveryone("", character: Character.WillyTheKid);
            attacker.UsedBang = true;
            var possiblechoices = Players.Where(x => x.IsReachableBy(attacker, Players));
            Player target;
            if (possiblechoices.Count() == 1)
            {
                target = possiblechoices.First();
                Tell($"The only player you can shoot is {target.Name}.", attacker);
            }
            else
            {
                Tell("Choose a player to shoot.", attacker);
                SendMessages(attacker, possiblechoices.Select(x => (new[] { new InlineKeyboardCallbackButton(x.Name, $"game|player|{x.Id}") })));
                target = WaitForChoice(attacker, 30)?.PlayerChosen ?? DefaultChoice.ChoosePlayer(possiblechoices);
                Tell($"You chose to shoot {target.Name}.", attacker);
            }

            TellEveryone($"{attacker.Name} shot {target.Name}!", character: attacker.Character == Character.SlabTheKiller ? Character.SlabTheKiller : Character.None, except: new[] { target, attacker });
            Tell($"You were shot by {attacker.Name}!" + (attacker.Character == Character.SlabTheKiller ? " You'll need two Missed! cards to miss his Bang!" : ""), target, character: attacker.Character == Character.SlabTheKiller ? Character.SlabTheKiller : Character.None);

            if (!Missed(attacker, target, false))
            {
                HitPlayer(target, 1, attacker);
                if (Status == GameStatus.Ending) return;
            }

            return;

        }

        private bool Missed(Player attacker, Player target, bool isgatling)
        {
            int missed = 0;

            //jourdounnais' ability
            if (target.Character == Character.Jourdounnais && UseBarrel(target, true))
                missed++;
            if (CheckMissed(attacker, target, missed, isgatling))
                return true;

            //barrel
            if (target.CardsOnTable.Any(x => x.Name == CardName.Barrel) && UseBarrel(target, false))
                missed++;
            if (CheckMissed(attacker, target, missed, isgatling))
                return true;

            //card
            var counter = 0;
            do
            {
                if (target.CardsInHand.Any(x => x.Name == CardName.Missed) || (target.Character == Character.CalamityJanet && target.CardsInHand.Any(x => x.Name == CardName.Bang)))
                {
                    Tell("You have a Missed! card. You have the possibility to miss the shoot!", target, character: target.Character == Character.CalamityJanet ? Character.CalamityJanet : Character.None);
                    SendMessages(target, AddYesButton(MakeCardsInHandMenu(target, Situation.PlayerShot), "Lose a life point"));
                    var choice = WaitForChoice(target, 30)?.CardChosen;
                    if (choice != null)
                    {
                        if (choice.Name != CardName.Missed && (choice.Name != CardName.Bang || target.Character != Character.CalamityJanet))
                            throw new Exception("Target was supposed to miss the shoot.");
                        Tell($"You played {choice.GetDescription()}.", target, character: target.Character == Character.CalamityJanet ? Character.CalamityJanet : Character.None, textforothers: $"{target.Name} played {choice.GetDescription()}.");
                        Discard(target, choice);
                        missed++;
                    }
                }
            } while (!isgatling && attacker.Character == Character.SlabTheKiller && ++counter < 2);
            if (CheckMissed(attacker, target, missed, isgatling))
                return true;

            return false;
        }

        private bool UseBarrel(Player target, bool jourdounnais)
        {
            if (jourdounnais && target.Character != Character.Jourdounnais)
                throw new Exception("Someone is pretending to be Jourdounnais...");
            Tell(jourdounnais ? 
                "You are Jourdounnais: you are considered to have a Barrel in play. Do you want to use this ability?" : 
                "You have a Barrel in play. Do you want to use it?",
                target, jourdounnais ? CardName.None : CardName.Barrel, jourdounnais ? Character.Jourdounnais : Character.None);
            var candefend = (jourdounnais && target.CardsOnTable.Any(x => x.Name == CardName.Barrel)) || (target.CardsInHand.Any(x => x.Name == CardName.Missed) || (target.CardsInHand.Any(x => x.Name == CardName.Bang) && target.Character == Character.CalamityJanet));
            SendMessages(target, MakeBoolMenu(jourdounnais ? "Use ability" : "Use Barrel", candefend ? "Defend otherwise" : "Lose a life point"));
            var choice = WaitForChoice(target, 20)?.ChoseYes ?? DefaultChoice.UseBarrel;
            if (!choice)
                return false;

            TellEveryone($"{target.Name} chose to use " + (jourdounnais ? "the ability." : "the Barrel."), jourdounnais ? CardName.None : CardName.Barrel, jourdounnais ? Character.Jourdounnais : Character.None, target.ToSinglet());
            if (Draw(target).Suit == CardSuit.Hearts)
            {
                Tell("You drew a Heart! You missed the shoot.", target, textforothers: $"{target.Name} drew a Heart! The shoot is missed.");
                return true;
            }
            else
                return false;
        }

        private bool CheckMissed(Player curplayer, Player target, int missed, bool isgatling)
        {
            if (missed == (curplayer.Character == Character.SlabTheKiller && !isgatling ? 2 : 1))
            {
                Tell($"{target.Name} missed your shot!", curplayer);
                Tell($"You missed {curplayer.Name}'s shot!", target);
                TellEveryone($"{target.Name} missed {curplayer.Name}'s shot!", character: curplayer.Character == Character.SlabTheKiller ? Character.SlabTheKiller : Character.None, except: new[] { target, curplayer });
                SendMessages();
                return true;
            }
            else
                return false;
        }

        private void PhaseThree(Player curplayer)
        {
            if (Status == GameStatus.Ending) return;
            Status = GameStatus.PhaseThree;

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
                    Tell(msg + "Select the cards you want to discard.", curplayer);
                }
                //send the menu
                var menu = MakeMenuFromCards(curplayer.CardsInHand, curplayer);
                if (curplayer.CardsInHand.Count() <= curplayer.Lives)
                    menu = AddYesButton(menu, "End of turn");
                SendMessages(curplayer, menu);
                var choice = WaitForChoice(curplayer, 30);
                //yes = end of turn
                if (choice?.ChoseYes ?? false)
                    break;
                var cardchosen = choice?.CardChosen ?? DefaultChoice.ChooseCard;
                if (cardchosen != null || discard) //even if they are afk they need to discard anyway
                {
                    var card = Discard(curplayer, cardchosen);
                    var desc = card.GetDescription();
                    Tell($"You discarded {desc}", curplayer, card.Name, textforothers: $"{curplayer.Name} discarded {desc}");
                }
                else
                    break;

                firsttime = false;
                discarded++;

                //sid ketchum can regain a life by discarding two cards
                if (curplayer.Character == Character.SidKetchum && discarded % 2 == 0 && curplayer.Lives < curplayer.MaxLives)
                {
                    Tell("You discarded two cards. Do you want to use your ability and regain one life point?", curplayer, character: Character.SidKetchum);
                    SendMessages(curplayer, MakeBoolMenu("Yes", "No"));
                    if (WaitForChoice(curplayer, 30)?.ChoseYes ?? DefaultChoice.UseAblityPhaseThree)
                        curplayer.AddLives(1);
                }
            }
            return;
        }

        private void UsePanicOrCatBalou(Player curplayer, bool iscatbalou = false)
        {
            var jessejonesability = curplayer.Character == Character.JesseJones && Status == GameStatus.PhaseOne && !iscatbalou;
            if (!jessejonesability && Status != GameStatus.PhaseTwo)
                throw new Exception("Someone is using Panic! / Cat Balou outside Phase Two...");

            var possiblechoices = iscatbalou ? 
                Players.Where(x => x.Id != curplayer.Id && x.Cards.Count() > 0) :
                (jessejonesability ? 
                    Players.Where(x => x.Id != curplayer.Id && x.CardsInHand.Count() > 0) : 
                    Players.Where(x => x.Cards.Count() > 0 && curplayer.DistanceSeen(x, Players) == 1)
                );
            Player playerchosen;

            if (possiblechoices.Count() > 1)
            {
                Tell(
                    (iscatbalou ? 
                        "Choose the player of which you want to discard a card." : 
                        "Choose the player to steal from.")
                    + "\nThe number in parenthesis is the number of cards they have in their hand.", 
                    curplayer, jessejonesability ? CardName.None : (iscatbalou ? CardName.CatBalou : CardName.Panic), jessejonesability ? Character.JesseJones : Character.None, 
                    jessejonesability ? 
                        $"{curplayer.Name} has decided to steal their first card from a player's hand." :
                        null
                );
                //make the menu and send
                var buttonslist = new List<InlineKeyboardCallbackButton[]>();
                foreach (var p in possiblechoices)
                    buttonslist.Add(new[] { new InlineKeyboardCallbackButton(p.Name + $"({p.CardsInHand.Count()})", $"game|player|{p.Id}") });
                SendMessages(curplayer, buttonslist);

                playerchosen = WaitForChoice(curplayer, 30)?.PlayerChosen ?? DefaultChoice.ChoosePlayer(possiblechoices);
            }
            else
                playerchosen = possiblechoices.First(); //if possiblechoices.Any() is false, this is gonna throw an exception I like.
            
            //tell the player who is the target
            Tell(possiblechoices.Count() == 1 ? 
                (iscatbalou ? $"The only player you can discard a card from is {playerchosen.Name}" : $"The only player you can steal from is {playerchosen.Name}.") : 
                (iscatbalou ? $"You chose to discard a card from {playerchosen.Name}" : $"You chose to steal from {playerchosen.Name}."), 
                curplayer);
            
            Card chosencard = null;
            if (!jessejonesability && playerchosen.CardsOnTable.Count() > 0)
            {
                //choose the card
                Tell(
                    iscatbalou ? "Choose which card to discard." : "Choose which card to steal.", 
                    curplayer, iscatbalou ? CardName.CatBalou : CardName.Panic,
                    textforothers: iscatbalou ? $"{curplayer.Name} chose to discard a card from {playerchosen.Name}." : $"{curplayer.Name} chose to steal a card from {playerchosen.Name}.");
                //make menu and send
                var buttonslist = AddYesButton(MakeMenuFromCards(playerchosen.CardsOnTable, curplayer), iscatbalou ? "Discard from hand" : "Steal from hand");
                SendMessages(curplayer, buttonslist);

                //see what they chose
                var choice = WaitForChoice(curplayer, 30);
                //yes = card from hand
                if (!choice?.ChoseYes ?? true)
                    chosencard = choice?.CardChosen ?? DefaultChoice.ChooseCard;
            }
            //steal the card
            var realcard = (iscatbalou ?
                Dealer.Discard(playerchosen, chosencard) :
                curplayer.StealFrom(playerchosen, chosencard)
            );
            var card = realcard.GetDescription();
            if (chosencard == null)
            {
                //was from hand
                Tell(iscatbalou ? $"You discarded {card} from {playerchosen.Name}'s hand." : $"You stole {card} from {playerchosen.Name}'s hand.", 
                    curplayer, realcard.Name);
                Tell(iscatbalou? $"{curplayer.Name} discarded your {card}." : $"{curplayer.Name} stole you {card}", playerchosen, realcard.Name);
                TellEveryone(iscatbalou ? $"{curplayer.Name} discarded {card} from {playerchosen.Name}'s hand." : $"{curplayer.Name} stole a card from {playerchosen.Name}'s hand.", realcard.Name, except: new[] { curplayer, playerchosen });
            }
            else
            {
                //was from table
                Tell(iscatbalou ? $"You discarded {card} from {playerchosen.Name}" : $"You stole {card} from {playerchosen.Name}.", curplayer, realcard.Name, textforothers: iscatbalou ? $"{curplayer.Name} discarded {card} from {playerchosen.Name}." : $"{curplayer.Name} stole {card} from {playerchosen.Name}.");
            }
            SendMessages();
            return;
        }
        
        private bool CanUseAbility(Player player, Situation s = Situation.Standard)
        {
            switch (player.Character)
            {
                case Character.JesseJones:
                    if (Status != GameStatus.PhaseOne)
                        throw new InvalidOperationException("Jesse Jones is using his ability while not in Phase One");
                    return Players.Any(x => x.CardsInHand.Count() > 0);
                case Character.PedroRamirez:
                    if (Status != GameStatus.PhaseOne)
                        throw new InvalidOperationException("Pedro Ramirez is using his ability while not in Phase One");
                    return Dealer.Graveyard.Any();
                case Character.SidKetchum:
                    if (s == Situation.PlayerDying)
                    {
                        if (player.Lives > 0)
                            throw new InvalidOperationException("Sid Ketchum is doing his ability while living");
                        //regainable lives = beers count + other cards / 2 (2 non-beers = 1 life)
                        return player.CardsInHand.Count(x => x.Name == CardName.Beer) + player.CardsInHand.Count(x => x.Name != CardName.Beer) / 2 > -player.Lives;
                    }
                    else
                        return player.Lives < player.MaxLives;
                default:
                    throw new ArgumentException();
            }
        }

        private ErrorMessage CanUseCard(Player player, Card card, Situation s = Situation.Standard)
        {
            switch (s)
            {
                case Situation.PlayerDying:
                    if (card.Name == CardName.Beer)
                        return ErrorMessage.NoError;
                    else
                        return ErrorMessage.UseBeer;

                case Situation.PlayerShot:
                    if (card.Name == CardName.Missed || (card.Name == CardName.Bang && player.Character == Character.CalamityJanet))
                        return ErrorMessage.NoError;
                    else
                        return ErrorMessage.UseMissed;

                case Situation.DiscardBang:
                    if (card.Name == CardName.Bang || (card.Name == CardName.Missed && player.Character == Character.CalamityJanet))
                        return ErrorMessage.NoError;
                    else
                        return ErrorMessage.UseBang;
                //normal situation
                case Situation.Standard:
                default:
                    switch (card.Name)
                    {
                        case CardName.Bang:
                            return (!player.UsedBang || player.Weapon?.Name == CardName.Volcanic || player.Character == Character.WillyTheKid) ? (Players.Any(x => x.IsReachableBy(player, Players)) ? ErrorMessage.NoError : ErrorMessage.NoReachablePlayers) : ErrorMessage.OnlyOneBang;
                        case CardName.Missed:
                            return player.Character == Character.CalamityJanet ? ErrorMessage.NoError : ErrorMessage.CantUseMissed;
                        case CardName.Jail:
                            return Players.Any(x => x.Role != Role.Sheriff && !x.CardsOnTable.Any(c => c.Name == CardName.Jail) && x.Id != player.Id) ? ErrorMessage.NoError : ErrorMessage.NoPlayersToPutInJail;
                        case CardName.Panic:
                            return Players.Any(x => x.Cards.Count() > 0 && player.DistanceSeen(x, Players) == 1) ? ErrorMessage.NoError : ErrorMessage.NoPlayersToStealFrom;
                        case CardName.Beer:
                            return Players.Count() == 2 ? ErrorMessage.BeerFinalDuel : (player.Lives == player.MaxLives ? ErrorMessage.MaxLives : ErrorMessage.NoError);
                        case CardName.CatBalou:
                            return Players.Any(x => x.Cards.Count() > 0 && x.Id != player.Id) ? ErrorMessage.NoError : ErrorMessage.NoCardsToDiscard;
                        case CardName.Saloon:
                            return Players.All(x => x.Lives == x.MaxLives) ? ErrorMessage.EveryoneMaxLives : ErrorMessage.NoError;
                        case CardName.Barrel:
                        case CardName.Scope:
                        case CardName.Mustang:
                            return player.CardsOnTable.Any(x => x.Name == card.Name) ? ErrorMessage.AlreadyInUse : ErrorMessage.NoError;
                        default:
                            return ErrorMessage.NoError;
                    }
            }
        }

        private List<Card> DrawCards(Player p, int n)
        {
            var result = Dealer.DrawCards(n, p);
            var listofcards = result.Item1;
            var reshuffled = result.Item2;
            if (reshuffled == -1)
            {
                Tell($"You drew {string.Join(", ", listofcards.Select(x => x.GetDescription()))} from the deck.", p, textforothers: $"{p.Name} drew {listofcards.Count()} cards from the deck.");
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
                Tell(msgforp + ".", p, textforothers: msgforothers + ".");
            }
            return listofcards;
        }

        private Card Draw(Player player)
        {
            if (player.Character == Character.LuckyDuke)
            {
                Tell("You are Lucky Duke. You draw two cards, then choose one.", player, character: Character.LuckyDuke);
                //draw the cards, then send a menu to choose
                var cards = DrawCards(player, 2);
                Tell("Choose a card.", player);
                SendMessages(player, MakeMenuFromCards(cards, player));

                //tell people the two cards
                var cardchosen = WaitForChoice(player, 30).CardChosen ?? DefaultChoice.ChooseCardFrom(cards);
                var carddiscarded = cards.First(x => x != cardchosen);
                Tell($"You choose {cardchosen.GetDescription()} and discard {carddiscarded.GetDescription()}.", player, character: Character.LuckyDuke, textforothers: $"{player.Name} chose {cardchosen.GetDescription()} and discarded {carddiscarded.GetDescription()}.");
                
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
                Tell($"You drew {card.GetDescription()}" + (reshuffled ? ", then reshuffled the deck." : ""), player, card.Name, textforothers: $"{player.Name} drew {card.GetDescription()}" + (reshuffled ? ", then reshuffled the deck." : ""));
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
            if (p.Character == Character.SuzyLafayette && p.CardsInHand.Count() == 0)
            {
                DrawCards(p, 1);
                TellEveryone("", CardName.None, Character.SuzyLafayette);
            }
            return result;
        }
        
        private void HitPlayer(Player target, int lives, Player attacker = null)
        {
            target.AddLives(-lives);
            Tell($"You lose {lives} life points.", target, textforothers: $"{target.Name} loses {lives} life points.\n");

            if (target.Lives <= 0 && LethalHit(target))
                PlayerDies(target);
            else
            {
                switch (target.Character)
                {
                    case Character.BartCassidy:
                        DrawCards(target, lives);
                        TellEveryone("", CardName.None, Character.BartCassidy);
                        break;
                    case Character.ElGringo:
                        if (attacker != null)
                        {
                            var card = target.StealFrom(attacker);
                            var desc = card.GetDescription();
                            Tell($"You stole {desc} from {attacker.Name}'s hand.", target, card.Name);
                            Tell($"{target.Name} stole you {desc}", attacker, card.Name, Character.ElGringo);
                            TellEveryone($"{target.Name} stole a card from {attacker.Name}'s hand.", card.Name, Character.ElGringo, except: new[] { attacker, target });
                        }
                        break;
                    default:
                        break;
                }
            }
            SendMessages();
            return;
        }

        private bool LethalHit(Player target)
        {
            if (target.Lives > 0)
                throw new ArgumentException("Player is not lethally hit.");
            //check if they can be saved
            while ((target.CardsInHand.Count(x => x.Name == CardName.Beer) > -target.Lives && target.Lives < 1) || //they have enough beers
                (target.Character == Character.SidKetchum && CanUseAbility(target, Situation.PlayerDying))) //they are sid ketchum and have enough cards / beers.
            {
                List<InlineKeyboardCallbackButton[]> menu = null;
                if (target.Character == Character.SidKetchum)
                {
                    Tell($"You are dying! You have {target.Lives} life points. You can still use a beer, or use your ability (discard two cards), to regain a life point.\nChoose the card to use or discard.", target, CardName.Beer, Character.SidKetchum);
                    menu = MakeMenuFromCards(target.CardsInHand, target);
                }
                else
                {
                    Tell($"You are dying! You have {target.Lives} life points. You can still use a beer to regain a life point.\nSelect the beer.", target, CardName.Beer);
                    menu = MakeCardsInHandMenu(target, Situation.PlayerDying);
                }
                AddYesButton(menu, "Resign");
                SendMessages(target, menu);
                var choice = WaitForChoice(target, 30);
                if (choice == null || choice.ChoseYes == true)
                    break;
                else if (choice.CardChosen.Name == CardName.Beer)
                {
                    target.AddLives(1);
                    Discard(target, choice.CardChosen);
                    Tell($"You used {choice.CardChosen.GetDescription()}, and regained one life point.", target, choice.CardChosen.Name, textforothers: $"{target.Name} used {choice.CardChosen.GetDescription()}, and regained one life point!");
                    
                }
                else if (target.Character == Character.SidKetchum)
                //this should ALWAYS be sid ketchum...
                {
                    Discard(target, choice.CardChosen);
                    Tell($"You discarded {choice.CardChosen.GetDescription()}. Select another card to discard.", target, choice.CardChosen.Name);
                    menu = AddYesButton(MakeMenuFromCards(target.CardsInHand, target), "Resign");
                    SendMessage(target, menu);
                    var secondchoice = WaitForChoice(target, 30);
                    if (secondchoice == null || secondchoice.ChoseYes == true)
                        break;
                    else
                    {
                        Discard(target, secondchoice.CardChosen);
                        Tell(
                            $"You discarded {secondchoice.CardChosen.GetDescription()}, and regained a life point.",
                            target, choice.CardChosen.Name, Character.SidKetchum,
                            textforothers: $"{target.Name} discarded {choice.CardChosen.GetDescription()} and {secondchoice.CardChosen.GetDescription()}, and regained a life point!");
                        TellEveryone("", secondchoice.CardChosen.Name);
                        target.AddLives(1);
                    }
                }
                else
                    throw new IndexOutOfRangeException("Something not being taken in account...");
            }
            SendMessages();
            return target.Lives <= 0;
        }
        
        private void PlayerDies(Player target)
        {
            Tell($"You're out of lives! You died.", target, textforothers: $"{target.Name} died! {target.Name} was {target.Role.GetString<Role>()}");
            Players.Remove(target);
            DeadPlayers.Add(target);

            CheckForGameEnd(target);
            if (Status == GameStatus.Ending) return;

            var vulturesam = Players.FirstOrDefault(x => x.Character == Character.VultureSam);
            if (vulturesam != null)
            {
                foreach (var c in target.Cards)
                    vulturesam.StealFrom(target, c);
                Tell($"You take in hand all {target.Name}'s cards.", vulturesam, character: Character.VultureSam, textforothers: $"{vulturesam.Name} takes in hand all {target.Name}'s cards.");
            }
            else
            {
                TellEveryone($"{target.Name} discards all the cards: " + string.Join(", ", target.Cards.Select(x => x.GetDescription())));
                foreach (var c in target.Cards)
                    Discard(target, c);
            }
            SendMessages();
            return;
        }

        private void CheckForGameEnd(Player deadplayer)
        {
            var finalmsg = "";
            if (deadplayer.Role == Role.Sheriff && Players.Any(x => x.Role == Role.Outlaw))
                finalmsg = $"The Sheriff has died! The Outlaws " + string.Join(", ", Players.Where(x => x.Role == Role.Outlaw).Select(x => x.Name)) + " have won!";
            else if (deadplayer.Role == Role.Sheriff && Players.All(x => x.Role == Role.Renegade))
                finalmsg = $"The Sheriff has died! The Renegade {Players.First().Name} has won!";
            else if (!Players.Any(x => x.Role == Role.Outlaw))
                finalmsg = $"The Renegade and the Outlaws have died! The Sheriff and the Deputies have won, and the Renegade {Players.Union(DeadPlayers).First(x => x.Role == Role.Renegade).Name} has lost!";

            if (!String.IsNullOrEmpty(finalmsg))
            {
                Status = GameStatus.Ending;
                SendMessages();
                foreach (var p in Players)
                    Bot.Send(finalmsg, p.Id);
            }

            return;
        }
    }
}