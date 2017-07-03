using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace BangGameBot
{
    public class Player
    {
        public long Id;
        public User TelegramUser;
        public Message JoinMsg;
        public bool VotedToStart;

        public Player (User u) {
            TelegramUser = u;
            Id = u.Id;
        }
    }
}

