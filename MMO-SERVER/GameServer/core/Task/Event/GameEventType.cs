using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Core.Task.Event
{
    public enum GameEventType
    {
        // ===== 基础事件 =====
        None = 0,

        // ===== 战斗相关 =====
        EnemyKilled,            // 参数: (EnemyID, Count)
        BossDefeated,           // 参数: (BossID, Difficulty)
        DamageDealt,            // 参数: (DamageAmount, DamageType)
        PlayerDamaged,          // 参数: (DamageAmount, SourceType)
        PlayerHealed,           // 参数: (HealAmount)
        SkillCast,              // 参数: (SkillID, TargetType)
        CriticalHit,            // 参数: (DamageAmount)

        // ===== 探索与移动 =====
        AreaEntered,         // 参数: (AreaID)
        HiddenItemFound,     // 参数: (ItemID)
        TeleportUsed,        // 参数: (FromLocation, ToLocation)
        MapCompleted,        // 参数: (MapID, CompletionTime)

        // ===== 物品与资源 =====
        ItemCollected,       // 参数: (ItemID, Count)
        ItemCrafted,         // 参数: (ItemID, RecipeID)
        ResourceGathered,    // 参数: (ResourceType, Amount)
        InventoryFull,       // 参数: (无)
        ItemSold,            // 参数: (ItemID, GoldEarned)
        ItemPurchased,       // 参数: (ItemID, GoldSpent)

        // ===== 角色成长 =====
        LevelUp,             // 参数: (NewLevel)
        SkillUpgraded,       // 参数: (SkillID, NewLevel)
        AttributeIncreased,  // 参数: (AttributeType, NewValue)
        AchievementUnlocked, // 参数: (AchievementID)

        // ===== 社交与多人 =====
        GuildJoined,            // 参数: (GuildID)
        PlayerTrade,            // 参数: (PartnerID, ItemID, Amount)
        PartyFormed,            // 参数: (PartySize)
        FriendAdded,            // 参数: (FriendID)
        PvPVictory,             // 参数: (OpponentLevel)

        // ===== 经济与商店 =====
        GoldEarned,          // 参数: (Amount, SourceType)
        GoldSpent,           // 参数: (Amount, ItemID)
        AuctionWon,          // 参数: (ItemID, Price)
        ShopRestocked,       // 参数: (ShopID)

        // ===== 任务与剧情 =====
        QuestAccepted,       // 参数: (QuestID)
        QuestCompleted,      // 参数: (QuestID)
        DialogueChoiceMade,  // 参数: (NPCID, ChoiceID)
        StoryBranchUnlocked, // 参数: (BranchID)

        // ===== 时间与周期 =====
        DailyLogin,          // 参数: (连续登录天数)
        TimeOfDayChanged,    // 参数: (NewHour)
        SeasonChanged,       // 参数: (SeasonType)

        // ===== 特殊挑战 =====
        SurvivalTimeElapsed, // 参数: (SecondsSurvived)
        ComboReached,        // 参数: (ComboCount)
        SpeedrunMilestone,   // 参数: (CheckpointTime)

        // ===== 自定义事件 =====
        CustomEvent1,        // 参数: 自定义
        CustomEvent2,         // 参数: 自定义

        // else
        EnterGame,
    }


}
