using UnityEngine;

namespace ShooterGame.Core
{
    /// <summary>
    /// 全域物理圖層定義與常數
    /// </summary>
    public static class GameLayers
    {
        public const string LayerNamePlayer = "Player";
        public const string LayerNamePlayerBullet = "PlayerBullet";
        public const string LayerNameEnemy = "Enemy";
        public const string LayerNameEnemyBullet = "EnemyBullet";
        public const string LayerNameItem = "Item";

        public static int Player => LayerMask.NameToLayer(LayerNamePlayer);
        public static int PlayerBullet => LayerMask.NameToLayer(LayerNamePlayerBullet);
        public static int Enemy => LayerMask.NameToLayer(LayerNameEnemy);
        public static int EnemyBullet => LayerMask.NameToLayer(LayerNameEnemyBullet);
        public static int Item => LayerMask.NameToLayer(LayerNameItem);

        public static int PlayerMask => 1 << Player;
        public static int PlayerBulletMask => 1 << PlayerBullet;
        public static int EnemyMask => 1 << Enemy;
        public static int EnemyBulletMask => 1 << EnemyBullet;
        public static int ItemMask => 1 << Item;
    }
}
