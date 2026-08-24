
public enum UIEnum
{
    GAMEPLAY_UI,
    LOADING_UI,
    LEVEL_UP_UI,
    LOSE_UI,
    RENT_SLOT_UI,
}

public enum GameSessionState { Loading, Playing, Won, Failed }

public static class GameFailReason
{
    public const string TimeExpired = "TimeExpired";
    public const string ConveyorOverflow = "ConveyorOverflow";
    public const string None = "None";
}
public enum MinionColor
{
    Blue = 0,
    Orange = 1,
    Teal = 2,
    Red = 3,
    Green = 4,
    Yellow = 5,
    Purple = 6,
    Pink = 7,
    White = 8,
    Brown = 9,
}
