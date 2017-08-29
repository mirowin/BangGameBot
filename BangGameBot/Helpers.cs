using System;
using System.Collections.Generic;
using Telegram.Bot.Types.InlineQueryResults;

namespace BangGameBot
{
    static class Helpers
    {
        public static readonly Dictionary<ErrorMessage, string> ErrorMessages = new Dictionary<ErrorMessage, string>() {
            {ErrorMessage.NoPlayersToStealFrom, "There are no players to steal from!"},
            {ErrorMessage.UseBeer, "You have to use a Beer!"},
            {ErrorMessage.NoPlayersToPutInJail, "You can't put the Sheriff in Jail!" },
            {ErrorMessage.CantUseMissed, "You can't use a Missed! card during your turn!" },
            {ErrorMessage.OnlyOneBang, "You can use only one Bang! card per turn!" },
            {ErrorMessage.AlreadyInUse, "You already have another copy of this card in play!" },
            {ErrorMessage.EveryoneMaxLives, "All the players have the maximum number of life points!" },
            {ErrorMessage.NoCardsToDiscard, "There are no cards to discard from any player!" },
            {ErrorMessage.MaxLives, "You already have the maximum possible life points!" },
            {ErrorMessage.NoReachablePlayers, "You can't reach any player with your weapon!" },
            {ErrorMessage.BeerFinalDuel, "Beers have no effect when only two players are left!" },
            {ErrorMessage.UseMissed, "You have to use a Missed! card!" },
            {ErrorMessage.UseBang, "You have to use a Bang! card!" }
        };

        public static readonly List<CardDescription> Cards = new List<CardDescription>() {
            //normal cards
            new CardDescription("AgADBAADqKkxGw_eSVN7g-gAAT0LfR7747wZAATVefIawihEpUw5AwABAg", "Bang!", CardName.Bang, "Remove one life point from a player at reachable distance.\nNote: You can play only one Bang! card per turn (unless you have a Volcanic in play)."),
            new CardDescription("AgADBAADq6kxGw_eSVOQAxG7emk3FsOGpxkABFv2slmuEVuErbQDAAEC", "Beer", CardName.Beer, "Regain one life point. You can use this card also out of turn, only if you have just received a hit that is lethal.\nNote: You cannot gain more life points than your starting amount."),
            new CardDescription("AgADBAADr6kxGw_eSVPJWSHq0QHkziw_qRkABBH0Qj1tfBrnsbgDAAEC", "Cat Balou", CardName.CatBalou, "With this card, you make any player discard a card of your choice."),
            new CardDescription("AgADBAADsqkxGw_eSVO6pJbke38JvKJkuxkABLzwUc2A_AVwlj0DAAEC", "Duel", CardName.Duel, "Challenge any other player. Each of you two, in turns, may discard a Bang! card. The one of you that can't (or doesn't want to) loses a life point."),
            new CardDescription("AgADBAADtqkxGw_eSVNU6vaFKoioPvpmqRkABEVbDdSQTUIB0bUDAAEC", "Gatling", CardName.Gatling, "Shoot a Bang! to all the other players.\nNote: it is NOT considered as a Bang! card."),
            new CardDescription("AgADBAADtKkxGw_eSVMxPqzNZET-CDO-mxkABLRT08CeUKO5_RsEAAEC", "General Store", CardName.GeneralStore, "Reveal as many cards from the deck face up as the players. Starting with you, each player chooses one of those cards and takes it in hand."),
            new CardDescription("AgADBAADt6kxGw_eSVPAT_AIi-LzPJvHvBkABAf1HtmQA0yW2ToDAAEC", "Indians!", CardName.Indians, "Each player, excluding you, may discard a Bang! card, or lose one life point.\nNote: Missed! and Barrel have no effect in this case."),
            new CardDescription("AgADBAADvKkxGw_eSVNjoCTVYyXi7bykmxkABPYSpAnFGsEeCeEBAAEC", "Missed!", CardName.Missed, "You can play this card only when you're the target of a Bang! or Gatling, to cancel the shot and not lose a life point."),
            new CardDescription("AgADBAADv6kxGw_eSVNqN4dLDw2kcG4cvRkABHiGAAEQVxHuXbo5AwABAg", "Panic!", CardName.Panic, "Draw a card from a player which you see at distance 1."),
            new CardDescription("AgADBAADxqkxGw_eSVNJU7DOZ4FCHYravBkABEyrKe-_CbDJODsDAAEC", "Saloon", CardName.Saloon, "Everyone regains a life point.\nNote: this is not a Beer card, so you can't play it if you received a lethal hit.\nNote: You cannot gain more life points than your starting amount."),
            new CardDescription("AgADBAADsKkxGw_eSVOa2f2Ps8lu_9E-qRkABLfGKLyFQmUTdrEDAAEC", "Stagecoach", CardName.Stagecoach, "Draw two cards from the deck."),
            new CardDescription("AgADBAADz6kxGw_eSVO_TF7agq5ct9fPvBkABKqX4VkLPlr6rjwDAAEC", "Wells Fargo", CardName.WellsFargo, "Draw three cards from the deck."),

            //permcards
            new CardDescription("AgADBAADqakxGw_eSVP11sEKFHKtEFVYqRkABO7VEFW2NSJbBLEDAAEC", "Barrel", CardName.Barrel, "Allows you to “draw!” when you are the target of a Bang! or of a Gatling. If you draw a Heart card, you are Missed!, otherwise, you lose a life point."),
            new CardDescription("AgADBAADsakxGw_eSVPM5GA34ufB19lZqRkABJccV61a9ahbj7ADAAEC", "Dynamite", CardName.Dynamite, "At the beginning of your turn, you “draw!”: if the “drawn!” card is a 2-9 of spades, you lose 3 life points and discard the Dynamite, otherwise, pass the Dynamite to the next player."),
            new CardDescription("AgADBAADwqkxGw_eSVPTwDiSgmjomvXbvBkABBKdaEpaR5jUmzgDAAEC", "Jail", CardName.Jail, "Choose a player (any but the Sheriff!) to put in jail. If you are in jail, at the beginning of the turn you “draw!”: you play your turn only if the card is a Heart. The Jail is discarded anyway."),
            new CardDescription("AgADBAADvqkxGw_eSVN_ihOEeIeTtWRVqRkABCLj0mNcJSwFG7ADAAEC", "Mustang", CardName.Mustang, "If you have this card in play, other players see you at a distance increased by 1. You still see other players at normal distance."),
            new CardDescription("AgADBAADvakxGw_eSVNTuF27mcEVYwUYvRkABL7Q3NeR6_m4Zz0DAAEC", "Scope", CardName.Scope, "If you have this card in play, you see others at a distance decreased by 1. Other players still see you at normal distance.\nNote: Distances less than 1 are considered to be 1."),

            //weapons
            new CardDescription("AgADBAADyKkxGw_eSVMg6pYqnVHxX5AcvRkABL679kCpTALrzzgDAAEC", "Schofield", CardName.Schofield, "This weapon increases your reachable distance to 2 (default is 1).\nNote: You can only have one weapon in play at a time (if you have another one in play, it is discarded)."),
            new CardDescription("AgADBAADw6kxGw_eSVP4mSVWWh046Bb7qBkABPJYydekdGr2RawDAAEC", "Remington", CardName.Remington, "This weapon increases your reachable distance to 3 (default is 1).\nNote: You can only have one weapon in play at a time (if you have another one in play, it is discarded)."),
            new CardDescription("AgADBAADrqkxGw_eSVNIMQ8p0-Qm4KpKXhkABKDs9IRHxmxyMsgCAAEC", "Rev. Carabine", CardName.RevCarabine, "This weapon increases your reachable distance to 4 (default is 1).\nNote: You can only have one weapon in play at a time (if you have another one in play, it is discarded)."),
            new CardDescription("AgADBAAD0akxGw_eSVMzrQjUwBwfi9CRpxkABBRPeZb2E9_fta4DAAEC", "Winchester", CardName.Winchester, "This weapon increases your reachable distance to 5 (default is 1).\nNote: You can only have one weapon in play at a time (if you have another one in play, it is discarded)."),
            new CardDescription("AgADBAADzakxGw_eSVPrSHSrFpURespJuxkABPRZvWBAhYVWsTsDAAEC", "Volcanic", CardName.Volcanic, "This weapon lets you shoot as many Bang! cards as you want, but only at distance 1.\nNote: You can only have one weapon in play at a time (if you have another one in play, it is discarded)."),

            //characters
            new CardDescription("AgADBAADqqkxGw_eSVOiUP_XsnKPc-puaRkABODvJnNtrC3f9eEBAAEC", "Bart Cassidy", Character.BartCassidy, "Each time he loses a life point, he immediately draws a card from the deck."),
            new CardDescription("AgADBAADrKkxGw_eSVNKhVOoPE6KD1OwuxkABF-akCQV9AyCjz0DAAEC", "Black Jack", Character.BlackJack, "At the beginning of his turn, he must show the second card he draws: if it’s a Heart or Diamond, he draws one additional card (without revealing it)."),
            new CardDescription("AgADBAADrakxGw_eSVNYNi5mODn1VgMavRkABZty29NaL2ZcPgMAAQI", "Calamity Janet", Character.CalamityJanet, "She can use Bang! cards as Missed! cards and vice versa.\nNote: If she plays a Missed! card as a Bang!, she cannot play another Bang! card that turn (unless a Volcanic is active)."),
            new CardDescription("AgADBAADs6kxGw_eSVMtfLFHRGgZlPUoqRkABPncLrs1KWSTZa8DAAEC", "El Gringo", Character.ElGringo, "Each time he loses a life point due to a card played by another player, he draws a card from the hand of that player.\nNote: Dynamite damages are not caused by anyone."),
            new CardDescription("AgADBAADuKkxGw_eSVN5KSwkbCkuu904nRkABJ7I5yWPgqGi9doBAAEC", "Jesse Jones", Character.JesseJones, "At the beginning of his turn, he may choose to draw the first card from the deck, or from the hand of any other player; then he draws the second card from the deck."),
            new CardDescription("AgADBAADuakxGw_eSVM-lZRWie_k9p39qBkABJp6Bdmd71BDna8DAAEC", "Jourdounnais", Character.Jourdounnais, "He is considered to have Barrel in play at all times.\nNote that he can still play another real Barrel card, and use both of them."),
            new CardDescription("AgADBAADuqkxGw_eSVNBzxjuwQUF_tP_vBkABBb1iaGOShsqUzgDAAEC", "Kit Carlson", Character.KitCarlson, "At the beginning of his turn, he looks at the top three cards of the deck: he chooses 2 to draw, and puts the other one back on the top of the deck, face down."),
            new CardDescription("AgADBAADu6kxGw_eSVPapbLPKgr4etXNvBkABFn5dE3XieMQmzwDAAEC", "Lucky Luke", Character.LuckyDuke, "Each time he is required to “draw!”, he flips the top two cards from the deck, and chooses the result he prefers. Discard both cards afterward."),
            new CardDescription("AgADBAADwKkxGw_eSVMqJOr9U_sX8NhuaRkABA-rpi47MpA5N90BAAEC", "Paul Regret", Character.PaulRegret, "He is considered to have Mustang in play at all times.\nNote that he can still play another real Mustang card, and make others see him at distance increased by 2."),
            new CardDescription("AgADBAADwakxGw_eSVMPVKkDTGNfFN9UvRkABCNbcRVgNjjw2kEDAAEC", "Pedro Ramirez", Character.PedroRamirez, "At the beginning of his turn, he may choose to draw the first card from the deck, or from the top of the graveyard; then he draws the second card from the deck."),
            new CardDescription("AgADBAADxakxGw_eSVNuECNl14WkHuIVvRkABOVYEOiQi7tVCD4DAAEC", "Rose Doolan", Character.RoseDoolan, "She is considered to have Scope in play at all times.\nNote that she can still play another real Scope card, and see others at distance decreased by 2."),
            new CardDescription("AgADBAADyakxGw_eSVPL7mBomFAEYUzlvBkABGFI7j7fobMf-jsDAAEC", "Sid Ketchum", Character.SidKetchum, "At any time, he can discard 2 cards to regain a life point. This can also be used if he received a lethal hit.\nNote: Unlike Beer cards, this does have effect in the 1v1 duel."),
            new CardDescription("AgADBAADyqkxGw_eSVNblXUkQVEGpXrLvBkABAkwRniMNSx31DkDAAEC", "Slab the Killer", Character.SlabTheKiller, "Players trying to cancel his Bang! cards need to play 2 Missed! cards. The Barrel effect, if successfully used, only counts as one Missed! card."),
            new CardDescription("AgADBAADy6kxGw_eSVPbTMV5Q_LZaZ9OqRkABIgyUR2hu5xg6rUDAAEC", "Suzy Lafayette", Character.SuzyLafayette, "As soon as she has no cards in her hand, she instantly draws a card from the deck."),
            new CardDescription("AgADBAADzqkxGw_eSVMlD4DVchKYFsSvpxkABE31Rx3Xj52SNLEDAAEC", "Vulture Sam", Character.VultureSam, "Whenever a player dies, he takes in hand all the cards that player had in his hands and in play."),
            new CardDescription("AgADBAAD0KkxGw_eSVNKWLTzV7zud7R5mxkABFI0Boe86KoR_94BAAEC", "Willy the Kid", Character.WillyTheKid, "He can play any number of Bang! cards during his turn."),

            //roles
            new CardDescription("AgADBAADx6kxGw_eSVMdKmjaYiAQcpjQvBkABNULlSPg3tu27zwDAAEC", "Sheriff", Role.Sheriff, "His goal is to kill all the Outlaws and the Renegade.\nNote: He has one more life point than how many are shown on his character card. He is the only role revealed to everyone."),
            new CardDescription("AgADBAADtakxGw_eSVPUl1xvda6db3FZqRkABMkTU9JmFYPUxrQDAAEC", "Outlaw", Role.Outlaw, "His goal is to kill the Sheriff.\nNote: Only the Sheriff's role is revealed to everyone."),
            new CardDescription("AgADBAADxKkxGw_eSVNUOFpmF8ovEA8bqRkABKOrZQG6cM0Luq0DAAEC", "Renegade", Role.Renegade, "His goal is to kill all the other players and be the last one standing. If the Sheriff does not die as the last one, he loses.\nNote: Only the Sheriff's role is revealed to everyone."),
            new CardDescription("AgADBAADzKkxGw_eSVM9FqBYVepwi6ZbuxkABIPv3i4RPQSJZUADAAEC", "Deputy Sheriff", Role.DepSheriff, "His goal is to protect the Sheriff and kill all the Outlaws and the Renegade. If the Sheriff dies, he loses.\nNote: Only the Sheriff's role is revealed to everyone.")
        };

        private static List<InlineQueryResultCachedPhoto> _inlineResults = null;

        public static List<InlineQueryResultCachedPhoto> GetInlineResults()
        {
            if (_inlineResults != null)
                return _inlineResults;
            _inlineResults = new List<InlineQueryResultCachedPhoto>();
            foreach (var card in Cards)
            {
                _inlineResults.Add(new InlineQueryResultCachedPhoto()
                {
                    Id = card.Name,
                    Title = card.Name,
                    FileId = card.PhotoId,
                    Description = card.Description, //hoping someone will see it XD
                    Caption = card.Name + "\n\n" + card.Description
                });
            }
            return _inlineResults;
        }

        public class CardDescription
        {
            public Type CardType;
            public int EnumVal;
            public string Name;
            public string PhotoId;
            public string Description;
            public CardDescription(string photoid, string name, CardName card, string description)
            {
                CardType = typeof(CardName);
                EnumVal = (int)card;
                Name = name;
                PhotoId = photoid;
                Description = description;
            }

            public CardDescription(string photoid, string name, Character character, string description)
            {
                CardType = typeof(Character);
                EnumVal = (int)character;
                Name = name;
                PhotoId = photoid;
                Description = description;
            }

            public CardDescription(string photoid, string name, Role role, string description)
            {
                CardType = typeof(Role);
                EnumVal = (int)role;
                Name = name;
                PhotoId = photoid;
                Description = description;
            }

        }

    }
}
