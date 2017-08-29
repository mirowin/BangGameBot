using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace BangGameBot
{
    public static class Handler
    {
        public static List<Game> Games = new List<Game>();
        
        public static void HandleMessage (Message msg) {
#if !DEBUG
            try
#endif
            {
                var chatid = msg.Chat.Id;
                var userid = msg.From.Id;
                //for now don't do anything if not private
                if (msg.Text == null || msg.Chat.Type != ChatType.Private)
                    return;

                if (!msg.Text.StartsWith("/") && !msg.Text.StartsWith("!"))
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


                switch (cmd)
                {
                    case "start":
                        Bot.Send("Hello! I'm a test bot.", userid);
                        break;
                    case "newgame":
                        //check to see if they are in a game
                        if (Games.Any(x => x.Players.Any(y => y.Id == userid)))
                        {
                            Bot.Send("Already in a game", chatid);
                            return;
                        }
                        //add them to a game
                        if (Games.Any(x => x.Status == GameStatus.Joining))
                            //there should be only one game joining at a time, but...
                            Games.Where(x => x.Status == GameStatus.Joining).OrderBy(x => x.Players.Count()).FirstOrDefault().AddPlayer(msg.From);
                        else
                            //create new game
                            Games.Add(new Game(msg));
                        return;
                    case "photoid":
                        if (userid != Program.renyhp)
                            return;
                        if (msg.ReplyToMessage?.Photo[0]?.FileId == null)
                            Bot.Send("You must reply to a photo", chatid);
                        Bot.Send(msg.ReplyToMessage?.Photo[0]?.FileId, chatid);
                        return;
                    case "help":
                        var reply = "TBD. Complete rules: http://www.dvgiochi.net/bang/bang_rules.pdf" +
                            "\nUse /helpMode to toggle the Help Mode." +
                            "\nAt any time, you can get info for any card by simply typing @BangGameBot and the card you're searching for.";
                        Bot.Send(reply, chatid);
                        return;
                    case "helpmode":
                        using (var db = new LiteDatabase("BangDB.db"))
                        {
                            var settings = db.GetCollection<Settings>("settings");
                            var playersettings = settings.FindOne(x => x.TelegramId == userid);
                            if (playersettings == null)
                            {
                                playersettings = new Settings { TelegramId = userid, HelpMode = false };
                                settings.Insert(playersettings);
                            }
                            playersettings.HelpMode = !playersettings.HelpMode;
                            settings.Update(playersettings);
                            Bot.Send("Help mode turned " + (playersettings.HelpMode ? "on" : "off"), chatid);
                        }
                        return;
                    default:
                        return;
                }
                
            }
#if !DEBUG
            catch (Exception e)
            {
                try
                {
                    Bot.Send(e.Message, msg.From.Id);
                }
                catch { }
                Program.LogError(e);
            }
#endif
        }

        public static void HandleCallbackQuery (CallbackQuery q) {
#if !DEBUG
            try
#endif
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
                if (int.TryParse(args[0], out int gameid) && args.Length >= 2)
                {
                    //they are in a game, it's a game command
                    var game = Games.FirstOrDefault(x => x.Id == gameid);
                    var player = game?.Players.FirstOrDefault(x => x.Id == userid);
                    if (player == null) return;
                    if (!new[] { "mycards" }.Contains(args[1]))
                        Bot.SendAlert(q);
                    switch (args[1])
                    {
                        case "start":
                            game.VoteStart(player);
                            return;
                        case "leave":
                            game.PlayerLeave(player);
                            return;
                        case "players":
                            game.SendPlayerList(player, args[2] == "new" ? null : q);
                            return;
                        case "playerinfo":
                            game.SendPlayerInfo(q, game.Players.FirstOrDefault(x => x.Id == long.Parse(args[2])), player);
                            return;
                        case "mycards":
                            game.ShowMyCards(q, player);
                            return;
                        default:
                            game.HandleChoice(player, args.Skip(1).ToArray(), q);
                            return;
                    }
                }

                //not a game command
                switch (args[0])
                {
                    case "help":
                        var character = args[1] == "character";
                        var card = Helpers.Cards.FirstOrDefault(x => x.CardType == (character ? typeof(Character) : typeof(CardName)) && x.EnumVal == int.Parse(args[2]));
                        Bot.SendAlert(q, card.Name + "\n" + card.Description);
                        break;
                }
            }
#if !DEBUG
            catch (Exception e)
            {
                try
                {
                    Bot.Send(e.Message, q.From.Id);
                }
                catch { }
                Program.LogError(e);
            }
#endif
        }

        public static void HandleInlineQuery (InlineQuery q) {
#if !DEBUG
            try
#endif
            {
                Bot.Api.AnswerInlineQueryAsync(q.Id, Helpers.GetInlineResults().Where(x => x.Title.ToLower().Contains(q.Query.ToLower()) || x.Title.ToLower().ComputeLevenshtein(q.Query.ToLower()) < 3).ToArray());
                return;
            }
#if !DEBUG
            catch (Exception e)
            {
                try
                {
                    Bot.Send(e.Message, q.From.Id);
                }
                catch { }
                Program.LogError(e);
            }
#endif
        }

    }
}