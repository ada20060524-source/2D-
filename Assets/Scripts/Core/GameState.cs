namespace ShooterGame.Core
{
    /// <summary>
    /// 全域遊戲狀態機定義
    /// </summary>
    public enum GameState
    {
        Title,
        Playing,
        Warning,
        Paused,
        GameOver,
        Victory
    }
}
