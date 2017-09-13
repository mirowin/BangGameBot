using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputMessageContents;
using Telegram.Bot.Types.ReplyMarkups;

namespace BangGameBot
{
    static class Program
	{
        public static readonly Random R = new Random();
        public static readonly string TokenPath = @"token.txt";
        public static readonly string LogPath = "errors.log";
        public static readonly long renyhp = 133748469;
        public static readonly DateTime StartTime = DateTime.UtcNow;
        public static readonly string LiteDBConnectionString = "BangDB.db";
        
        public static List<Game> Games = new List<Game>();

        public static void Main() {
            Console.Title = "Bang! @" + Bot.Me.Username;
            new Task(() => MonitorUpdater()).Start();

            //handle updates
            Bot.Api.OnMessage += Bot_OnMessage;
            Bot.Api.OnCallbackQuery += Bot_OnCallbackQuery;
            Bot.Api.OnInlineQuery += Bot_OnInlineQuery;

            //handle errors
            Bot.Api.OnReceiveError += new EventHandler<ReceiveErrorEventArgs>((s, e) => LogError(e.ApiRequestException, "ApiError"));
            Bot.Api.OnReceiveGeneralError += new EventHandler<ReceiveGeneralErrorEventArgs>((s, e) => LogError(e.Exception, "ApiGeneralError"));

            //start receiving
            Bot.StartReceiving();
            new ManualResetEvent(false).WaitOne();
        }

        
        static void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            var msg = e.Message;
            if (msg?.Date == null || msg.Date.ToUniversalTime() < StartTime.AddSeconds(-5))
                return;

            new Task(() =>
            {
                try
                {
                    Handler.HandleMessage(msg);
                }
                catch (Exception ex)
                {
                    Bot.Send("Oops, I have encountered an error.\n" + ex.Message, msg.Chat.Id);
                    LogError(ex);
                }
            }).Start();
            return;
        }

        static void Bot_OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            var q = e.CallbackQuery;
            if (q?.Message?.Date == null || q.Message.Date < StartTime.AddSeconds(-5))
            {
                Bot.Edit("This message has expired.", q.Message);
                return;
            }
            new Task(() => 
            {
                try
                {
                    Handler.HandleCallbackQuery(q);
                }
                catch (Exception ex)
                {
                    Bot.SendAlert(q, "Oops!\n" + ex.Message);
                    LogError(ex);
                }
            }).Start();
            return;
        }

        static void Bot_OnInlineQuery (object sender, InlineQueryEventArgs e)
        {
            new Task(() => 
            {
                try
                {
                    Handler.HandleInlineQuery(e.InlineQuery);
                }
                catch (Exception ex)
                {
                    Bot.Api.AnswerInlineQueryAsync(e.InlineQuery.Id, new InlineQueryResult() { Title = "Oops!", InputMessageContent = new InputTextMessageContent() { MessageText = ex.Message } }.ToSinglet());
                    LogError(ex);
                }
            }).Start();
            return;
        }

        
        public static void LogError (object o, string sender = "")
        {
            if (o is ApiRequestException apiex && apiex.Message == "Request timed out")
                return;
            
            if (o is Exception e)
            {
                var msg = "";
                var counter = 0;
                do
                {
                    var indents = new String('>', counter++);
                    msg += indents + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " - " + sender + ":  " + o.GetType().ToString() + " " + e.Source +
                        Environment.NewLine + indents + e.Message +
                        Environment.NewLine + indents + e.StackTrace +
                        Environment.NewLine + Environment.NewLine;
                    e = e.InnerException;
                } while (e != null);
                try
                {
                    Bot.Send(msg, renyhp, null, ParseMode.Default).Wait();
                }
                catch
                {
                    // ignored
                }

                msg += Environment.NewLine +
                    "------------------------------------------------------------------------------------" +
                    Environment.NewLine + Environment.NewLine;
                System.IO.File.AppendAllText(LogPath, msg);
            }
            return;
        }

        private static void MonitorUpdater()
        {
            while (true)
            {
                Console.Clear();
                Console.SetCursorPosition(0, 0);
                Console.WriteLine($"Bang! v{FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location)}");
                Console.WriteLine($"Current total games: {Games.Count}");
                Console.WriteLine($"Current running games: {Games.Count(x => x.Status != GameStatus.Joining)}");

                Task.Delay(30000).Wait();
            }
        }

    }


    static class Bot {
        public static TelegramBotClient Api = new TelegramBotClient(System.IO.File.ReadAllText(Program.TokenPath)) { Timeout = TimeSpan.FromSeconds(20) };
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