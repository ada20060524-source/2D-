namespace ShooterGame.Core
{
    /// <summary>
    /// 可受擊介面：適用於敵機、隕石障礙物與未來 Boss 等實體
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(int damage);
    }
}
