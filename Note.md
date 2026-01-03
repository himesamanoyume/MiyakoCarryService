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

## 低优先级

- **与护航玩家聊天时Users没有内容**
> 参考`GetDialogueUsers`, `GetDialogByIdFromProfile`
- - 错误情况
```json
"6957491d2df471f9a85e4de9": {
  "attachmentsNew": 0,
  "new": 0,
  "type": 1,
  "Users": [],
  "pinned": false,
  "messages": [
    {
      "_id": "6957498c2df471f9a85e4e98",
      "uid": "68fc96b7dd043e81bc7a506c",
      "type": 1,
      "dt": 1767328140,
      "text": "哎哟我",
      "hasRewards": false,
      "rewardCollected": false,
      "items": {}
    }
  ],
  "_id": "6957491d2df471f9a85e4de9"
}
```
- - 正常情况
```json
"692edf66bfcd227424bceb4d": {
  "attachmentsNew": 0,
  "new": 0,
  "type": 1,
  "Users": [
    {
      "_id": "692edf66bfcd227424bceb4d",
      "aid": 0,
      "Info": {
        "Nickname": "TrueOMGer",
        "Side": "Bear",
        "Level": 7,
        "MemberCategory": 0,
        "SelectedMemberCategory": 0
      }
    },
    {
      "_id": "68fc96b7dd043e81bc7a506c",
      "aid": 1665585,
      "Info": {
        "Nickname": "tester1",
        "Side": "Bear",
        "Level": 23,
        "MemberCategory": 1026,
        "SelectedMemberCategory": 1024
      }
    }
  ],
  "pinned": false,
  "messages": [
    {
      "_id": "692ee153bfcd227424bd5ece",
      "uid": "692edf66bfcd227424bceb4d",
      "type": 1,
      "dt": 1764680019,
      "text": "你是个很强的对手 我的兄弟",
      "items": {}
    }
  ],
  "_id": "692edf66bfcd227424bceb4d"
}
```

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
- ~~Patch替换任务的函数~~
- ~~找个机会测试一下替换任务是否报错~~
- ~~参考FriendlyPMC如何添加队友成员，~~
- - 1. 生成BOT,构建存档信息
- - 2. 并作为好友列表申请类型的消息被发送
> 好友请求`NotificationEventType.friendListNewRequest`

> 生成bot`botGenerator.PrepareAndGenerateBot`

> 删除好友`dialogueController.DeleteFriend`
- - 尝试实现通过完成订单任务后才添加指定数量的好友
- ~~要在`database/orders`中记录订单状态，并根据其中的数量生成指定好友、根据时长设定EndTime~~
- - ~~对CompleteQuest相关函数进行Patch，当检查到任务完成的QuestId属于OrderInfos中的QuestId时，改变其OrderInfo的状态为Stated，并触发一系列函数：根据OrderInfo内容生成对应数量的Profile并保存，重新计算过期时间，发送好友列表申请~~
- ~~要在`database/profiles`中记录护航玩家存档~~
- - ~~profiles下先是玩家的sessionId文件夹，里面才实际存放护航的存档~~
- - 主要参考`saveServer.SaveProfileAsync`函数
> `jsonUtil.Serialize`, `fileUtil.WriteFile`
- ~~当前只是生成了护航玩家的部分存档，还没有生成装备、成就等信息~~(似乎用不着)
> 参考`_AddProfile`, `GenerateBot -> GenerateInventory -> GenerateAndAddEquipmentToBot`
- ~~暂时进入Debug阶段~~
- ~~完成任务后报错`MCSProfileService.SaveMCPlayerProfile`~~
- ~~BUG:申请的好友信息是自己而不是Bot~~
- ~~申请之后再次登录游戏时玩家是如何读取好友列表中的护航玩家存档数据的~~
- ~~重启游戏后直接连好友列表都无法读取，需要先实现重登后能加载护航的好友列表~~
- ~~好像最初查看其他人的档案会报错?~~
- - ~~通过读取aid来获取，但目前推测先需要将护航存档加载并放入到服务端的总存档数据结构中，否则将无法找到~~
- ~~实现能够查看护航的存档信息, 若有必要, 可再实现一遍将BotBase转换为PmcData~~
- ~~再把ScavData的PmcData一并生成~~
- ~~保存的cs存档是压缩的~~
- ~~保存的cs存档_id与sessionId不对应~~
- ~~botBase没有SessionId~~
- ~~测试持续时长结束后好友是否有被自动删除~~
- - ~~`ProcessExpiredCarryServiceProfile`推测服务端确实能够删除，但是不会有消息发送过去，因为原本删除是直接生效在客户端所以无需返回消息，而从服务端主动删除应该需要手动发送消息~~
- - `youAreRemovedFromFriendList`
- - `"{0} removed you from friends list"` -> `GClass2515`
- ~~是否有必要由mod主动开启允许服务端编辑存档~~
- ~~补上order被删除时的手动构造部分~~
- `orderInfo`与`/client/repeatalbeQuests/activityPeriods`仍然存在有bug
- ~~`orderInfo.json`内若有过期order会导致进入游戏加载错误，应该在开启服务端时就进行一次过期函数执行~~
- ~~`ChangeRepeatableQuestPatch`目前还不知道非Order类型的任务是否能够正常更换~~
---
开始偏向客户端
- **实现能够邀请加入队伍的**