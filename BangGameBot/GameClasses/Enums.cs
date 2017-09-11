namespace BangGameBot
{

    public enum CardName
    {
        None,
        //Cards
        Bang, Missed, Beer, Panic, CatBalou, Stagecoach, WellsFargo, Gatling, Duel, Indians, GeneralStore, Saloon,
        //PermCards
        Jail, Dynamite, Barrel, Scope, Mustang,
        //Weapons
        Volcanic, Schofield, Remington, RevCarabine, Winchester
    }

    public enum CardSuit
    {
        Hearts, Diamonds, Clubs, Spades
    }

    public enum Character
    {
        None,

        PaulRegret, Jourdounnais, BlackJack, SlabTheKiller, ElGringo, JesseJones, SuzyLafayette, WillyTheKid, RoseDoolan, BartCassidy, PedroRamirez, SidKetchum, LuckyDuke, VultureSam, CalamityJanet, KitCarlson
    }

    public enum Role
    {
        Sheriff, DepSheriff, Renegade, Outlaw
    }

    public enum GameStatus
    {
        Joining, Initialising, PhaseZero, PhaseOne, PhaseTwo, PhaseThree,
        Ending
    }

    public enum CardType
    {
        Normal, PermCard, Weapon
    }

    public enum ErrorMessage
    {
        NoError = 0, NoPlayersToStealFrom, UseBeer,
        NoPlayersToPutInJail,
        CantUseMissed,
        OnlyOneBang,
        AlreadyInUse,
        EveryoneMaxLives,
        NoCardsToDiscard,
        MaxLives,
        NoReachablePlayers,
        BeerFinalDuel,
        UseMissed,
        UseBang
    }

    public enum Situation
    {
        Standard, PlayerDying, PhaseThree,
        PlayerShot,
        DiscardBang
    }  

    public enum Request
    {
        Join, VoteStart, Leave
    }

}