# 项目概述

自网络游戏出现以来，其发展趋势便势不可挡，可见其受欢迎程度，毕竟相对于死板的ai，人更喜欢与真实的人进行交互。

本项目主要用于学习如今网游的开发流程，学习网游关键技术，最终能构建我心中的那个世界。



**项目演示视频链接：**

https://www.bilibili.com/video/BV1Rw4m1m7vS/?spm_id_from=333.999.0.0&vd_source=ff929fb8407b30d15d4d258e14043130



# 项目已完成功能

- 基础的人物位置同步、属性同步、技能同步
- 热更新(yooasset+hybridclr实现)
- 简单的敌人AI(状态机实现)
- 背包系统(物品、装备)
- 频道系统(仅全服)
- AOI机制(九宫格实现)



# MMORPG项目结构

**MMO-Client**：unity客户端的源码。

**MMO-SERVER**：服务端C#源代码。

**Tools**：里面有proto工具、excel工具、构建mysql数据库的sql文件。

**note**:是本人在学习项目的时候所做笔记。





# 项目部署运行

1.搭建Mysql数据库环境。

2.搭建资源服务器环境，这里使用宝塔。

3.server部署，使用net6.0环境

​	具体操作参考个人笔记MMORPG.md，注意要修改config.yaml配置文件。

4.client部署，使用unity 2021.3.5f1c1