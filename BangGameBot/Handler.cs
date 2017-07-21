using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;

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

                if (msg.Text.StartsWith("/") || msg.Text.StartsWith("!"))
                {
                    //we received a command
                    //get the command
                    var text = msg.Text.Replace("@" + Bot.Me.Username, "").TrimStart('/', '!');
                    var cmd = text.Contains(' ') ? text.Substring(0, text.IndexOf(' ')) : text;
                    text = text.Replace(cmd, "").Trim();
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
                            break;
                        case "photoid":
                            if (userid != Program.renyhp)
                                return;
                            if (msg.ReplyToMessage?.Photo[0]?.FileId == null)
                                Bot.Send("You must reply to a photo", chatid);
                            Bot.Send(msg.ReplyToMessage?.Photo[0]?.FileId, chatid);
                            return;
                        default:
                            break;
                    }
                }
                else
                {
                    //they are trying to chat with other players
                    var game = Games.FirstOrDefault(x => x.Players.Any(p => p.Id == userid));
                    if (game != null)
                        game.SendMessage(msg.From, msg.Text);
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
                int gameid = 0;
                if (int.TryParse(args[0], out gameid) && args.Length >= 2)
                {
                    //they are in a game, it's a game command
                    var game = Games.FirstOrDefault(x => x.Id == gameid);
                    var player = game.Players.FirstOrDefault(x => x.Id == userid);
                    if (args[1] != "playerinfo")
                        Bot.SendAlert(q);
                    switch (args[1])
                    {
                        case "start":
                            game.VoteStart(player);
                            break;
                        case "leave":
                            game.PlayerLeave(player);
                            break;
                        case "playerinfo":
                            game.SendPlayerInfo(q, game.Players.FirstOrDefault(x => x.Id == long.Parse(args[2])), player);
                            break;
                        default:
                            game.HandleChoice(player, args.Skip(1).ToArray(), q);
                            break;
                    }
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
                Bot.Api.AnswerInlineQueryAsync(q.Id, Helpers.Cards.Where(x => x.Title.ToLower().Contains(q.Query.ToLower()) || x.Title.ToLower().ComputeLevenshtein(q.Query.ToLower()) < 3).ToArray());
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