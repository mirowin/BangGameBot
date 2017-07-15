using System.Collections.Generic;
using Telegram.Bot.Types.InlineQueryResults;

namespace BangGameBot
{
    static class Helpers
    {
        public static readonly Dictionary<ErrorMessage, string> ErrorMessages = new Dictionary<ErrorMessage, string>() {
            {ErrorMessage.NoPlayersToStealFrom, "There are no players to steal from!"}
        };

        public static readonly List<InlineQueryResultCachedPhoto> Cards = new List<InlineQueryResultCachedPhoto>() {
            //normal cards
            MakeInlineResult("AgADBAADqKkxGw_eSVN7g-gAAT0LfR7747wZAATVefIawihEpUw5AwABAg", "Bang!", "Remove one life point from a player at reachable distance."),
            MakeInlineResult("AgADBAADq6kxGw_eSVOQAxG7emk3FsOGpxkABFv2slmuEVuErbQDAAEC", "Beer", "Regain one life point. You can use this card also out of turn, only if you have just received a hit that is lethal.\nNote: You cannot gain more life points than your starting amount."),
            MakeInlineResult("AgADBAADr6kxGw_eSVPJWSHq0QHkziw_qRkABBH0Qj1tfBrnsbgDAAEC", "Cat Balou", "With this card, you make any player discard a card of your choice."),
            MakeInlineResult("AgADBAADsKkxGw_eSVOa2f2Ps8lu_9E-qRkABLfGKLyFQmUTdrEDAAEC", "Stagecoach", "Draw two cards from the deck."),
            MakeInlineResult("AgADBAADsqkxGw_eSVO6pJbke38JvKJkuxkABLzwUc2A_AVwlj0DAAEC", "Duel", "Challenge any other player. Each of you two, in turns, may discard a Bang! card. The one of you that can't, or doesn't want to, loses a life point."),
            MakeInlineResult("AgADBAADtKkxGw_eSVMxPqzNZET-CDO-mxkABLRT08CeUKO5_RsEAAEC", "General Store", "Reveal as many cards from the deck face up as the players. Starting with you, each player chooses one of those cards and takes it in hand."),
            MakeInlineResult("AgADBAADtqkxGw_eSVNU6vaFKoioPvpmqRkABEVbDdSQTUIB0bUDAAEC", "Gatling", "Shoot a Bang! to all the other players.\nNote: it is NOT considered as a Bang! card."),
            MakeInlineResult("AgADBAADt6kxGw_eSVPAT_AIi-LzPJvHvBkABAf1HtmQA0yW2ToDAAEC", "Indians!", "Each player, excluding the one who played this card, may discard a BANG! card, or lose one life point.\nNote: Missed! and Barrel have no effect in this case."),
            
            //permcards
            MakeInlineResult("AgADBAADqakxGw_eSVP11sEKFHKtEFVYqRkABO7VEFW2NSJbBLEDAAEC", "Barrel", "Allows you to “draw!” when you are the target of a Bang! or of a Gatling. If you draw a Heart card, you are Missed!, otherwise, you lose a life point."),
            MakeInlineResult("AgADBAADsakxGw_eSVPM5GA34ufB19lZqRkABJccV61a9ahbj7ADAAEC", "Dynamite", "At the beginning of your turn, you “draw!”: if the “drawn!” card is a 2-9 of spades, you lose 3 life points and discard the Dynamite, otherwise, pass the Dynamite to the next player."),
            
            //weapons
            MakeInlineResult("AgADBAADrqkxGw_eSVNIMQ8p0-Qm4KpKXhkABKDs9IRHxmxyMsgCAAEC", "Rev. Carabine", "This weapon increases your reachable distance to 4 (default is 1).\nNote: You can only have one weapon in play at a time."),
            
            //characters
            MakeInlineResult("AgADBAADqqkxGw_eSVOiUP_XsnKPc-puaRkABODvJnNtrC3f9eEBAAEC", "Bart Cassidy", "Each time he loses a life point, he immediately draws a card from the deck."),
            MakeInlineResult("AgADBAADrKkxGw_eSVNKhVOoPE6KD1OwuxkABF-akCQV9AyCjz0DAAEC", "Black Jack", "At the beginning of his turn, he must show the second card he draws: if it’s a Heart or Diamond, he draws one additional card (without revealing it)."),
            MakeInlineResult("AgADBAADrakxGw_eSVNYNi5mODn1VgMavRkABZty29NaL2ZcPgMAAQI", "Calamity Janet", "She can use Bang! cards as Missed! cards and vice versa.\nNote: If she plays a Missed! card as a Bang!, she cannot play another Bang! card that turn (unless a Volcanic is active)."),
            MakeInlineResult("AgADBAADs6kxGw_eSVMtfLFHRGgZlPUoqRkABPncLrs1KWSTZa8DAAEC", "El Gringo", "Each time he loses a life point due to a card played by another player, he draws a card from the hand of that player.\nNote: Dynamite damages are not caused by anyone."),
            MakeInlineResult("AgADBAADuKkxGw_eSVN5KSwkbCkuu904nRkABJ7I5yWPgqGi9doBAAEC", "Jesse Jones", "At the beginning of his turn, he may choose to draw the first card from the deck, from the hand of any other player; then he draws the second card from the deck."),
            MakeInlineResult("AgADBAADuakxGw_eSVM-lZRWie_k9p39qBkABJp6Bdmd71BDna8DAAEC", "Jourdounnais", "He is considered to have Barrel in play at all times.\nNote that he can still play another real Barrel card, and use both of them."),
            MakeInlineResult("AgADBAADuqkxGw_eSVNBzxjuwQUF_tP_vBkABBb1iaGOShsqUzgDAAEC", "Kit Carlson", "At the beginning of his turn, he looks at the top three cards of the deck: he chooses 2 to draw, and puts the other one back on the top of the deck, face down."),

            //roles
            MakeInlineResult("AgADBAADtakxGw_eSVPUl1xvda6db3FZqRkABMkTU9JmFYPUxrQDAAEC", "Outlaw", "His goal is to kill the sheriff.\nNote: Only the sheriff's role is revealed to everyone."),
            
            //TODO

        };

        private static InlineQueryResultCachedPhoto MakeInlineResult(string photoid, string name, string description)
        {
            var result = new InlineQueryResultCachedPhoto()
            {
                Id = name,
                Title = name,
                FileId = photoid,
                Description = description, //hoping someone will see it XD
                Caption = name + "\n\n" + description
            };
            if (result.Caption.Length > 200)
                throw new System.Exception($"{name} has a too long description ({description.Length} chars)");
            return result;
        }
    }
}
