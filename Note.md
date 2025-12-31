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

- ~~能否改掉寻物上交这些内容~~
- **`RemoveInvalidRepeatableQuests`可能有必要打补丁防止删除订单任务**
- ~~确认一下现在的Order类型任务是否还能更改任务~~**可以，需要进行处理**
- ~~确认一下现在的Order类型任务是否能够同时存在两个(可以，但完成一个同时也会完成同池子下其他任务)弄清为何会导致这种情况~~
- ~~条件还是改为多个上交~~
- ~~`QuestListItem.UpdateTimer`似乎可以触发`/client/repeatalbeQuests/activityPeriods`~~
- - ~~其实可以直接在打开任务界面的时候强制触发`/client/repeatalbeQuests/activityPeriods`~~
- - ~~猜测是15分钟到了开始刷新新的每日任务导致触发的~~
- ~~BUG: 15分钟过期后没有正确将任务消除，很可能是受后端影响，因为后端存档中显示此任务还处于activeQuest~~
- - ~~判断是`ProcessExpiredQuests`未按预期执行~~
- ~~任务队列添加需要指定对应pmc的id，才进行添加~~
- ~~获取行动任务的函数不应该会持续执行~~
- ~~当前能够做到多个行动任务拥有不同的过期时间，但是最开始的任务会因为被刷新了过期时间导致出现即便过期了也不会立即清除的问题~~
- ~~Patch替换任务的函数~~**找个机会测试一下替换任务是否报错**
- 参考FriendlyPMC如何添加队友成员，并能够邀请加入队伍的
- - 1. 生成BOT,构建存档信息
- - 2. 并作为好友列表申请类型的消息被发送
> 好友请求`NotificationEventType.friendListNewRequest`

> 生成bot`botGenerator.PrepareAndGenerateBot`

> 删除好友`dialogueController.DeleteFriend`
- - 尝试实现通过完成订单任务后才添加指定数量的好友
- ~~要在`database/orders`中记录订单状态，并根据其中的数量生成指定好友、根据时长设定EndTime~~
- - **对FinishQuest相关函数进行Patch，当检查到任务完成的QuestId属于OrderInfos中的QuestId时，改变其OrderInfo的状态为Stated，并触发一系列函数：根据OrderInfo内容生成对应数量的Profile并保存，重新计算过期时间，发送好友列表申请**
- 要在`database/profiles`中记录护航玩家存档
- - **profiles下先是玩家的sessionId文件夹，里面才实际存放护航的存档**
- - 主要参考`saveServer.SaveProfileAsync`函数
> `jsonUtil.Serialize`, `fileUtil.WriteFile`
