# Player (15).log 报错分析

分析日期：2026-09-11。日志：`D:\QQ\Player (15).log`。目标项目：`E:\RimModDev\MunoRace\MunoRace\MunoRace`。

## 已处理的缪诺问题

| 日志位置 | 问题 | 原因与处理 |
| --- | --- | --- |
| 1462–1580，后续重复到 2031 附近 | 人质交换生成奖励成员时反复出现空引用 | `GenerateMunoColonist` 使用玩家派系和 `PlayerStarter`，人物在年龄初始化时已被视为殖民者。成年回调提前分配成年背景，访问尚未生成的 `pawn.story.Childhood.workDisables`。改为以缪诺派系、`NonPlayer` 完成生成，再招募到玩家派系。 |
| 1514–1518、1575–1579 | 奖励生成失败后反复重试 | 异常越过返回值检查，使 `MunoShuttleExchangeSession` 未能标记失败，每 30 tick 再次结算。现在批量生成捕获并记录完整异常，清理已生成但未交付的成员，返回失败，让既有会话失败分支结束交换。 |
| 69 | `Parsed 3.2 as int.` | 连发间隔 `ticksBetweenBurstShots` 是整数。冲锋手枪的配置从 `3.2` 改为 `3`，明确使用整数 tick。 |
| 70 | 找不到 `Shot_Machinepistol` | 原版音效 Def 名为 `Shot_MachinePistol`，已纠正大小写。 |
| 81 | 找不到 Shader `Transparent` | `ClShaderPro` 继承原版 `ShaderTypeDef`，其 `shaderPath` 需要原版资源路径。改为 `Map/Transparent`；自定义材质引用保持原有配置。 |
| 95 | `Mote_MunoRadiationConeEffect` 缺少启动初始化标记 | 该类持有静态 `MaterialPropertyBlock`，已添加 `[StaticConstructorOnStartup]`，交由原版主线程初始化。 |

奖励成员生成入口也供据点交换和军事任务奖励调用，因此这些入口使用同一修正。未删除背景故事的工作能力约束，未增加全局人物生成补丁。

## 日志中仍需区分的问题

- 120–143：原版 `Tribal_ChiefRanged` 连续 120 次无法满足 `requiredWorkTags`，随后引发世界生成空引用。日志没有给出每次失败人物的背景、特性和禁用工作标签；现有调用栈不足以证明由缪诺导致。本次未取消原版首领能力要求，也未对所有人物生成安装兜底补丁。
- 175–1408：存在大量 `RK_*`、`LG_*`、`Kiiro_*` 等 Def 缺失引用，以及 `Meow_RecycleApparel` 配方缺失。加载的旧数据引用了本次未加载的模组内容；随后两条 `Bill.GetUniqueLoadID` 异常与空配方账单对应。具体数据来源可能是存档或开发工具保留的模板，仅凭此日志无法进一步确认。未进行旧存档兼容或删改用户数据。
- 93–94：`Dialog_WorkbenchImageBrowser` 和 `AnimationTextureLayerRuntime` 的初始化警告来自本地 ChezhouLib 对应类，不在缪诺程序集。本次未修改 ChezhouLib。
- 日志开头其他模组的依赖元数据警告，与缪诺奖励生成异常属于不同问题。

## 源码依据

- 本地原版 `E:\捷豹\code\Source\Verse\Verse\PawnGenerator.cs`：先设置派系、生成随机年龄，再调用背景和姓名生成逻辑。
- 原版 `LifeStageWorker_HumanlikeAdult.Notify_LifeStageStarted`：游戏进行中，对殖民者补充分配成年背景。
- 原版 `PawnBioAndNameGenerator.FillBackstorySlotShuffled`：成年背景带有工作要求时访问 `pawn.story.Childhood.workDisables`。
- 原版 `VerbProperties`：`ticksBetweenBurstShots` 字段为 `int`。
- 原版 `ShaderDatabase`：透明 Shader 的资源路径为 `Map/Transparent`。
- 原版 `StaticConstructorOnStartupUtility`：包含静态 `MaterialPropertyBlock` 的类型需要启动初始化标记。
- 本地 `E:\RimModDev\ChezhouLib\Source\ChezhouLib\Rendering\Shaders\Defs\ClShaderPro.cs`：确认自定义 Shader Def 的继承关系。

## 编译结果与交付范围

使用 `E:\VS\MSBuild\Current\Bin\MSBuild.exe` 编译 `Source\MunoRaceLib\MunoRaceLib.csproj`，配置为 `Debug / AnyCPU`，编译成功，退出码为 0。

生成程序集：`1.6\Assemblies\MunoRaceLib.dll`。编译仍有项目既有的 `MSB3270` 警告：AnyCPU 项目引用 AMD64 架构的 `HarmonyMod`。

本次仅修改开发项目并完成编译与静态文件检查，没有启动游戏或运行玩法测试，也没有同步到日志所示的 Steam 游戏 Mod 目录。运行时效果仍需在使用更新后的 DLL 和 XML 后确认。
