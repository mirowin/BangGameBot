using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;

namespace BangGameBot
{
    public partial class Game
    {
        private Player _currentPlayer => Users[Turn];

        #region Initializers

        private void StartGame()
        {
            try
            {
                Status = GameStatus.Initialising;
                AssignRoles();
                AssignCharacters();
                DealCards();
                Players = new List<Player>(Users);
                while (Status != GameStatus.Ending)
                {
                    Status = GameStatus.PhaseZero;
                    Turn = (Turn + 1) % Users.Count();
                    ResetPlayers();
                    if (_currentPlayer.IsDead) continue;
                    SendPlayerList();
                    CheckDynamiteAndJail();
                    if (!_currentPlayer.CardsOnTable.Any(x => x.Name == CardName.Jail))
                    {
                        PhaseOne();
                        PhaseTwo();
                        PhaseThree();
                        if (Status != GameStatus.Ending)
                            SendMessages(); //do a last send and disable menus
                    }
                    else if (Status != GameStatus.Ending)
                        Discard(_currentPlayer, _currentPlayer.CardsOnTable.First(x => x.Name == CardName.Jail));
                }
            }
            catch (Exception e)
            {
                try
                {
                    foreach (var w in Watchers)
                        Bot.Send("OOPS! I'm very sorry. I had to cancel the game.\n\n" + e.Message, w.Id);
                }
                catch
                {
                    // ignored
                }
                Program.LogError(e);
            }
            finally
            {
                this.Dispose();
            }
            return;
        }
        
        private void AssignRoles()
        {
            var rolesToAssign = new List<Role>();
            var count = Users.Count();
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

            Users.Shuffle();
            Users.Shuffle();
            rolesToAssign.Shuffle();
            rolesToAssign.Shuffle();
            rolesToAssign.Shuffle();

            if (Users.Count() - 1 != rolesToAssign.Count())
                throw new Exception("Players count != roles to assign");

            //first one is sheriff
            Users[0].Role = Role.Sheriff;

            //assign others randomly
            for (var i = 1; i < count ; i++)
                Users[i].Role = rolesToAssign[i - 1];

            return;
        }

        private void AssignCharacters()
        {
            var charsToAssign = new List<Character>();
            charsToAssign.AddRange(Enum.GetValues(typeof(Character)).Cast<Character>().Where(x => x != Character.None).ToList());
            
            foreach (var p in Users)
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
            foreach (var p in Users)
                Dealer.DrawCards(p.Lives, p);
            return;
        }

        private void ResetPlayers()
        {
            foreach (var p in Players.Where(x => x.HasLeftGame && !x.IsDead))
                PlayerDies(p, null, true);
            if (Status == GameStatus.Ending) return;
            Players = new List<Player>(AlivePlayers);
            foreach (var p in Players)
            {
                p.UsedBang = false;
                p.Choice = null; //just to be sure
            }
        }

        #endregion

        #region Phases

        private void CheckDynamiteAndJail()
        {
            if (_currentPlayer.CardsOnTable.Any(x => x.Name == CardName.Dynamite))
            {
                TellEveryone($"{_currentPlayer.Name} has the Dynamite!", CardName.Dynamite);
                var dynamite = _currentPlayer.CardsOnTable.First(x => x.Name == CardName.Dynamite);
                var card = Draw(_currentPlayer);
                if (card.Number < 10 && card.Suit == CardSuit.Spades)
                {
                    TellEveryone("The dynamite explodes!", CardName.Dynamite);
                    HitPlayer(_currentPlayer, 3);
                    if (Status == GameStatus.Ending) return;

                    Discard(_currentPlayer, dynamite);
                }
                else
                {
                    Player nextplayer = Players[(Turn + 1) % Players.Count()];
                    TellEveryone($"The dynamite passes to {nextplayer.Name}.", CardName.Dynamite);
                    nextplayer.StealFrom(_currentPlayer, dynamite);
                    Dealer.PutPermCardOnTable(nextplayer, dynamite);
                }
            }
            if (_currentPlayer.CardsOnTable.Any(x => x.Name == CardName.Jail))
            {
                TellEveryone($"{_currentPlayer.Name} is in jail!", CardName.Jail);
                var jail = _currentPlayer.CardsOnTable.First(x => x.Name == CardName.Jail);
                var card = Draw(_currentPlayer);
                if (card.Suit == CardSuit.Hearts)
                {
                    TellEveryone($"The Jail is discarded and {_currentPlayer.Name} plays their turn.", CardName.Jail);
                    Discard(_currentPlayer, jail);
                }
                else
                {
                    TellEveryone($"{_currentPlayer.Name} skips this turn. The Jail is discarded.", CardName.Jail);
                    // the caller method will discard jail
                }
            }
            SendMessages();
        }

        private void PhaseOne()
        {
            if (Status == GameStatus.Ending) return;
            Status = GameStatus.PhaseOne;

            List<Card> cardsdrawn;
            switch (_currentPlayer.Character)
            {
                case Character.KitCarlson:
                    Tell("You are Kit Carlson. You draw 3 cards from the deck, then choose one to put back at the top of the deck.", _currentPlayer, character: Character.KitCarlson);
                    cardsdrawn = DrawCards(_currentPlayer, 3);
                    Tell("Choose the card to put back into the deck.", _currentPlayer);
                    AddToHelp(_currentPlayer, cardsdrawn);
                    SendMessages(_currentPlayer, cardsdrawn.MakeMenu(_currentPlayer));
                    var cardchosen = (WaitForChoice(_currentPlayer, GameSettings.AbilityPhaseOneTime)?.CardChosen ?? DefaultChoice.ChooseCardFrom(cardsdrawn));
                    Dealer.PutIntoDeck(_currentPlayer, cardchosen);
                    Tell($"You put {cardchosen.GetDescription()} back at the top of the deck.", _currentPlayer, CardName.None, Character.KitCarlson, textforothers: $"{_currentPlayer.Name} put a card back at the top the deck");
                    Tell("", _currentPlayer, cardchosen.Name);
                    break;
                case Character.BlackJack:
                    Tell("You are Black Jack. You show the second card you draw; on Hearts or Diamonds, you draw one more card.", _currentPlayer, character: Character.BlackJack);
                    var secondcard = DrawCards(_currentPlayer, 2)[1];
                    var heartsordiamonds = secondcard.Suit == CardSuit.Hearts || secondcard.Suit == CardSuit.Diamonds;
                    Tell($"The second card was {secondcard.Suit.ToEmoji()}, so you " + (heartsordiamonds ? "" : "can't ") + "draw another card", _currentPlayer, secondcard.Name, Character.BlackJack, $"{_currentPlayer.Name} drew {secondcard.GetDescription()}, so they " + (heartsordiamonds ? "" : "can't ") + "draw another card");
                    if (heartsordiamonds)
                        DrawCards(_currentPlayer, 1);
                    break;
                default:
                    //Jesse Jones & Pedro Ramirez can choose.
                    if ((_currentPlayer.Character == Character.JesseJones || _currentPlayer.Character == Character.PedroRamirez) && CanUseAbility(_currentPlayer))
                    {
                        //ask them if they want to use the ability
                        Tell((
                            _currentPlayer.Character == Character.JesseJones ?
                                "You are Jesse Jones: you can draw your first card from the hand of a player." :
                                $"You are Pedro Ramirez: you can draw your first card from the top of the graveyard. ({Dealer.Graveyard.Last().GetDescription()})") +
                            "\nDo you want to use your ability or do you want to draw from the deck?",
                            _currentPlayer, _currentPlayer.Character == Character.JesseJones ? CardName.None : Dealer.Graveyard.Last().Name, _currentPlayer.Character);
                        SendMessages(_currentPlayer, MakeBoolMenu("Use ability", "Draw from deck"));

                        //now let's see what they chose
                        if (WaitForChoice(_currentPlayer, GameSettings.AbilityPhaseOneTime)?.ChoseYes ?? DefaultChoice.UseAbilityPhaseOne)
                        {
                            if (_currentPlayer.Character == Character.JesseJones)
                                UsePanicOrCatBalou(_currentPlayer); //steal from a player
                            else //pedro ramirez
                            {
                                var card = Dealer.DrawFromGraveyard(_currentPlayer);
                                var desc = card.GetDescription();
                                Tell($"You drew {desc} from the graveyard", _currentPlayer, card.Name, Character.PedroRamirez, textforothers: $"{_currentPlayer.Name} drew {desc} from the graveyard");
                            }
                            DrawCards(_currentPlayer, 1);
                            break;
                        }
                        //if they chose no, exit from the if block and behave like other players (draw 2 cards)
                    }
                    DrawCards(_currentPlayer, 2);
                    break;
            }
            SendMessages();
        }

        private void PhaseTwo()
        {
            if (Status == GameStatus.Ending) return;
            Status = GameStatus.PhaseTwo;

            bool firsttime = true;
            while (_currentPlayer.CardsInHand.Count() > 0)
            {
                //ask them what they want to do
                Tell("Select the card you want to use.", _currentPlayer);
                var menu = MakeCardsInHandMenu(_currentPlayer, Situation.Standard).AddYesButton("Discard cards »");
                if (_currentPlayer.Character == Character.SidKetchum)
                {
                    //sid ketchum can discard two cards
                    if (firsttime)
                    {
                        Tell("At any time, you can use your ability to discard two cards and regain one life point.", _currentPlayer, character: Character.SidKetchum);
                        firsttime = false;
                    }
                    if (CanUseAbility(_currentPlayer))
                        menu.Add(new[] { new InlineKeyboardCallbackButton("Use ability", $"game|bool|no") });
                }
                SendMessages(_currentPlayer, menu);

                //see what they chose
                var choice = WaitForChoice(_currentPlayer, GameSettings.PhaseTwoTime);
                if (choice == null) //afk
                    return; 
                if (choice.ChoseYes == true) //discard cards
                    return;
                if (choice.ChoseYes == false)
                {
                    if (_currentPlayer.Character != Character.SidKetchum)
                        throw new Exception("Someone chose no during Phase Two...");
                    //it was sid ketchum! he wants to discard two cards and regain one life point.
                    Tell($"Choose the cards to discard.", _currentPlayer);
                    SendMessages(_currentPlayer, _currentPlayer.CardsInHand.MakeMenu(_currentPlayer));
                    var chosencard = WaitForChoice(_currentPlayer, GameSettings.SidKetchumAbilityTime)?.CardChosen ?? _currentPlayer.ChooseCardFromHand();
                    Discard(_currentPlayer, chosencard);
                    Tell($"You discarded {chosencard.GetDescription()}. Select another card to discard.", _currentPlayer, chosencard.Name);
                    SendMessages(_currentPlayer, _currentPlayer.CardsInHand.MakeMenu(_currentPlayer));
                    var secondchosencard = WaitForChoice(_currentPlayer, GameSettings.SidKetchumAbilityTime)?.CardChosen ?? _currentPlayer.ChooseCardFromHand();
                    Discard(_currentPlayer, secondchosencard);
                    Tell(
                        $"You discarded {secondchosencard.GetDescription()}, and regained a life point.",
                        _currentPlayer, chosencard.Name,
                        textforothers: $"{_currentPlayer.Name} discarded {chosencard.GetDescription()} and {secondchosencard.GetDescription()}, and regained a life point!");
                    TellEveryone("", secondchosencard.Name, Character.SidKetchum);
                    _currentPlayer.AddLives(1);
                    //SendMessages();
                    continue;
                }


                //get the card
                var cardchosen = choice.CardChosen;
                if (cardchosen.GetCardType() == CardType.PermCard || cardchosen.GetCardType() == CardType.Weapon)
                {
                    if (cardchosen.Name == CardName.Jail)
                    {
                        Tell($"You used {cardchosen.GetDescription()}", _currentPlayer, cardchosen.Name, textforothers: $"{_currentPlayer.Name} used {cardchosen.GetDescription()}.");
                        var possiblechoices = AlivePlayers.Where(x => x.Role != Role.Sheriff && !x.CardsOnTable.Any(c => c.Name == CardName.Jail) && x.Id != _currentPlayer.Id);
                        Player chosenplayer;
                        if (possiblechoices.Count() == 1)
                            chosenplayer = possiblechoices.First();
                        else
                        {
                            Tell($"Choose a player to put in jail.", _currentPlayer, CardName.Jail);
                            SendMessages(_currentPlayer, possiblechoices.Select(x => new[] { new InlineKeyboardCallbackButton(x.Name, $"game|player|{x.Id}") }));
                            chosenplayer = WaitForChoice(_currentPlayer, GameSettings.ChooseJailTargetTime)?.PlayerChosen ?? possiblechoices.Random();
                        }
                        chosenplayer.StealFrom(_currentPlayer, cardchosen);
                        Dealer.PutPermCardOnTable(chosenplayer, cardchosen);
                        Tell($"You put {chosenplayer.Name} in jail!", _currentPlayer, CardName.Jail);
                        Tell($"{_currentPlayer.Name} put you in jail!", chosenplayer, CardName.Jail);
                        TellEveryone($"{_currentPlayer.Name} put {chosenplayer.Name} in jail!", CardName.Jail, except: new[] { chosenplayer, _currentPlayer });
                    }
                    else
                    {
                        var discardweapon = Dealer.PutPermCardOnTable(_currentPlayer, cardchosen);
                        var msg = "";
                        if (discardweapon != null)
                            msg = $", and discarded {discardweapon.GetDescription()}";
                        msg += ".";
                        Tell($"You put {cardchosen.GetDescription()} in play" + msg, _currentPlayer, cardchosen.Name, textforothers: $"{_currentPlayer.Name} put {cardchosen.GetDescription()} in play" + msg);
                    }
                    //SendMessages();
                    continue;
                }

                Tell($"You used {cardchosen.GetDescription()}", _currentPlayer, cardchosen.Name, textforothers: $"{_currentPlayer.Name} used {cardchosen.GetDescription()}.");
                Discard(_currentPlayer, cardchosen);
                switch (cardchosen.Name)
                {
                    case CardName.Bang:
                        UseBang(_currentPlayer);
                        break;
                    case CardName.Missed:
                        if (_currentPlayer.Character != Character.CalamityJanet)
                            throw new Exception("Someone is using Missed! during their turn!");
                        TellEveryone(character: Character.CalamityJanet);
                        UseBang(_currentPlayer);
                        break;
                    case CardName.Beer:
                        if (AlivePlayers.Count() == 2)
                            throw new Exception("Someone is using a Beer in the final duel!");
                        _currentPlayer.AddLives(1);
                        Tell($"You regained one life point.", _currentPlayer, CardName.Beer, textforothers: $"{_currentPlayer.Name} regained one life point.");
                        break;
                    case CardName.CatBalou:
                    case CardName.Panic:
                        UsePanicOrCatBalou(_currentPlayer, cardchosen.Name == CardName.CatBalou);
                        break;
                    case CardName.Duel:
                        UseDuel(_currentPlayer);
                        break;
                    case CardName.Gatling:
                        TellEveryone($"{_currentPlayer.Name} shot everyone!", CardName.Gatling, except: _currentPlayer.ToSinglet());
                        var candiscard = AlivePlayers.Where(x => x.Id != _currentPlayer.Id && //not curplayer
                            x.Character != Character.Jourdounnais && !x.CardsOnTable.Any(c => c.Name == CardName.Barrel) && //no barrel
                            (x.CardsInHand.Any(c => c.Name == CardName.Missed) || (x.Character == Character.CalamityJanet && x.CardsInHand.Any(c => c.Name == CardName.Bang))) // have a missed! card
                        ); //these can only discard a missed card, and can do that async'ly. the others will choose if use barrel, or missed, or lose a life point

                        foreach (var p in candiscard)
                        {
                            Tell("You have a Missed! card! You may use it to miss the shot, or lose a life point.", p, character: Character.CalamityJanet.OnlyIfMatches(_currentPlayer));
                            SendMessages(p, MakeCardsInHandMenu(p, Situation.PlayerShot).AddYesButton("Lose a life point"));
                        }
                        var tasks = new List<Task>();
                        foreach (var p in candiscard)
                        {
                            var task = new Task(() => {
                                var discarded = WaitForChoice(p, GameSettings.MissGatlingTime)?.CardChosen;
                                Tell("You chose to " + (discarded != null ? $"use {discarded.GetDescription()}." : "lose a life point.") + "\nWaiting for the other players to choose...", p);
                                SendMessage(p);
                            });
                            tasks.Add(task);
                            task.Start();
                        }
                        while (tasks.Any(x => !x.IsCompleted))
                            Task.Delay(1000).Wait();

                        for (var i = 1; i < Players.Count(); i++)
                        {
                            var target = Players[(i + Turn) % Players.Count()];
                            if (target.IsDead) continue;
                            var discarded = target.Choice?.CardChosen;
                            if (candiscard.Contains(target) && discarded != null)
                            {
                                Tell($"You played {discarded.GetDescription()}.", target, character: Character.CalamityJanet.OnlyIfMatches(target), textforothers: $"{target.Name} played {discarded.GetDescription()}.");
                                Discard(target, discarded);
                            }
                            else if ((candiscard.Contains(target) && (target.Choice?.ChoseYes ?? DefaultChoice.LoseLifePoint)) || !Missed(_currentPlayer, target, true))
                            {
                                HitPlayer(target, 1, _currentPlayer);
                                if (Status == GameStatus.Ending) return;
                            }
                        }
                        break;
                    case CardName.GeneralStore:
                        var reshuffled = Dealer.PeekCards(AlivePlayers.Count()).Item2;
                        TellEveryone(Dealer.PeekedCards.Aggregate($"{_currentPlayer.Name} draws the following cards from the deck" + (reshuffled ? " reshuffling it" : " ") + ":\n", (s, c) => s + c.GetDescription() + "\n"));
                        AddToHelp(Dealer.PeekedCards);
                        SendMessages();
                        for (var i = 0; i < Players.Count(); i++)
                        {
                            var player = Players[(i + Turn) % Players.Count()];
                            if (player.IsDead) continue;
                            Tell("Choose the card to take in hand.", player);
                            SendMessages(player, Dealer.PeekedCards.MakeMenu(player));
                            var chosencard = WaitForChoice(player, GameSettings.GeneralStoreTime)?.CardChosen ?? DefaultChoice.ChooseCardFrom(Dealer.PeekedCards);
                            Tell($"You take {chosencard.GetDescription()} in hand.", player, textforothers: $"{player.Name} took {chosencard.GetDescription()} in hand");
                            Dealer.DrawFromPeeked(player, chosencard);
                        }
                        if (Dealer.PeekedCards.Any())
                        {
                            TellEveryone("Some players left, so " + string.Join(", ", Dealer.PeekedCards.Select(x => x.GetDescription())) + " were discarded.");
                            AddToHelp(Dealer.PeekedCards);
                            Dealer.DiscardPeekedCards();
                        }
                        break;
                    case CardName.Indians:
                        UseIndians(_currentPlayer);
                        break;
                    case CardName.Saloon:
                        foreach (var p in AlivePlayers)
                            p.AddLives(1);
                        TellEveryone("Everyone regained a life point.", CardName.Saloon);
                        break;
                    case CardName.Stagecoach:
                        DrawCards(_currentPlayer, 2);
                        break;
                    case CardName.WellsFargo:
                        DrawCards(_currentPlayer, 3);
                        break;
                }
                SendMessages();
            }
            return;
        }

        private void PhaseThree()
        {
            if (Status == GameStatus.Ending) return;
            Status = GameStatus.PhaseThree;

            bool firsttime = true;
            var discarded = 0;
            while (true)
            {
                var msg = "";
                var discard = _currentPlayer.CardsInHand.Count() > _currentPlayer.Lives; //do they have to discard?

                if (firsttime)
                {
                    if (discard)
                        //tell how many cards they have to discard
                        msg = "You need to discard at least " + (_currentPlayer.CardsInHand.Count() - _currentPlayer.Lives).ToString() + " cards.\n";
                    Tell(msg + "Select the cards you want to discard.", _currentPlayer);
                }
                //send the menu
                var menu = _currentPlayer.CardsInHand.MakeMenu(_currentPlayer);
                if (_currentPlayer.CardsInHand.Count() <= _currentPlayer.Lives)
                    menu = menu.AddYesButton("End of turn");
                SendMessages(_currentPlayer, menu);
                var choice = WaitForChoice(_currentPlayer, GameSettings.PhaseThreeTime);
                //yes = end of turn
                if (choice?.ChoseYes ?? false)
                    break;
                var cardchosen = choice?.CardChosen ?? DefaultChoice.ChooseCard;
                if (cardchosen != null || discard) //even if they are afk they need to discard anyway
                {
                    var card = Discard(_currentPlayer, cardchosen);
                    var desc = card.GetDescription();
                    Tell($"You discarded {desc}", _currentPlayer, card.Name, textforothers: $"{_currentPlayer.Name} discarded {desc}");
                }
                else
                    break;

                firsttime = false;
                discarded++;

                //sid ketchum can regain a life by discarding two cards
                if (_currentPlayer.Character == Character.SidKetchum && discarded % 2 == 0 && _currentPlayer.Lives < _currentPlayer.MaxLives)
                {
                    Tell("You discarded two cards. Do you want to use your ability and regain one life point?", _currentPlayer, character: Character.SidKetchum);
                    SendMessages(_currentPlayer, MakeBoolMenu("Yes", "No"));
                    if (WaitForChoice(_currentPlayer, GameSettings.SidKetchumAbilityPhaseThreeTime)?.ChoseYes ?? DefaultChoice.UseAblityPhaseThree)
                        _currentPlayer.AddLives(1);
                }
            }
            return;
        }

        #endregion

        #region Bang! & Missed

        private void UseBang(Player attacker)
        {
            if (attacker.UsedBang && attacker.Character == Character.WillyTheKid)
                TellEveryone(character: Character.WillyTheKid);
            attacker.UsedBang = true;
            var possiblechoices = AlivePlayers.Where(x => x.IsReachableBy(attacker, AlivePlayers));
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
                target = WaitForChoice(attacker, GameSettings.ChooseBangTargetTime)?.PlayerChosen ?? DefaultChoice.ChoosePlayer(possiblechoices);
                Tell($"You chose to shoot {target.Name}.", attacker);
            }

            TellEveryone($"{attacker.Name} shot {target.Name}!", character: Character.SlabTheKiller.OnlyIfMatches(attacker), except: new[] { target, attacker });
            Tell($"You were shot by {attacker.Name}!" + (attacker.Character == Character.SlabTheKiller ? " You'll need two Missed! cards to miss his Bang!" : ""), target, character: Character.SlabTheKiller.OnlyIfMatches(attacker));

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
            int counter = 0;
            do
            {
                if (target.CardsInHand.Any(x => x.Name == CardName.Missed) || (target.Character == Character.CalamityJanet && target.CardsInHand.Any(x => x.Name == CardName.Bang)))  //target has a missed card
                {
                    Tell("You have a Missed! card. You have the possibility to miss the shoot!", target, character: Character.CalamityJanet.OnlyIfMatches(target));
                    SendMessages(target, MakeCardsInHandMenu(target, Situation.PlayerShot).AddYesButton("Lose a life point"));
                    var choice = WaitForChoice(target, GameSettings.MissBangTime);
                    var cardchosen = choice?.CardChosen;
                    if (cardchosen != null)
                    {
                        if (cardchosen.Name != CardName.Missed && (cardchosen.Name != CardName.Bang || target.Character != Character.CalamityJanet))
                            throw new Exception("Target was supposed to miss the shoot.");
                        Tell($"You played {cardchosen.GetDescription()}.", target, character: Character.CalamityJanet.OnlyIfMatches(target), textforothers: $"{target.Name} played {cardchosen.GetDescription()}.");
                        Discard(target, cardchosen);
                        missed++;
                    }
                    else //if they don't discard a missed card, they either want to lose a life point, or... afk.
                        return false; //in that case, immediately return false.
                }
                if (CheckMissed(attacker, target, missed, isgatling))
                    return true;
            } while (!isgatling && attacker.Character == Character.SlabTheKiller && ++counter < 2); // executions comes here only if the hit was not missed yet.
                                                                                                    // if only one missed card was required (and wasn't played), !isgatling && attacker == slab is false, and "while" will exit from the cycle and return false.
                                                                                                    // if it's slab the killer, repeat again. if they don't play the second missed card, checkmissed is false, and ++counter<2 will make them exit from the cycle and return false.


            return false;
        }

        private bool UseBarrel(Player target, bool jourdounnais)
        {
            if (jourdounnais && target.Character != Character.Jourdounnais)
                throw new Exception("Someone is pretending to be Jourdounnais...");
            Tell(jourdounnais ?
                "You are Jourdounnais: you are considered to have a Barrel in play. Do you want to use this ability?" :
                "You have a Barrel in play. Do you want to use it?",
                target, CardName.Barrel, Character.Jourdounnais.OnlyIf(jourdounnais));

            var candefend = (jourdounnais && target.CardsOnTable.Any(x => x.Name == CardName.Barrel)) || //is jourdounnais and has a barrel OR
               target.CardsInHand.Any(x => x.Name == CardName.Missed) || //has a Missed! card
               (target.CardsInHand.Any(x => x.Name == CardName.Bang) && target.Character == Character.CalamityJanet); //is calamity janet and has a Bang! card

            SendMessages(target, MakeBoolMenu(jourdounnais ? "Use ability" : "Use Barrel", candefend ? "Defend otherwise" : "Lose a life point"));

            var choice = WaitForChoice(target, GameSettings.ChooseUseBarrelTime)?.ChoseYes ?? DefaultChoice.UseBarrel;
            if (!choice)
                return false;

            TellEveryone($"{target.Name} chose to use " + (jourdounnais ? "the ability." : "the Barrel."), CardName.Barrel, Character.Jourdounnais.OnlyIf(jourdounnais), target.ToSinglet());
            if (Draw(target).Suit == CardSuit.Hearts)
            {
                Tell("You drew a Heart! This counts as a Missed! card.", target, textforothers: $"{target.Name} drew a Heart! This counts as a Missed! card.");
                return true;
            }
            else
                return false;
        }

        private bool CheckMissed(Player attacker, Player target, int missed, bool isgatling)
        {
            if (missed == (attacker.Character == Character.SlabTheKiller && !isgatling ? 2 : 1))
            {
                Tell($"{target.Name} missed your shot!", attacker);
                Tell($"You missed {attacker.Name}'s shot!", target);
                TellEveryone($"{target.Name} missed {attacker.Name}'s shot!", character: attacker.Character == Character.SlabTheKiller ? Character.SlabTheKiller : Character.None, except: new[] { target, attacker });
                //SendMessages();
                return true;
            }
            else
                return false;
        }
        
        #endregion
        
        #region Other cards effect

        private void UseDuel(Player curplayer)
        {
            var possiblechoices = AlivePlayers.Where(x => x.Id != curplayer.Id);
            Player target;
            if (possiblechoices.Count() == 1)
            {
                target = possiblechoices.First();
            }
            else
            {
                Tell("Choose a player to challenge to duel.", curplayer, CardName.Duel);
                SendMessages(curplayer, possiblechoices.Select(x => (new[] { new InlineKeyboardCallbackButton(x.Name, $"game|player|{x.Id}") })));
                target = WaitForChoice(curplayer, GameSettings.ChooseDuelTargetTime)?.PlayerChosen ?? DefaultChoice.ChoosePlayer(possiblechoices);
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
                    SendMessages(player, MakeCardsInHandMenu(player, Situation.DiscardBang).AddYesButton("Lose a life point"));
                    WaitForChoice(player, GameSettings.MissDuelTime);
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
            var candiscard = AlivePlayers.Where(x => x.Id != curplayer.Id && (x.CardsInHand.Any(c => c.Name == CardName.Bang) || (x.Character == Character.CalamityJanet && x.CardsInHand.Any(c => c.Name == CardName.Missed))));
            foreach (var p in candiscard)
            {
                Tell("You have a Bang! card! You may discard it, or lose a life point.", p, character: curplayer.Character == Character.CalamityJanet ? Character.CalamityJanet : Character.None);
                SendMessages(p, MakeCardsInHandMenu(p, Situation.DiscardBang).AddYesButton("Lose a life point"));
            }
            var tasks = new List<Task>();
            foreach (var p in candiscard)
            {
                var task = new Task(() => {
                    var choice = WaitForChoice(p, GameSettings.MissIndiansTime)?.CardChosen;
                    Tell("You chose to " + (choice != null ? "discard a Bang! card." : "lose a life point.") + "\nWaiting for the other players to choose...", p);
                    SendMessage(p);
                });
                tasks.Add(task);
                task.Start();
            }
            while (tasks.Any(x => !x.IsCompleted))
                Task.Delay(1000).Wait();
            
            for (var i = 1; i < Players.Count(); i++)
            {
                var p = Players[(Turn + i) % Players.Count()];
                if (p.IsDead) continue;
                if (p.Choice?.CardChosen != null) // discarded
                {
                    if (p.Choice.CardChosen.Name != CardName.Bang && (p.Choice.CardChosen.Name != CardName.Missed || p.Character != Character.CalamityJanet))
                        throw new Exception("The player was meant to discard a Bang! card.");
                    Discard(p, p.Choice.CardChosen);
                    Tell($"You discarded {p.Choice.CardChosen.GetDescription()}.", p, p.Choice.CardChosen.Name, textforothers: $"{p.Name} discarded {p.Choice.CardChosen.GetDescription()}");
                }
                else
                {
                    HitPlayer(p, 1, curplayer);
                    if (Status == GameStatus.Ending) return;
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
                AlivePlayers.Where(x => x.Id != curplayer.Id && x.Cards.Count() > 0) :
                (jessejonesability ?
                    AlivePlayers.Where(x => x.Id != curplayer.Id && x.CardsInHand.Count() > 0) :
                    AlivePlayers.Where(x => x.Cards.Count() > 0 && curplayer.DistanceSeen(x, AlivePlayers) == 1)
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
                    buttonslist.Add(new[] { new InlineKeyboardCallbackButton($"({p.CardsInHand.Count()}) {p.Name}", $"game|player|{p.Id}") });
                SendMessages(curplayer, buttonslist);

                playerchosen = WaitForChoice(curplayer, GameSettings.ChoosePanicTargetTime)?.PlayerChosen ?? DefaultChoice.ChoosePlayer(possiblechoices);
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
                var buttonslist = playerchosen.CardsOnTable.MakeMenu(curplayer);
                if (playerchosen.CardsInHand.Count() > 0)
                    buttonslist = buttonslist.AddYesButton(iscatbalou ? "Discard from hand" : "Steal from hand");
                SendMessages(curplayer, buttonslist);

                //see what they chose
                var choice = WaitForChoice(curplayer, GameSettings.ChooseCardToStealTime);
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
                Tell(iscatbalou ? $"{curplayer.Name} discarded your {card}." : $"{curplayer.Name} stole you {card}", playerchosen, realcard.Name);
                TellEveryone(iscatbalou ? $"{curplayer.Name} discarded {card} from {playerchosen.Name}'s hand." : $"{curplayer.Name} stole a card from {playerchosen.Name}'s hand.", iscatbalou ? realcard.Name : CardName.None, except: new[] { curplayer, playerchosen });
            }
            else
            {
                //was from table
                Tell(iscatbalou ? $"You discarded {card} from {playerchosen.Name}" : $"You stole {card} from {playerchosen.Name}.", curplayer, realcard.Name, textforothers: iscatbalou ? $"{curplayer.Name} discarded {card} from {playerchosen.Name}." : $"{curplayer.Name} stole {card} from {playerchosen.Name}.");
            }
            //SendMessages();
            return;
        }

        #endregion

        #region Hits

        private void HitPlayer(Player target, int lives, Player attacker = null)
        {
            target.AddLives(-lives);
            Tell($"You lose {lives} life points.", target, textforothers: $"{target.Name} loses {lives} life points.\n");

            if (LethalHit(target))
                PlayerDies(target, attacker);
            else
            {
                switch (target.Character)
                {
                    case Character.BartCassidy:
                        DrawCards(target, lives);
                        TellEveryone(character: Character.BartCassidy);
                        break;
                    case Character.ElGringo:
                        if (attacker != null)
                        {
                            var card = target.StealFrom(attacker);
                            var desc = card.GetDescription();
                            Tell($"You stole {desc} from {attacker.Name}'s hand.", target, card.Name);
                            Tell($"{target.Name} stole you {desc}", attacker, card.Name, Character.ElGringo);
                            TellEveryone($"{target.Name} stole a card from {attacker.Name}'s hand.", CardName.None, Character.ElGringo, except: new[] { attacker, target });
                        }
                        break;
                    default:
                        break;
                }
            }
            //SendMessages();
            return;
        }

        private bool LethalHit(Player target)
        {
            //check if they can be saved
            while (
                target.Lives <= 0 && //lethal hit
                target.CardsInHand.Count(x => x.Name == CardName.Beer) > -target.Lives || //they have enough beers
                (target.Character == Character.SidKetchum && CanUseAbility(target, Situation.PlayerDying)) //they are sid ketchum and have enough cards / beers.
            ) 
            {
                List<InlineKeyboardCallbackButton[]> menu;
                if (target.Character == Character.SidKetchum)
                {
                    Tell($"You are dying! You have {target.Lives} life points. You can still use a beer, or use your ability (discard two cards), to regain a life point.\nChoose the card to use or discard.", target, CardName.Beer, Character.SidKetchum);
                    menu = target.CardsInHand.MakeMenu(target);
                }
                else
                {
                    Tell($"You are dying! You have {target.Lives} life points. You can still use a beer to regain a life point.\nSelect the beer.", target, CardName.Beer);
                    menu = MakeCardsInHandMenu(target, Situation.PlayerDying);
                }
                menu = menu.AddYesButton("Resign");
                SendMessages(target, menu);
                var choice = WaitForChoice(target, GameSettings.LethalHitTime);
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
                    menu = target.CardsInHand.MakeMenu(target).AddYesButton("Resign");
                    SendMessage(target, menu);
                    var secondchoice = WaitForChoice(target, GameSettings.SidKetchumLethalAbilityTime);
                    if (secondchoice == null || secondchoice.ChoseYes == true)
                        break;
                    else
                    {
                        Discard(target, secondchoice.CardChosen);
                        Tell(
                            $"You discarded {secondchoice.CardChosen.GetDescription()}, and regained a life point.",
                            target, character: Character.SidKetchum,
                            textforothers: $"{target.Name} discarded {choice.CardChosen.GetDescription()} and {secondchoice.CardChosen.GetDescription()}, and regained a life point!");
                        AddToHelp(new List<Card>() { choice.CardChosen, secondchoice.CardChosen });
                        target.AddLives(1);
                    }
                }
                else
                    throw new IndexOutOfRangeException("Something not being taken in account...");
            }
            SendMessages();
            return target.Lives <= 0;
        }

        private void PlayerDies(Player target, Player killer = null, bool left = false)
        {
            if (left)
                TellEveryone($"{target.Name} has left the game. {target.Name} was {target.Role.GetString<Role>()}");
            else
                Tell($"You're out of life points! You died.", target, textforothers: $"{target.Name} died! {target.Name} was {target.Role.GetString<Role>()}");
            
            target.IsDead = true;

            CheckForGameEnd(target);
            if (Status == GameStatus.Ending) return;

            if (killer != null)
            {
                if (target.Role == Role.Outlaw)
                {
                    TellEveryone($"{target.Name} was an Outlaw, so {killer.Name} draws three cards as a reward.");
                    DrawCards(killer, 3);
                }
                else if (killer.Role == Role.Sheriff && target.Role == Role.DepSheriff)
                {
                    TellEveryone($"Oh no! The Sheriff killed his Deputy! {killer.Name} discards all his cards: " + string.Join(", ", killer.Cards.Select(x => x.GetDescription())) + ".");
                    AddToHelp(killer.Cards);
                    Dealer.DiscardAll(killer);
                    if (killer.Character == Character.SuzyLafayette && killer.CardsInHand.Count() == 0)
                    {
                        DrawCards(killer, 1);
                        TellEveryone(character: Character.SuzyLafayette);
                    }
                }
            }
            
            var vulturesam = AlivePlayers.FirstOrDefault(x => x.Character == Character.VultureSam);
            if (vulturesam != null)
            {
                foreach (var c in target.Cards)
                    vulturesam.StealFrom(target, c);
                Tell($"You take in hand all {target.Name}'s cards.", vulturesam, character: Character.VultureSam, textforothers: $"{vulturesam.Name} takes in hand all {target.Name}'s cards.");
            }
            else
            {
                TellEveryone($"{target.Name} discards all the cards: " + string.Join(", ", target.Cards.Select(x => x.GetDescription())));
                Dealer.DiscardAll(target);
            }
            SendMessages();
            return;
        }

        private void CheckForGameEnd(Player deadplayer)
        {
            var finalmsg = "";
            if (deadplayer.Role == Role.Sheriff && AlivePlayers.Any(x => x.Role == Role.Outlaw))
                finalmsg = $"The Sheriff has died! The Outlaws " + string.Join(", ", Users.Where(x => x.Role == Role.Outlaw).Select(x => x.Name)) + " have won!";
            else if (AlivePlayers.All(x => x.Role == Role.Renegade))
                finalmsg = $"The Sheriff has died! The Renegade {AlivePlayers.First().Name} has won!";
            else if (!AlivePlayers.Any(x => x.Role == Role.Outlaw || x.Role == Role.Renegade))
                finalmsg = $"The Renegade and the Outlaws have died! The Sheriff and the Deputies have won, and the Renegade {Users.First(x => x.Role == Role.Renegade).Name} has lost!";

            if (!String.IsNullOrEmpty(finalmsg))
            {
                SendMessages();
                foreach (var p in Watchers)
                    Bot.Send(finalmsg, p.Id);
                Status = GameStatus.Ending; 
            }

            return;
        }

        #endregion

        #region Helpers

        private bool CanUseAbility(Player player, Situation s = Situation.Standard)
        {
            switch (player.Character)
            {
                case Character.JesseJones:
                    if (Status != GameStatus.PhaseOne)
                        throw new InvalidOperationException("Jesse Jones is using his ability while not in Phase One");
                    return AlivePlayers.Any(x => x.CardsInHand.Count() > 0);
                case Character.PedroRamirez:
                    if (Status != GameStatus.PhaseOne)
                        throw new InvalidOperationException("Pedro Ramirez is using his ability while not in Phase One");
                    return Dealer.Graveyard.Any();
                case Character.SidKetchum:
                    if (s == Situation.PlayerDying)
                        //regainable lives = beers count + other cards / 2 (2 non-beers = 1 life)
                        return player.CardsInHand.Count(x => x.Name == CardName.Beer) + player.CardsInHand.Count(x => x.Name != CardName.Beer) / 2 > -player.Lives;
                    
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
                            return (!player.UsedBang || player.Weapon?.Name == CardName.Volcanic || player.Character == Character.WillyTheKid) ? (AlivePlayers.Any(x => x.IsReachableBy(player, AlivePlayers)) ? ErrorMessage.NoError : ErrorMessage.NoReachablePlayers) : ErrorMessage.OnlyOneBang;
                        case CardName.Missed:
                            return player.Character == Character.CalamityJanet ? ErrorMessage.NoError : ErrorMessage.CantUseMissed;
                        case CardName.Jail:
                            return AlivePlayers.Any(x => x.Role != Role.Sheriff && !x.CardsOnTable.Any(c => c.Name == CardName.Jail) && x.Id != player.Id) ? ErrorMessage.NoError : ErrorMessage.NoPlayersToPutInJail;
                        case CardName.Panic:
                            return AlivePlayers.Any(x => x.Cards.Count() > 0 && player.DistanceSeen(x, AlivePlayers) == 1) ? ErrorMessage.NoError : ErrorMessage.NoPlayersToStealFrom;
                        case CardName.Beer:
                            return AlivePlayers.Count() == 2 ? ErrorMessage.BeerFinalDuel : (player.Lives == player.MaxLives ? ErrorMessage.MaxLives : ErrorMessage.NoError);
                        case CardName.CatBalou:
                            return AlivePlayers.Any(x => x.Cards.Count() > 0 && x.Id != player.Id) ? ErrorMessage.NoError : ErrorMessage.NoCardsToDiscard;
                        case CardName.Saloon:
                            return AlivePlayers.All(x => x.Lives == x.MaxLives) ? ErrorMessage.EveryoneMaxLives : ErrorMessage.NoError;
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
            foreach (var card in listofcards.Select(x => x.Name))
                Tell("", p, card);
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
                SendMessages(player, cards.MakeMenu(player));

                //tell people the two cards
                var cardchosen = WaitForChoice(player, GameSettings.LuckyDukeAbilityTime).CardChosen ?? DefaultChoice.ChooseCardFrom(cards);
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
                TellEveryone(character: Character.SuzyLafayette);
            }
            return result;
        }

        #endregion
    }
}