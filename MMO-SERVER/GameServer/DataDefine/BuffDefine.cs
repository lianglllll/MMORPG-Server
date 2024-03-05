//
// Auto Generated Code By excel2json
// https://neil3d.gitee.io/coding/excel2json.html
// 1. 每个 Sheet 形成一个 Struct 定义, Sheet 的名称作为 Struct 的名称
// 2. 表格约定：第一行是变量名称，第二行是变量类型

// Generate From BuffDefine.xlsx

public class BuffDefine
{
	public int BID; // 编号
	public string Name; // 名称
	public string Description; // 介绍
	public string IconPath; // 图标路径
	public float MaxDuration; // 持续时间
	public int MaxLevel; // 堆叠上限
	public string BuffType; // 种类
	public string BuffConflict; // 叠加方式
	public bool Dispellable; // 是否可以驱散
	public int Demotion; // 降级
	public float TimeScale; // 时间速率
}


// End of Auto Generated Code
