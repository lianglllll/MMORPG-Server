//
// Auto Generated Code By excel2json
// https://neil3d.gitee.io/coding/excel2json.html
// 1. 每个 Sheet 形成一个 Struct 定义, Sheet 的名称作为 Struct 的名称
// 2. 表格约定：第一行是变量名称，第二行是变量类型

// Generate From UnitDefine.xlsx

public class UnitDefine
{
	public int TID; // 类型编号
	public string Name; // 名称
	public string Resource; // 资源
	public string BgResource; // 图片资源
	public string Kind; // 类型
	public string Desc; // 介绍
	public int[] DefaultSkills; // 默认技能组,这里其实就是普通攻击
	public int Speed; // 速度
	public int HPMax; // 生命值
	public int MPMax; // 法力值
	public int InitLevel; // 初始等级
	public float AD; // 物攻
	public float AP; // 魔攻
	public float DEF; // 物防
	public float MDEF; // 魔防
	public float CRI; // 暴击率%
	public float CRD; // 暴击伤害%
	public float HitRate; // 命中率%
	public float DodgeRate; // 闪避率%
	public float HpRegen; // 生命恢复/秒
	public float HpSteal; // 伤害吸血%
	public float STR; // 力量
	public float INT; // 智力
	public float AGI; // 敏捷
	public float GSTR; // 力量成长
	public float GINT; // 智力成长
	public float GAGI; // 敏捷成长
	public string AI; // AI名称
	public long ExpReward; // 经验奖励
	public long GoldReward; // 金币奖励
}


// End of Auto Generated Code
