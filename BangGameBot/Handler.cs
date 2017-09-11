using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineKeyboardButtons;

namespace BangGameBot
{
    public static class Handler
    {
        public static List<Game> Games = new List<Game>();

        public static void HandleMessage(Message msg)
        {
            var chatid = msg.Chat.Id;
            var userid = msg.From.Id;

            if (msg.Text == null || (!msg.Text.StartsWith("/") && !msg.Text.StartsWith("!")))
                return;


            //we received a command
            //get the command
            var text = msg.Text.Replace("@" + Bot.Me.Username, "").TrimStart('/', '!');
            var cmd = text.Contains(' ') ? text.Substring(0, text.IndexOf(' ')) : text;
            cmd.ToLower();
            text = text.Replace(cmd, "").Trim();

            if (cmd.StartsWith("help_"))
            {
                cmd = cmd.Replace("help_", "");
                if (Enum.TryParse(cmd, true, out CardName card))
                {
                    var description = Helpers.Cards.FirstOrDefault(x => x.EnumVal == (int)card && x.CardType == typeof(CardName));
                    Bot.Send(description.Name.ToBold() + "\n\n" + description.Description, chatid);
                }
                else if (Enum.TryParse(cmd, true, out Character character))
                {
                    var description = Helpers.Cards.FirstOrDefault(x => x.EnumVal == (int)character && x.CardType == typeof(Character));
                    Bot.Send(description.Name.ToBold() + "\n\n" + description.Description, chatid);
                }
                return;
            }

            string reply;
            switch (cmd)
            {
                case "start":
                    reply = "Hello! I can make you play games of Bang!, a card game by Emiliano Sciarra.\n\nSend /help for more information, or /newgame to start playing!";
                    Bot.Send(reply, chatid);
                    break;
                case "newgame":
                    if (msg.Chat.Type != ChatType.Private)
                    {
                        Bot.Send("Message me privately to use this command!", chatid, new InlineKeyboardUrlButton("Start me", $"t.me/{Bot.Me.Username}").ToSinglet().ToSinglet().ToKeyboard());
                        return;
                    }
                    //check if they are in a game
                    if (Games.Any(x => x.Users.Any(y => y.Id == userid && !y.HasLeftGame)))
                    {
                        Bot.Send("Already in a game. You can /leave it to start a new game.", chatid);
                        return;
                    }
                    //add them to a game
                    if (Games.Any(x => x.Status == GameStatus.Joining))
                        //there should be only one game joining at a time, but...
                        Games.Where(x => x.Status == GameStatus.Joining).OrderBy(x => x.Users.Count()).FirstOrDefault().PlayerRequest(new Player(msg.From), Request.Join);
                    else
                        //create new game
                        Games.Add(new Game(new Player(msg.From)));
                    return;
                case "leave":
                    if (Games.Any(x => x.Users.Any(y => y.Id == userid && !y.HasLeftGame)))
                    {
                        var menu = new[]
                        {
                                new InlineKeyboardCallbackButton("Leave", "game|leave"),
                                new InlineKeyboardCallbackButton("Cancel", "delete")
                            };
                        Bot.Send("Are you sure you want to leave the game? You won't be able to receive any message from it anymore.", chatid, menu.ToSinglet().ToKeyboard());
                    }
                    else
                        Bot.Send("You are not in a game.", chatid);
                    return;
                case "photoid":
                    if (userid != Program.renyhp)
                        return;
                    if (msg.ReplyToMessage?.Photo[0]?.FileId == null)
                        Bot.Send("You must reply to a photo", chatid);
                    Bot.Send(msg.ReplyToMessage?.Photo[0]?.FileId, chatid);
                    return;
                case "help":
                    reply = "You can find the complete official rules for Bang! <a href=\"http://www.dvgiochi.net/bang/bang_rules.pdf\">here</a>." +
                        "\nUse /helpMode to toggle the Help Mode." +
                        "\nAt any time, you can get info for any card by simply typing @BangGameBot and the card you're searching for.";
                    Bot.Api.SendTextMessageAsync(chatid, reply, ParseMode.Html);
                    return;
                case "helpmode":
                    Settings playersettings;
                    using (var db = new LiteDatabase(Program.LiteDBConnectionString))
                    {
                        var settings = db.GetCollection<Settings>("settings");
                        playersettings = settings.FindOne(x => x.TelegramId == userid);
                        if (playersettings == null)
                        {
                            playersettings = new Settings { TelegramId = userid, HelpMode = false };
                            settings.Insert(playersettings);
                        }
                        playersettings.HelpMode = !playersettings.HelpMode;
                        settings.Update(playersettings);
                    }
                    Bot.Send("Help mode turned " + (playersettings.HelpMode ? "on" : "off"), chatid);

                    //change helpmode in game         
                    var player = Games.FirstOrDefault(x => x.Users.Any(p => p.Id == userid && !p.HasLeftGame))?.Users.FirstOrDefault(x => x.Id == userid);
                    if (player != null)
                        player.HelpMode = playersettings.HelpMode;
                    return;
                default:
                    return;
            }
        }

        public static void HandleCallbackQuery(CallbackQuery q)
        {
            string errormessage = null;
            if (q.Data.StartsWith("err"))
            {
                errormessage = Helpers.ErrorMessages[(ErrorMessage)Enum.Parse(typeof(ErrorMessage), q.Data.Substring(3))];
                Bot.SendAlert(q, errormessage);
                return;
            }
            var chatid = q.Message.Chat.Id;
            var userid = q.From.Id;
            var args = q.Data.Split('|');
            if (args[0] == "game" && args.Length >= 2)
            {
                //they are in a game, it's a game command
                var game = Games.FirstOrDefault(x => x.Users.Any(p => p.Id == userid && !p.HasLeftGame));
                var player = game?.Users.FirstOrDefault(x => x.Id == userid);
                if (player == null)
                {
                    //remove the buttons and ignore.
                    Bot.EditMenu(null, q.Message);
                    return;
                }
                switch (args[1])
                {
                    case "start":
                        game.PlayerRequest(player, Request.VoteStart);
                        break;
                    case "leave":
                        if (game.Status != GameStatus.Joining)
                            game.LeaveGame(player, q);
                        else
                            game.PlayerRequest(player, Request.Leave, q);
                        break;
                    case "players":
                        game.SendPlayerList(player, args[2] == "new" ? null : q, args[2] == "expanded");
                        break;
                    case "playerinfo":
                        game.SendPlayerInfo(q, game.Users.FirstOrDefault(x => x.Id == long.Parse(args[2])), player);
                        break;
                    case "mycards":
                        game.ShowMyCards(q, player);
                        return; //this must not send the alert
                    default:
                        game.HandleChoice(player, args.Skip(1).ToArray(), q);
                        break;
                }
                Bot.SendAlert(q);
                return;
            }

            //not a game command
            switch (args[0])
            {
                case "help":
                    var character = args[1] == "character";
                    var card = Helpers.Cards.FirstOrDefault(x => x.CardType == (character ? typeof(Character) : typeof(CardName)) && x.EnumVal == int.Parse(args[2]));
                    Bot.SendAlert(q, card.Name + "\n" + card.Description);
                    break;
                case "delete":
                    Bot.Delete(q.Message);
                    return;
            }

        }

        public static void HandleInlineQuery(InlineQuery q)
        {
            Bot.Api.AnswerInlineQueryAsync(q.Id, Helpers.GetInlineResults().Where(x => x.Title.ToLower().Contains(q.Query.ToLower()) || x.Title.ToLower().ComputeLevenshtein(q.Query.ToLower()) < 3).ToArray());
            return;
        }
            

    }
}