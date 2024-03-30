//
// Auto Generated Code By excel2json
// https://neil3d.gitee.io/coding/excel2json.html
// 1. 每个 Sheet 形成一个 Struct 定义, Sheet 的名称作为 Struct 的名称
// 2. 表格约定：第一行是变量名称，第二行是变量类型

// Generate From SkillDefine.xlsx

public class SkillDefine
{
	public int ID; // 编号
	public int TID; // 单位类型
	public int Code; // 技能码
	public string Name; // 技能名称
	public string Description; // 技能描述
	public int Level; // 技能等级
	public int MaxLevel; // 技能上限等级
	public int ReqLevel; // 等级要求
	public string Type; // 技能的类别
	public string TargetType; // 目标类型
	public bool IsGroupAttack; // 是否是群体体攻击
	public string EffectAreaType; // 技能影响的有效范围类别
	public int Area; // 影响半径区域
	public int EffectAreaAngle; // 影响的扇形角度，以角色的forword为中心
	public int[] EffectAreaLengthWidth; // 影响的矩形区域，以角色的forword为起始方向
	public string Icon; // 技能图标
	public string IntonateArt; // 蓄气自身的粒子效果
	public string HitArt; // 击中效果，粒子特效
	public int SpellRange; // 施法距离
	public int Cost; // 魔法消耗
	public float AD; // 物理攻击
	public float AP; // 法术攻击
	public float ADC; // 物攻加成(百分比)
	public float APC; // 法攻加成
	public float IntonateTime; // 施法前摇
	public string IntonateAnimName; // 前摇动作
	public float Duration; // 激活状态动画持续的时间
	public string ActiveAnimName; // 激活动作
	public float PostRockTime; // 技能的后摇时间，我这里用作缓冲
	public float CD; // 冷却时间
	public bool IsMissile; // 是否是投射物
	public string Missile; // 投射物
	public int MissileSpeed; // 投射速度
	public float Interval; // 伤害间隔
	public float[] HitDelay; // 命中时间,伤害次数
	public int[] BUFF; // 附加效果，这里面填buffId如流血效果
}


// End of Auto Generated Code
