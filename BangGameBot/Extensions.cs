using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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

        
        public static List<InlineKeyboardCallbackButton[]> MakeMenu(this IEnumerable<Card> list, Player recipient)
        {
            var rows = new List<InlineKeyboardCallbackButton[]>();
            foreach (var c in list)
            {
                var button = new InlineKeyboardCallbackButton(c.GetButtonText(), $"game|card|{c.Encode()}");
                rows.Add(recipient.HelpMode ? new[] { button, c.Name.ToHelpButton() } : button.ToSinglet());
            }
            return rows;
        }

        public static List<InlineKeyboardCallbackButton[]> AddYesButton(this List<InlineKeyboardCallbackButton[]> buttons, string str, bool firstbutton = false)
        {
            var button = new[] { new InlineKeyboardCallbackButton(str, $"game|bool|yes") };
            if (firstbutton)
                buttons.Insert(0, button);
            else
                buttons.Add(button);
            return buttons;
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

        public static InlineKeyboardMarkup ToKeyboard(this IEnumerable<InlineKeyboardButton[]> buttons)
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

