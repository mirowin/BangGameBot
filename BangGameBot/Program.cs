using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace BangGameBot
{
	static class Program
	{
        public static readonly Random R = new Random();
        public static readonly string TokenPath = "token.txt";
        public static readonly string LogPath = "errors.log";
        public static readonly long renyhp = 133748469;
        public static readonly DateTime StartTime = DateTime.UtcNow;

        public static void Main () {
            Console.WriteLine("Successfully connected to @" + Bot.Me.Username);
            Bot.Api.OnMessage += Bot_OnMessage;
            Bot.Api.OnCallbackQuery += Bot_OnCallbackQuery;
            Bot.Api.OnReceiveError += Bot_Api_OnReceiveError;
            Bot.Api.OnReceiveGeneralError += Bot_Api_OnReceiveGeneralError;
            Bot.Api.OnInlineQuery += Bot_OnInlineQuery;
            Bot.Api.StartReceiving();
            Thread.Sleep(-1);
        }

        static void Bot_OnInlineQuery (object sender, Telegram.Bot.Args.InlineQueryEventArgs e)
        {
            new Task (() => {
                try {
                    Handler.HandleInlineQuery (e.InlineQuery);
                } catch (Exception ex) {
                    LogError (ex);
                }
            }).Start ();
            return;
        }

        static void Bot_Api_OnReceiveGeneralError (object sender, Telegram.Bot.Args.ReceiveGeneralErrorEventArgs e)
        {
            if (!Bot.Api.IsReceiving)
                Bot.Api.StartReceiving();
            LogError(e.Exception);
            return;
        }

        static void Bot_Api_OnReceiveError (object sender, Telegram.Bot.Args.ReceiveErrorEventArgs e)
        {
            if (!Bot.Api.IsReceiving)
                Bot.Api.StartReceiving();
            LogError(e.ApiRequestException);
            return;
        }

        static void Bot_OnCallbackQuery (object sender, Telegram.Bot.Args.CallbackQueryEventArgs e)
        {
            if (e.CallbackQuery?.Message?.Date == null || e.CallbackQuery.Message.Date < Program.StartTime.AddSeconds(-5)) {
                Bot.Edit("This message has expired.", e.CallbackQuery.Message);
                return;
            }
            new Task (() => {
                try {
                    Handler.HandleCallbackQuery (e.CallbackQuery);
                } catch (Exception ex) {
                    LogError (ex);
                }
            }).Start ();
            return;
        }

        static void Bot_OnMessage (object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            if (e.Message?.Date == null || e.Message.Date < Program.StartTime.AddSeconds (-5))
                return;
            new Task (() => {
                try {
                    Handler.HandleMessage (e.Message);
                } catch (Exception ex) {
                    LogError (ex);
                }
            }).Start ();
            return;
        }

        public static void LogError (Exception e)
        {
            var msg = "";
            do {
                msg = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " - " + e.GetType().ToString() + " " + e.Source +
                Environment.NewLine + e.Message +
                Environment.NewLine + e.StackTrace + Environment.NewLine + Environment.NewLine;
                System.IO.File.AppendAllText(LogPath, msg);
                try {
                    Bot.Send(msg, renyhp);
                } catch {
                    //ignored
                }
                e = e.InnerException;
            } while (e != null);
            return;
        }
    }


    static class Bot {
        public static TelegramBotClient Api = new TelegramBotClient(System.IO.File.ReadAllText(Program.TokenPath));
        public static User Me = Api.GetMeAsync().Result;

        public static Task<Message> Send(string text, long chatid, IReplyMarkup replyMarkup = null, ParseMode parseMode = ParseMode.Html, int replyToMessageId = 0) {
            return Api.SendTextMessageAsync(chatid, text, true, false, replyToMessageId, replyMarkup, parseMode);
        }

        public static Task<Message> Edit(string newtext, Message msg, IReplyMarkup replyMarkup = null, ParseMode parseMode = ParseMode.Html, int replyToMessageId = 0) {
            return Api.EditMessageTextAsync(msg.Chat.Id, msg.MessageId, newtext, parseMode, true, replyMarkup);
        }
    }
}