//
// Auto Generated Code By excel2json
// https://neil3d.gitee.io/coding/excel2json.html
// 1. 每个 Sheet 形成一个 Struct 定义, Sheet 的名称作为 Struct 的名称
// 2. 表格约定：第一行是变量名称，第二行是变量类型

// Generate From TaskDefine.xlsx

public class TaskDefine
{
	public int Task_id; // 任务id
	public int Task_type; // 任务类型
	public int Chain_id; // 所属任务链ID
 
	public int Sub_id; // 任务链中的子序号

	public string Title; // 任务标题
	public string Desc; // 任务描述
	public string Icon; // 任务图标
	public string Pre_conditions; // 任务解锁条件
	public string Target_conditions; // 任务完成条件
	public string Reward_items; // 奖励
	public int[] Next_chains; // 后续开启的支线链
	public string Daily_refresh_time; // 日常任务刷新时间（格式：HH:MM）
	public string Expire_time; // 限时任务过期时间（格式：YYYY-MM-DD HH:MM:SS）
}


// End of Auto Generated Code
