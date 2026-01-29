using UnityEngine;

public enum GameState
{
    None,
    MainMenu,
    WaitforOtherPlayer,
    Gameplay,
    GameWon,
    GameOver, 
    WaitForLevelChange

}

public enum OpponentType
{
    Player,
    Ai
}

public enum GameModeType
{
    NONE,
    SINGLEPLAYER,
    MULTIPLAYER
}

public enum PowerUpType
{
    None,
    BigArrow,
    Bomb,
    Freeze,
    Shield
}