using System.Collections.Generic;

namespace BangGameBot
{
    public class Choice {
        public Card CardChosen { get; } = null;
        public bool? ChoseYes { get; } = null;
        public Player PlayerChosen { get; } = null;

        public Choice (bool choice) {
            ChoseYes = choice;
        }

        public Choice (Card choice) {
            CardChosen = choice;
        }

        public Choice (Player p) {
            PlayerChosen = p;
        }
    }

    public static class DefaultChoice {
        public static readonly bool UseAbilityPhaseOne = false;
        public static Player ChoosePlayer(IEnumerable<Player> players) {
            return players.Random();
        }
        public static readonly Card ChooseCard = null;
        public static Card ChooseCardFrom(List<Card> cards) {
            return cards.Random();
        }
        public static readonly bool DiscardCard = false;
        public static readonly bool UseAblityPhaseThree = false;
        public static readonly bool UseBarrel = false;
    }
}

