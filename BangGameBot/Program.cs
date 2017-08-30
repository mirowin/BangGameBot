using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace BangGameBot
{
	static class Program
	{
        public static readonly Random R = new Random();
        public static readonly string TokenPath = @"D:\Git\BangGameBot\BangGameBot\bin\Debug\token.txt";
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
            Bot.StartReceiving();
            Thread.Sleep(-1);
        }

        static void Bot_OnInlineQuery (object sender, Telegram.Bot.Args.InlineQueryEventArgs e)
        {
            new Task(() => { Handler.HandleInlineQuery(e.InlineQuery); }).Start();
            return;
        }

        static void Bot_Api_OnReceiveGeneralError (object sender, Telegram.Bot.Args.ReceiveGeneralErrorEventArgs e)
        {
            if (!Bot.Api.IsReceiving)
                Bot.StartReceiving();
            LogError(e.Exception);
            return;
        }

        static void Bot_Api_OnReceiveError (object sender, Telegram.Bot.Args.ReceiveErrorEventArgs e)
        {
            if (!Bot.Api.IsReceiving)
                Bot.StartReceiving();
            if (!e.ApiRequestException.Message.Contains("timed out"))
                LogError(e.ApiRequestException);
            return;
        }

        static void Bot_OnCallbackQuery (object sender, Telegram.Bot.Args.CallbackQueryEventArgs e)
        {
            if (e.CallbackQuery?.Message?.Date == null || e.CallbackQuery.Message.Date < Program.StartTime.AddSeconds(-5)) {
                Bot.Edit("This message has expired.", e.CallbackQuery.Message);
                return;
            }
            new Task(() => { Handler.HandleCallbackQuery(e.CallbackQuery); }).Start();
            return;
        }

        static void Bot_OnMessage (object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            if (e.Message?.Date == null || e.Message.Date < StartTime.AddSeconds (-5))
                return;
            new Task(() => { Handler.HandleMessage(e.Message); }).Start();
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
        public static TelegramBotClient Api = new TelegramBotClient(System.IO.File.ReadAllText(Program.TokenPath)) { Timeout = TimeSpan.FromSeconds(15) };
        public static User Me = Api.GetMeAsync().Result;

        public static void StartReceiving()
        {
            Api.StartReceiving();
        }

        public static Task<Message> Send(string text, long chatid, IReplyMarkup replyMarkup = null, ParseMode parseMode = ParseMode.Html, int replyToMessageId = 0)
        {
            return Api.SendTextMessageAsync(chatid, text, parseMode, true, false, replyToMessageId, replyMarkup);
        }
        
        public static Task<Message> Edit(string newtext, Message msg, IReplyMarkup replyMarkup = null, ParseMode parseMode = ParseMode.Html, int replyToMessageId = 0) {
            return Api.EditMessageTextAsync(msg.Chat.Id, msg.MessageId, newtext, parseMode, true, replyMarkup);
        }

        public static Task<Message> EditMenu(IReplyMarkup replyMarkup, Message msg)
        {
            return Api.EditMessageReplyMarkupAsync(msg.Chat.Id, msg.MessageId, replyMarkup);
        }

        public static Task<bool> Delete(Message msg)
        {
            return Api.DeleteMessageAsync(msg.Chat.Id, msg.MessageId);
        }

        public static Task<bool> SendAlert(CallbackQuery query, string text = null)
        {
            return Api.AnswerCallbackQueryAsync(query.Id, text, true);
        }
    }
}