using LiteDB;
using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineKeyboardButtons;

namespace BangGameBot
{
    public static class Handler
    {

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

            cmd = cmd.ToLower();
            string reply;
            InlineKeyboardButton[][] menu;
            Message result = null;
            switch (cmd)
            {
                case "start":
                    if (String.IsNullOrWhiteSpace(text))
                    {
                        reply = "Hello! I can make you play games of Bang!, a card game by Emiliano Sciarra.\n\nSend /help for more information, or /newgame to start playing!";
                        Bot.Send(reply, chatid);
                    }
                    else if (int.TryParse(text, out int id))
                    {
                        if (id != 1)
                        {
                            var game = Program.Games.FirstOrDefault(x => x.Status == GameStatus.Joining && x.Id == id);
                            if (game == null)
                                Bot.Send("This game has expired. Please start a /newgame", chatid);
                            else if (game.Users.Any(x => x.Id == userid))
                                Bot.Send("You are already in this game.", chatid);
                            else if ((Program.Games.FirstOrDefault(x => x.Users.Any(u => u.Id == userid && !u.HasLeftGame))?.Id ?? id) != id)
                                Bot.Send("You are already in a game. Please /leave the game to join this one.", chatid);
                            else
                            {
                                game.PlayerRequest(new Player(msg.From), Request.Join);
                                JoinBangTesting(msg.From);
                            }
                        }
                        else if (Program.Games.Any(x => x.Users.Any(y => y.Id == userid && !y.HasLeftGame)))
                        {
                            Bot.Send("You are already in a game. You can /leave it to start a new game.", chatid);
                            return;
                        }
                        else
                            NewPublicGame(msg.From);
                    }
                    break;
                case "help":
                    reply = "You can find the complete official rules for Bang! <a href=\"http://www.dvgiochi.net/bang/bang_rules.pdf\">here</a>." +
                        "\nUse /helpMode to toggle the Help Mode." +
                        "\nAt any time, you can get info for any card by simply typing @BangGameBot and the card you're searching for.";
                    Bot.Api.SendTextMessageAsync(chatid, reply, ParseMode.Html);
                    return;
                case "ping":
                    var ping = DateTime.Now - msg.Date;
                    var sendtime = DateTime.Now;
                    reply = "Time to receive your message: " + ping.ToString(@"mm\:ss\.fff");
                    result = Bot.Send(reply, chatid).Result;
                    ping = DateTime.Now - sendtime;
                    reply += Environment.NewLine + "Time to send this message: " + ping.ToString(@"mm\:ss\.fff");
                    Bot.Edit(reply, result);
                    break;
                case "helpmode":
                    PlayerSettings playersettings;
                    using (var db = new LiteDatabase(Program.LiteDBConnectionString))
                    {
                        var settings = db.GetCollection<PlayerSettings>("settings");
                        playersettings = settings.FindOne(x => x.TelegramId == userid);
                        if (playersettings == null)
                        {
                            playersettings = new PlayerSettings { TelegramId = userid, HelpMode = false };
                            settings.Insert(playersettings);
                        }
                        playersettings.HelpMode = !playersettings.HelpMode;
                        settings.Update(playersettings);
                    }
                    Bot.Send("Help mode turned " + (playersettings.HelpMode ? "on" : "off"), chatid);

                    //change helpmode in game         
                    var player = Program.Games.FirstOrDefault(x => x.Users.Any(p => p.Id == userid && !p.HasLeftGame))?.Users.FirstOrDefault(x => x.Id == userid);
                    if (player != null)
                        player.HelpMode = playersettings.HelpMode;
                    return;
                case "newgame":
                    //check if they are in a game
                    if (Program.Games.Any(x => x.Users.Any(y => y.Id == userid && !y.HasLeftGame)))
                    {
                        Bot.Send("You are already in a game. You can /leave it to start a new game.", chatid);
                        return;
                    }
                    if (Program.Maintenance)
                    {
                        Bot.Send("Sorry, the bot is being shut down for maintenance. Please retry in a few minutes.", chatid);
                        return;
                    }
                    if (msg.Chat.Type == ChatType.Private)
                        menu = new [] { new InlineKeyboardCallbackButton("Play with strangers", "newgame|public").ToSinglet(), new InlineKeyboardCallbackButton("Play with friends", "newgame|private").ToSinglet() };
                    else
                        menu = new InlineKeyboardButton[][] { new InlineKeyboardUrlButton("Play with strangers", $"t.me/{Bot.Me.Username}?start=1").ToSinglet(), new InlineKeyboardCallbackButton("Create a private game", "newgame|private").ToSinglet() };

                    Bot.Send("Let's play Bang!", chatid, menu.ToKeyboard());
                    return;
                case "leave":
                    if (Program.Games.Any(x => x.Users.Any(y => y.Id == userid && !y.HasLeftGame)))
                    {
                        menu = new[]
                            {
                                new InlineKeyboardCallbackButton("Leave", "game|leave"),
                                new InlineKeyboardCallbackButton("Cancel", "delete")
                            }.ToSinglet();
                        Bot.Send("Are you sure you want to leave the game? You won't be able to receive any message from it anymore.", chatid, menu.ToKeyboard());
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
                case "maintenance":
                    if (userid != Program.renyhp)
                        return;
                    Program.Maintenance = true;

                    var joining = Program.Games.Where(x => x.Status == GameStatus.Joining);
                    foreach (var uid in joining.SelectMany(x => x.Users).Select(x => x.Id))
                        Bot.Send("Sorry, we are entering maintenance mode. The game is being cancelled. Please retry in a few minutes.", uid);
                    foreach (var g in joining)
                        g.Dispose();

                    while (Program.Games.Count() > 0)
                    {
                        var games = Program.Games.Count();
                        reply = $"Maintenance mode turned on. Waiting for {games} games to end...";
                        if (result == null)
                            result = Bot.Send(reply, chatid).Result;
                        else
                            Bot.Edit(reply, result);
                        while (Program.Games.Count() == games)
                            Task.Delay(10000).Wait();
                    }
                    Bot.Send("No active games. The bot can be shut down.", chatid);
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
                var game = Program.Games.FirstOrDefault(x => x.Users.Any(p => p.Id == userid && !p.HasLeftGame));
                var player = game?.Users.FirstOrDefault(x => x.Id == userid);
                if (player == null)
                    //remove the buttons and ignore.
                    Bot.EditMenu(null, q.Message);
                else
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
                            game.SendPlayerList(player, args[2] == "new" ? null : q, args[2] == "new");
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
                case "newgame":
                    if (Program.Games.Any(x => x.Users.Any(y => y.Id == userid && !y.HasLeftGame)))
                    {
                        Bot.SendAlert(q, "You are already in a game. Please /leave the game to start a new one");
                        return;
                    }
                    if (Program.Maintenance)
                    {
                        Bot.SendAlert(q, "Sorry, the bot is being shut down for maintenance. Please retry in a few minutes.");
                        return;
                    }
                    if (args[1] == "public")
                    {
                        //add them to a game
                        Bot.Edit("Searching for a game...", q.Message).Wait();
                        NewPublicGame(q.From);
                    }
                    else if (args[1] == "private")
                    {
                        var game = new Game(null);
                        Program.Games.Add(game);
                        if (q.Message.Chat.Type == ChatType.Private)
                        {
                            Bot.Edit($"Share this link with your friends to let them join your game! http://t.me/{Bot.Me.Username}?start={game.Id}", q.Message).Wait();
                            game.PlayerRequest(new Player(q.From), Request.Join);
                            Task.Delay(500).Wait(); //wait for the request to be sent. i need the SendAlert to run AFTER this, otherwise telegram won't let me send the player list.
                        }
                        else
                            Bot.Send($"Share this link with your friends to let them join your game! http://t.me/{Bot.Me.Username}?start={game.Id}", q.Message.Chat.Id, new[] { new InlineKeyboardUrlButton("Join this game", $"http://t.me/{Bot.Me.Username}?start={game.Id}") }.ToSinglet().ToKeyboard()).Wait();
                    }

                    Bot.SendAlert(q);
                    return;
                case "help":
                    var character = args[1] == "character";
                    var card = Helpers.Cards.FirstOrDefault(x => x.CardType == (character ? typeof(Character) : typeof(CardName)) && x.EnumVal == int.Parse(args[2]));
                    Bot.SendAlert(q, card.Name + "\n" + card.Description);
                    return;
                case "delete":
                    Bot.Delete(q.Message).Wait();
                    Bot.SendAlert(q);
                    return;
            }

        }

        private static void NewPublicGame(User u)
        {
            if (Program.Games.Any(x => x.Status == GameStatus.Joining && x.Id == 1))
                //there should be only one game joining at a time, but...
                Program.Games.Where(x => x.Status == GameStatus.Joining).OrderBy(x => x.Users.Count()).FirstOrDefault().PlayerRequest(new Player(u), Request.Join);
            else
                //create new game
                Program.Games.Add(new Game(new Player(u)));
            JoinBangTesting(u);
        }

        private static void JoinBangTesting(User u)
        {
            if (new[] { ChatMemberStatus.Kicked, ChatMemberStatus.Left }.Contains(Bot.Api.GetChatMemberAsync(-1001126915338, u.Id).Result.Status))
                Bot.Send("Please join @bangtesting to help giving feedback about the bot.", u.Id);
        }

        public static void HandleInlineQuery(InlineQuery q)
        {
            Bot.Api.AnswerInlineQueryAsync(q.Id, Helpers.GetInlineResults().Where(x => x.Title.ToLower().Contains(q.Query.ToLower()) || x.Title.ToLower().ComputeLevenshtein(q.Query.ToLower()) < 3).ToArray());
            return;
        }
            

    }
}