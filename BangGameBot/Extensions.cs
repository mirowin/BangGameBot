﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;

namespace BangGameBot
{
    public static class Extensions
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
            int n = list.Count;
            while (n > 1) {
                byte[] box = new byte[1];
                do
                    provider.GetBytes(box);
                while (!(box[0] < n * (Byte.MaxValue / n)));
                int k = (box[0] % n);
                n--;
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static Card ChooseCardFromHand(this Player p) {
            return p.CardsInHand.Random();
        }

        public static int DistanceSeen(this Player source, Player target, List<Player> players) {
            if (target.IsDead)
                return 0;
            if (source.Id == target.Id)
                return -1;
            var i = players.IndexOf(source);
            var j = players.IndexOf(target);
            //direct distance
            var dist1 = Math.Abs(j - i);
            //cycling distance
            var dist2 = players.Count() - Math.Max(i, j) + Math.Min(i, j);
            var distance = Math.Min(dist1, dist2);
            //take characters & cards into account!
            if (target.Character == Character.PaulRegret)
                distance++;
            if (target.CardsOnTable.Any(x => x.Name == CardName.Mustang))
                distance++;
            if (source.Character == Character.RoseDoolan)
                distance--;
            if (source.CardsOnTable.Any(x => x.Name == CardName.Scope))
                distance--;
            return distance < 1 ? 1 : distance;
        }

        public static bool IsReachableBy(this Player target, Player source, List<Player> players)
        {
            if (target.Id == source.Id)
                return false;
            int reachdistance = 1;
            switch (source.Weapon?.Name)
            {
                case CardName.Schofield:
                    reachdistance = 2;
                    break;
                case CardName.Remington:
                    reachdistance = 3;
                    break;
                case CardName.RevCarabine:
                    reachdistance = 4;
                    break;
                case CardName.Winchester:
                    reachdistance = 5;
                    break;
            }
            return source.DistanceSeen(target, players) <= reachdistance;
        }

        public static string GetString<T>(this object en)
        {
            var text = Enum.GetName(typeof(T), en);
            if (string.IsNullOrWhiteSpace(text))
                return "";
            StringBuilder newText = new StringBuilder(text.Length * 2);
            newText.Append(text[0]);
            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]) && text[i - 1] != ' ')
                    newText.Append(' ');
                newText.Append(text[i]);
            }
            return newText.ToString();
        }

        public static string ToBold(this string str)
        {
            return $"<b>{str.FormatHTML()}</b>";
        }

        public static string ToItalic(this string str)
        {
            return $"<i>{str.FormatHTML()}</i>";
        }

        public static string FormatHTML(this string str)
        {
            return str?.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        public static string ToEmoji(this int i)
        {
            if (i == -1)
                return "➡️";
            var emojis = new[] { "⏹", "1️⃣", "2️⃣", "3️⃣", "4️⃣", "5️⃣", "6️⃣", "7️⃣", "8️⃣", "9️⃣", "🔟" };
            return emojis[i];
        }

        public static string ToEmoji(this CardSuit s)
        {
            switch (s) {
                case CardSuit.Clubs:
                    return "♣️";
                case CardSuit.Diamonds:
                    return "♦️";
                case CardSuit.Hearts:
                    return "♥️";
                case CardSuit.Spades:
                    return "♠️";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static string LivesString(this Player p)
        {
            string r = "";
            for (var i = 0; i < p.Lives; i++) {
                r += "❤️";
            }
            if (p.IsDead)
                r = "💀 - " + p.Role.GetString<Role>();
            return r;
        }

        public static string GetDescription(this Card c) {
            var numberstring = "";
            switch (c.Number) {
                case 11:
                    numberstring = "J";
                    break;
                case 12:
                    numberstring = "Q";
                    break;
                case 13:
                    numberstring = "K";
                    break;
                case 14:
                    numberstring = "A";
                    break;
                default:
                    numberstring = c.Number.ToString();
                    break;
            }
            return c.Name.GetString<CardName>() + "[" + numberstring + c.Suit.ToEmoji() + "]";
        }

        public static List<InlineKeyboardCallbackButton[]> MakeMenu(this IEnumerable<Card> list, Player recipient)
        {
            var rows = new List<InlineKeyboardCallbackButton[]>();
            foreach (var c in list)
            {
                var button = new InlineKeyboardCallbackButton(c.GetDescription(), $"game|card|{c.Encode()}");
                rows.Add(recipient.HelpMode ? new[] { button, c.Name.ToHelpButton() } : button.ToSinglet());
            }
            return rows;
        }

        public static List<InlineKeyboardCallbackButton[]> AddYesButton(this List<InlineKeyboardCallbackButton[]> buttons, string str)
        {
            buttons.Add(new[] { new InlineKeyboardCallbackButton(str, $"game|bool|yes") });
            return buttons;
        }

        public static string Encode(this Card c) {
            return c.Id.ToString();
        }

        public static Card GetCard(this string str, Game g)
        {
            return g.Dealer.Deck.Union(g.Dealer.PeekedCards).Union(g.Users.SelectMany(x => x.Cards)).FirstOrDefault(x => x.Id == int.Parse(str));
        }

        public static T[] ToSinglet<T>(this T obj) {
            return new[] { obj };
        }

        /// <summary>
        /// Returns a random item from the list.
        /// </summary>
        public static T Random<T>(this IEnumerable<T> list) {
            return list.ElementAt(Program.R.Next(list.Count()));
        }

        public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int N)
        {
            return source.Skip(Math.Max(0, source.Count() - N));
        }

        public static InlineKeyboardMarkup ToKeyboard(this IEnumerable<InlineKeyboardCallbackButton[]> buttons)
        {
            return new InlineKeyboardMarkup(buttons.ToArray());
        }


        //thanks Para! :P
        public static int ComputeLevenshtein(this string s, string t)
        {
            if (string.IsNullOrEmpty(s))
            {
                if (string.IsNullOrEmpty(t))
                    return 0;
                return t.Length;
            }

            if (string.IsNullOrEmpty(t))
            {
                return s.Length;
            }

            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // initialize the top and right of the table to 0, 1, 2, ...
            for (int i = 0; i <= n; d[i, 0] = i++) ;
            for (int j = 1; j <= m; d[0, j] = j++) ;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    int min1 = d[i - 1, j] + 1;
                    int min2 = d[i, j - 1] + 1;
                    int min3 = d[i - 1, j - 1] + cost;
                    d[i, j] = Math.Min(Math.Min(min1, min2), min3);
                }
            }
            return d[n, m];
        }

        //thanks Para for this too :)
        public static List<InlineKeyboardCallbackButton[]> MakeTwoColumns(this List<InlineKeyboardCallbackButton> buttons)
        {
            var menu = new List<InlineKeyboardCallbackButton[]>();
            for (var i = 0; i < buttons.Count; i++)
            {
                if (buttons.Count - 1 == i)
                {
                    menu.Add(new[] { buttons[i] });
                }
                else
                    menu.Add(new[] { buttons[i], buttons[i + 1] });
                i++;
            }
            return menu;
        }


        public static bool IsHTMLEqualTo(this string html, string text)
        {
            return text == html.Replace("<b>", "")
                .Replace("<i>", "")
                .Replace("<code>", "")
                .Replace("</b>", "")
                .Replace("</i>", "")
                .Replace("</code>", "")
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&quot;", "\"");
        }

        public static InlineKeyboardCallbackButton ToHelpButton(this Character c, string text = "")
        {
            return new InlineKeyboardCallbackButton($"ℹ️{text}", $"help|character|{(int)c}");
        }

        public static InlineKeyboardCallbackButton ToHelpButton(this CardName c, string text = "")
        {
            return new InlineKeyboardCallbackButton($"ℹ️{text}", $"help|card|{(int)c}");
        }

        public static CardType GetCardType(this Card card)
        {
            switch (card.Name)
            {
                case CardName.Jail:
                case CardName.Dynamite:
                case CardName.Barrel:
                case CardName.Scope:
                case CardName.Mustang:
                    return CardType.PermCard;
                case CardName.Volcanic:
                case CardName.Schofield:
                case CardName.Remington:
                case CardName.RevCarabine:
                case CardName.Winchester:
                    return CardType.Weapon;
                default:
                    return CardType.Normal;
            }
        }

        public static T OnlyIf<T>(this T input, bool condition)
        {
            return condition ? input : new List<T>().FirstOrDefault();
        }

        public static Character OnlyIfMatches(this Character input, Player p)
        {
            return input.OnlyIf(p.Character == input);
        }
    }
}

