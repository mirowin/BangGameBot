using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
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

        public static void Main() {
            Console.WriteLine("Successfully connected to @" + Bot.Me.Username);

            //handle updates
            Bot.Api.OnMessage += Bot_OnMessage;
            Bot.Api.OnCallbackQuery += Bot_OnCallbackQuery;
            Bot.Api.OnInlineQuery += Bot_OnInlineQuery;

            //handle errors

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler((s, e) => {
                Console.WriteLine("ERROR!");
                OnError(e.ExceptionObject);
            });
            Bot.Api.OnReceiveError += new EventHandler<ReceiveErrorEventArgs>((s, e) => OnError(e.ApiRequestException));
            Bot.Api.OnReceiveGeneralError += new EventHandler<ReceiveGeneralErrorEventArgs>((s, e) => OnError(e.Exception));

            //start receiving
            Bot.StartReceiving();
            new ManualResetEvent(false).WaitOne();
        }

        static void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            if (e.Message?.Date == null || e.Message.Date < StartTime.AddSeconds(-5))
                return;
            new Task(() => { Handler.HandleMessage(e.Message); }).Start();
            return;
        }

        static void Bot_OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            if (e.CallbackQuery?.Message?.Date == null || e.CallbackQuery.Message.Date < Program.StartTime.AddSeconds(-5))
            {
                Bot.Edit("This message has expired.", e.CallbackQuery.Message);
                return;
            }
            new Task(() => { Handler.HandleCallbackQuery(e.CallbackQuery); }).Start();
            return;
        }

        static void Bot_OnInlineQuery (object sender, InlineQueryEventArgs e)
        {
            new Task(() => { Handler.HandleInlineQuery(e.InlineQuery); }).Start();
            return;
        }

        
        public static void OnError (object o)
        {
            if (o is ApiRequestException apiex && apiex.Message == "Request timed out")
                return;

            if (o is Exception e)
            {
                var msg = "";
                var counter = 0;
                do
                {
                    var spaces = new String(' ', counter);
                    msg += spaces + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " - " + o.GetType().ToString() + " " + e.Source +
                        Environment.NewLine + spaces + e.Message +
                        Environment.NewLine + spaces + e.StackTrace +
                        Environment.NewLine + Environment.NewLine;
                    
                    e = e.InnerException;
                    counter++;
                } while (e != null);
                msg += Environment.NewLine +
                    "------------------------------------------------------------------------------------" +
                    Environment.NewLine + Environment.NewLine;
                System.IO.File.AppendAllText(LogPath, msg);
                try
                {
                    Bot.Send(msg, renyhp);
                }
                catch
                {
                    // ignored
                }
            }
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