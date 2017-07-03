using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace BangGameBot
{
    public static class Handler
    {
        public static List<Game> Games = new List<Game>();

        public static void HandleMessage (Message msg) {
            var chatid = msg.Chat.Id;
            var userid = msg.From.Id;
            if (msg.Text == null || msg.Chat.Type != ChatType.Private)
                return;
            if (msg.Text.StartsWith("/") || msg.Text.StartsWith("!")) {

                var text = msg.Text.Replace("@" + Bot.Me.Username, "").TrimStart('/', '!');
                var cmd = text.Contains(' ') ? text.Substring(0, text.IndexOf(' ')) : text;
                text = text.Replace(cmd, "").Trim();
                switch (cmd) {
                    case "start":
                        Bot.Send("Hello! I'm a test bot.", userid);
                        break;
                    case "newgame":
                        if (Games.Any(x => x.Players.Any(y => y.Id == userid))) {
                            Bot.Send("Already in a game", chatid);
                            return;
                        }
                        if (Games.Any(x => x.Status == GameStatus.Joining))
                            Games.OrderBy(x => x.Players.Count()).FirstOrDefault().AddPlayer(msg.From);
                        else
                            Games.Add(new Game(msg));
                        break;
                    default:
                        break;
                }
            }
        }

        public static void HandleCallbackQuery (CallbackQuery q) {
            Bot.Api.AnswerCallbackQueryAsync(q.Id);
            var chatid = q.Message.Chat.Id;
            var args = q.Data.Split ('|');
            int gameid = 0;
            if (int.TryParse(args[0], out gameid) && args.Length >= 2) {
                var game = Games.FirstOrDefault(x => x.Id == gameid);
                switch (args[1]) {
                    case "start":
                        game.VoteStart(q.From);
                        break;
                    case "leave":
                        game.PlayerLeave(q.From);
                        break;
                }
            }
        }


    }

    public enum GameStatus {
        Joining, Running, Ending
    }

}