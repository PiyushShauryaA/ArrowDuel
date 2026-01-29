public static class PlayerData
{
    public static string playerName = "Player";
    public static GameModeType gameMode = GameModeType.NONE;
    public static bool isAIMode = false;
    public static AiSkillLevels aiDifficulty = AiSkillLevels.Normal;
    
    public static void ClearData()
    {
        playerName = "Player";
        gameMode = GameModeType.NONE;
        isAIMode = false;
        aiDifficulty = AiSkillLevels.Normal;
    }
}
