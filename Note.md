# MiyakoCarryService / 宫子护航店

- 好友位自带一个TsukiyukiMiyako，通过她来进行下单（最初直接通过指令）
- 最好能使用上大模型，语音识别
- 拉去好友进队后能够在匹配界面显示
> 参考[FriendlyPMC](https://bitbucket.org/pitvenin/friendlypmc/src/version-4/)
- （最好是通过商人来交，给的是临时任务）交一点卢布获取好友位，一段时间后会自动删除好友。不同价位的装备、准度也不同
> 可参考ABPS这种根据玩家等级提升装备和等级
- 最好支持BOSS、BOSS+小弟护航
- 兼容SAIN、Fika
- 最好是自带一部分QuestingBots和LootingBots的功能，可根据交互来选择帮助你。比如我想要什么物品，他就会去帮你找
- **赞助版**：解锁红护/挂护：护航AI使用魔法子弹、无视障碍索敌，帮你吸物资
- - 如果是红护根据价位有不同的概率（通过封号系数决定，可关闭）可能被封号，然后根据你本局的战利品收益进行追缴（可关闭）
- 图标：宫子头饰, 后面一个光环
- 根据好感度等级能够打折扣

## TODO

- ~~实现好友位的TsukiyukiMiyako~~
- 实现~~自定义商人~~和~~发放行动任务~~
- - ~~弄清如何构建行动任务~~
- - ~~实现临时指令来触发发放行动任务~~
- - ~~根据人数修改条件数量~~。
> 参考`GenerateAvailableForFinish`

- - ~~别忘了修改任务接取过期时长为15分钟~~
- - 新的AI队友处于好友列表的时长才为指定的时间
> 参考[江湖](https://github.com/Hiokree/Jiang-Hu/tree/main/JiangHu.Server)的`new quest`,`new trader`,`quest generator`

> 参考[AddTraderWithAssortJson](https://github.com/sp-tarkov/server-mod-examples/tree/main/13AddTraderWithAssortJson)

> 参考[RepeatableQuestController](https://github.com/sp-tarkov/server-csharp/blob/main/Libraries/SPTarkov.Server.Core/Controllers/RepeatableQuestController.cs#L68)

- 能否改掉寻物上交这些内容
- `RemoveInvalidRepeatableQuests`可能有必要打补丁防止删除订单任务