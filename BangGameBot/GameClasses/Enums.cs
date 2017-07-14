namespace BangGameBot
{

    public enum CardName
    {
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
        PaulRegret, Jourdounnais, BlackJack, SlabTheKiller, ElGringo, JesseJones, SuzyLafayette, WillyTheKid, RoseDoolan, BartCassidy, PedroRamirez, SidKetchum, LuckyDuke, VultureSam, CalamityJanet, KitCarlson
    }

    public enum Role
    {
        Sheriff, DepSheriff, Renegade, Outlaw
    }

    public enum GameStatus
    {
        Joining, Running, Ending
    }

    public enum CardType
    {
        Normal, PermCard, Weapon
    }

    public enum ErrorMessage
    {
        NoError, NoPlayersToStealFrom
    }

}