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
            MakeInlineResult("AgADBAADqKkxGw_eSVN7g-gAAT0LfR7747wZAATVefIawihEpUw5AwABAg", "Bang!", "Remove one life point from a player at reachable distance.\nNote: You can play only one Bang! card per turn, unless you have a Volcanic in play."),
            MakeInlineResult("AgADBAADq6kxGw_eSVOQAxG7emk3FsOGpxkABFv2slmuEVuErbQDAAEC", "Beer", "Regain one life point. You can use this card also out of turn, only if you have just received a hit that is lethal.\nNote: You cannot gain more life points than your starting amount."),
            MakeInlineResult("AgADBAADr6kxGw_eSVPJWSHq0QHkziw_qRkABBH0Qj1tfBrnsbgDAAEC", "Cat Balou", "With this card, you make any player discard a card of your choice."),
            MakeInlineResult("AgADBAADsqkxGw_eSVO6pJbke38JvKJkuxkABLzwUc2A_AVwlj0DAAEC", "Duel", "Challenge any other player. Each of you two, in turns, may discard a Bang! card. The one of you that can't, or doesn't want to, loses a life point."),
            MakeInlineResult("AgADBAADtqkxGw_eSVNU6vaFKoioPvpmqRkABEVbDdSQTUIB0bUDAAEC", "Gatling", "Shoot a Bang! to all the other players.\nNote: it is NOT considered as a Bang! card."),
            MakeInlineResult("AgADBAADtKkxGw_eSVMxPqzNZET-CDO-mxkABLRT08CeUKO5_RsEAAEC", "General Store", "Reveal as many cards from the deck face up as the players. Starting with you, each player chooses one of those cards and takes it in hand."),
            MakeInlineResult("AgADBAADt6kxGw_eSVPAT_AIi-LzPJvHvBkABAf1HtmQA0yW2ToDAAEC", "Indians!", "Each player, excluding you, may discard a Bang! card, or lose one life point.\nNote: Missed! and Barrel have no effect in this case."),
            MakeInlineResult("AgADBAADvKkxGw_eSVNjoCTVYyXi7bykmxkABPYSpAnFGsEeCeEBAAEC", "Missed!", "You can play this card only when you're the target of a Bang! or Gatling, to cancel the shot and not lose a life point."),
            MakeInlineResult("AgADBAADv6kxGw_eSVNqN4dLDw2kcG4cvRkABHiGAAEQVxHuXbo5AwABAg", "Panic!", "Draw a card from a player which you see at distance 1."),
            MakeInlineResult("AgADBAADxqkxGw_eSVNJU7DOZ4FCHYravBkABEyrKe-_CbDJODsDAAEC", "Saloon", "Everyone regains a life point.\nNote: this is not a Beer card, so you can't play it if you received a lethal hit.\nNote: You cannot gain more life points than your starting amount."),
            MakeInlineResult("AgADBAADsKkxGw_eSVOa2f2Ps8lu_9E-qRkABLfGKLyFQmUTdrEDAAEC", "Stagecoach", "Draw two cards from the deck."),
            MakeInlineResult("AgADBAADz6kxGw_eSVO_TF7agq5ct9fPvBkABKqX4VkLPlr6rjwDAAEC", "Wells Fargo", "Draw three cards from the deck."),

            //permcards
            MakeInlineResult("AgADBAADqakxGw_eSVP11sEKFHKtEFVYqRkABO7VEFW2NSJbBLEDAAEC", "Barrel", "Allows you to “draw!” when you are the target of a Bang! or of a Gatling. If you draw a Heart card, you are Missed!, otherwise, you lose a life point."),
            MakeInlineResult("AgADBAADsakxGw_eSVPM5GA34ufB19lZqRkABJccV61a9ahbj7ADAAEC", "Dynamite", "At the beginning of your turn, you “draw!”: if the “drawn!” card is a 2-9 of spades, you lose 3 life points and discard the Dynamite, otherwise, pass the Dynamite to the next player."),
            MakeInlineResult("AgADBAADwqkxGw_eSVPTwDiSgmjomvXbvBkABBKdaEpaR5jUmzgDAAEC", "Jail", "Choose a player (any but the Sheriff!) to put in jail. If you are in jail, at the beginning of the turn you “draw!”: you play your turn only if the card is a Heart. The Jail is discarded anyway."),
            MakeInlineResult("AgADBAADvqkxGw_eSVN_ihOEeIeTtWRVqRkABCLj0mNcJSwFG7ADAAEC", "Mustang", "If you have this card in play, other players see you at a distance increased by 1. You still see other players at normal distance."),
            MakeInlineResult("AgADBAADvakxGw_eSVNTuF27mcEVYwUYvRkABL7Q3NeR6_m4Zz0DAAEC", "Scope", "If you have this card in play, you see others at a distance decreased by 1. Other players still see you at normal distance.\nNote: Distances less than 1 are considered to be 1."),

            //weapons
            MakeInlineResult("AgADBAADyKkxGw_eSVMg6pYqnVHxX5AcvRkABL679kCpTALrzzgDAAEC", "Schofield", "This weapon increases your reachable distance to 2 (default is 1).\nNote: You can only have one weapon in play at a time (if you have another one in play, it is discarded)."),
            MakeInlineResult("AgADBAADw6kxGw_eSVP4mSVWWh046Bb7qBkABPJYydekdGr2RawDAAEC", "Remington", "This weapon increases your reachable distance to 3 (default is 1).\nNote: You can only have one weapon in play at a time (if you have another one in play, it is discarded)."),
            MakeInlineResult("AgADBAADrqkxGw_eSVNIMQ8p0-Qm4KpKXhkABKDs9IRHxmxyMsgCAAEC", "Rev. Carabine", "This weapon increases your reachable distance to 4 (default is 1).\nNote: You can only have one weapon in play at a time (if you have another one in play, it is discarded)."),
            MakeInlineResult("AgADBAAD0akxGw_eSVMzrQjUwBwfi9CRpxkABBRPeZb2E9_fta4DAAEC", "Winchester", "This weapon increases your reachable distance to 5 (default is 1).\nNote: You can only have one weapon in play at a time (if you have another one in play, it is discarded)."),
            MakeInlineResult("AgADBAADzakxGw_eSVPrSHSrFpURespJuxkABPRZvWBAhYVWsTsDAAEC", "Volcanic", "This weapon lets you shoot as many Bang! cards as you want, but only at distance 1.\nNote: You can only have one weapon in play at a time (if you have another one in play, it is discarded)."),

            //characters
            MakeInlineResult("AgADBAADqqkxGw_eSVOiUP_XsnKPc-puaRkABODvJnNtrC3f9eEBAAEC", "Bart Cassidy", "Each time he loses a life point, he immediately draws a card from the deck."),
            MakeInlineResult("AgADBAADrKkxGw_eSVNKhVOoPE6KD1OwuxkABF-akCQV9AyCjz0DAAEC", "Black Jack", "At the beginning of his turn, he must show the second card he draws: if it’s a Heart or Diamond, he draws one additional card (without revealing it)."),
            MakeInlineResult("AgADBAADrakxGw_eSVNYNi5mODn1VgMavRkABZty29NaL2ZcPgMAAQI", "Calamity Janet", "She can use Bang! cards as Missed! cards and vice versa.\nNote: If she plays a Missed! card as a Bang!, she cannot play another Bang! card that turn (unless a Volcanic is active)."),
            MakeInlineResult("AgADBAADs6kxGw_eSVMtfLFHRGgZlPUoqRkABPncLrs1KWSTZa8DAAEC", "El Gringo", "Each time he loses a life point due to a card played by another player, he draws a card from the hand of that player.\nNote: Dynamite damages are not caused by anyone."),
            MakeInlineResult("AgADBAADuKkxGw_eSVN5KSwkbCkuu904nRkABJ7I5yWPgqGi9doBAAEC", "Jesse Jones", "At the beginning of his turn, he may choose to draw the first card from the deck, or from the hand of any other player; then he draws the second card from the deck."),
            MakeInlineResult("AgADBAADuakxGw_eSVM-lZRWie_k9p39qBkABJp6Bdmd71BDna8DAAEC", "Jourdounnais", "He is considered to have Barrel in play at all times.\nNote that he can still play another real Barrel card, and use both of them."),
            MakeInlineResult("AgADBAADuqkxGw_eSVNBzxjuwQUF_tP_vBkABBb1iaGOShsqUzgDAAEC", "Kit Carlson", "At the beginning of his turn, he looks at the top three cards of the deck: he chooses 2 to draw, and puts the other one back on the top of the deck, face down."),
            MakeInlineResult("AgADBAADu6kxGw_eSVPapbLPKgr4etXNvBkABFn5dE3XieMQmzwDAAEC", "Lucky Luke", "Each time he is required to “draw!”, he flips the top two cards from the deck, and chooses the result he prefers. Discard both cards afterward."),
            MakeInlineResult("AgADBAADwKkxGw_eSVMqJOr9U_sX8NhuaRkABA-rpi47MpA5N90BAAEC", "Paul Regret", "He is considered to have Mustang in play at all times.\nNote that he can still play another real Mustang card, and make others see him at distance increased by 2."),
            MakeInlineResult("AgADBAADwakxGw_eSVMPVKkDTGNfFN9UvRkABCNbcRVgNjjw2kEDAAEC", "Pedro Ramirez", "At the beginning of his turn, he may choose to draw the first card from the deck, or from the top of the graveyard; then he draws the second card from the deck."),
            MakeInlineResult("AgADBAADxakxGw_eSVNuECNl14WkHuIVvRkABOVYEOiQi7tVCD4DAAEC", "Rose Doolan", "She is considered to have Scope in play at all times.\nNote that she can still play another real Scope card, and see others at distance decreased by 2."),
            MakeInlineResult("AgADBAADyakxGw_eSVPL7mBomFAEYUzlvBkABGFI7j7fobMf-jsDAAEC", "Sid Ketchum", "At any time, he can discard 2 cards to regain a life point. This can also be used if he received a lethal hit.\nNote: Unlike Beer cards, this does have effect in the 1v1 duel."),
            MakeInlineResult("AgADBAADyqkxGw_eSVNblXUkQVEGpXrLvBkABAkwRniMNSx31DkDAAEC", "Slab the Killer", "Players trying to cancel his Bang! cards need to play 2 Missed! cards. The Barrel effect, if successfully used, only counts as one Missed! card."),
            MakeInlineResult("AgADBAADy6kxGw_eSVPbTMV5Q_LZaZ9OqRkABIgyUR2hu5xg6rUDAAEC", "Suzy Lafayette", "As soon as she has no cards in her hand, she instantly draws a card from the deck."),
            MakeInlineResult("AgADBAADzqkxGw_eSVMlD4DVchKYFsSvpxkABE31Rx3Xj52SNLEDAAEC", "Vulture Sam", "Whenever a player dies, he takes in hand all the cards that player had in his hands and in play."),
            MakeInlineResult("AgADBAAD0KkxGw_eSVNKWLTzV7zud7R5mxkABFI0Boe86KoR_94BAAEC", "Willy the Kid", "He can play any number of Bang! cards during his turn."),

            //roles
            MakeInlineResult("AgADBAADx6kxGw_eSVMdKmjaYiAQcpjQvBkABNULlSPg3tu27zwDAAEC", "Sheriff", "His goal is to kill all the Outlaws and the Renegade.\nNote: He has one more life point than how many are shown on his character card. He is the only role revealed to everyone."),
            MakeInlineResult("AgADBAADtakxGw_eSVPUl1xvda6db3FZqRkABMkTU9JmFYPUxrQDAAEC", "Outlaw", "His goal is to kill the Sheriff.\nNote: Only the Sheriff's role is revealed to everyone."),
            MakeInlineResult("AgADBAADxKkxGw_eSVNUOFpmF8ovEA8bqRkABKOrZQG6cM0Luq0DAAEC", "Renegade", "His goal is to kill all the other players and be the last one standing. If the Sheriff does not die as the last one, he loses.\nNote: Only the Sheriff's role is revealed to everyone."),
            MakeInlineResult("AgADBAADzKkxGw_eSVM9FqBYVepwi6ZbuxkABIPv3i4RPQSJZUADAAEC", "Deputy Sheriff", "His goal is to protect the Sheriff and kill all the Outlaws and the Renegade. If the Sheriff dies, he loses.\nNote: Only the Sheriff's role is revealed to everyone.")
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
