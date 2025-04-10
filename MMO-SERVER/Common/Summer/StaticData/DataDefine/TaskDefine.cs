//
// Auto Generated Code By excel2json
// https://neil3d.gitee.io/coding/excel2json.html
// 1. 每个 Sheet 形成一个 Struct 定义, Sheet 的名称作为 Struct 的名称
// 2. 表格约定：第一行是变量名称，第二行是变量类型

// Generate From TaskDefine.xlsx

public class TaskDefine
{
	public int ID; // 默认Id
	public int Task_chain_id; // 链id
	public int Task_sub_id; // 任务子id
	public string Icon; // 任务图标
	public string Desc; // 描述
	public string Task_target; // 任务目标
	public int Target_amount; // 目标数量
	public string Award; // 奖励
	public int[] Open_chain; // 打开的支线链
}


// End of Auto Generated Code
