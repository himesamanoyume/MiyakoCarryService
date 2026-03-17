# MiyakoCarryService / 宫子护航店

- ~~好友位自带一个TsukiyukiMiyako，通过她来进行下单~~（最初直接通过指令）
- 最好能使用上大模型，语音识别。游戏中通过语音识别、或扩展指令系统来发出指令
- ~~拉去好友进队后能够在匹配界面显示~~
- ~~交一点卢布获取好友位，一段时间后会自动删除好友。不同价位的装备、准度也不同~~
> 可参考APBS这种根据玩家等级提升装备和等级
> 可以参考游戏对物品排序的功能是如何修改物品位置的
- ~~最好支持BOSS护航~~
- ~~兼容SAIN、Fika~~
- 最好是自带一部分QuestingBots和LootingBots的功能，可根据交互来选择帮助你。比如我想要什么物品，他就会去帮你找
- **赞助版**：解锁红护/挂护：护航AI使用魔法子弹、无视障碍索敌，帮你吸物资
- - 如果是红护根据价位有不同的概率（通过封号系数决定，可关闭）可能被封号，然后根据你本局的战利品收益进行追缴（可关闭）
- ~~图标：宫子头饰, 后面一个光环~~
- ~~根据好感度等级能够打折扣~~

## 已知问题

- 邀请入队时即便返回了接受邀请的消息，也还是显示着正在邀请（小问题）

## 低优先级 | IDEA

- 增加退款机制，如果订单任务没有完成，则需要将交了的钱还回去
- - 可以是当任务失败或更换时根据该任务已完成的进度来获取总计上交的卢布，然后进行退款
- AI恢复无限体力、耐力。如果不开启的话需要Patch生成Bot时往保险箱塞太多物品的代码，这会导致重量太大使AI总是会想丢东西
- `[Info   : Fika.Core] Sending bot operation GClass3513 from KokaZ93`是否可以利用
- - **当前适配Fika的手段是：额外一个新的Mod，专门用于适配Fika发送指令、字幕**
> 参考[HeliCrash](https://github.com/ArysWasTaken/SamSWAT.HeliCrash.ArysReloaded)的Core和Fika部分

> [Fika Wiki](https://wiki.project-fika.com/modding-fika)

> 和[WTTClientCommonLib](https://github.com/WelcomeToTarkov/WTT-CommonLib/blob/main/WTT-ClientCommonLib/WTTClientCommonLib.cs)
- Fika联机下，护航会拿走玩家的物品去埋包
- 移除Bot的臂章
- 想办法实现武器的占用格数的计算
- 需要实现老板距离Boss超一定距离且一段时间后传送护航至老板处
- 修改指令系统数组`PredefinedLayoutGroup.PositionsToCenter`
- - `(-321, 241)，(0, 315)，(321, 241)，(492, 55)，(433, -158)，(171, -296)，(-171, -296)，(-433, -158)，(-492, 55)，(-321, 241)，（0, 150）,（0, -100）`
- - *关于指令系统和字幕系统，需要等待到有合适的联机同步方法后，才可继续推进*
- `GClass117`为AI掠夺战利品的Layer，需要适时进行参考学习
- 适配联机部分时
- - 实现配置管理器显示默认配置项
- - 包含物品收集筛选器：是否需要帮忙找老板的任务物品（特殊的任务物品不能拿，基本都是无价值物品吧。但是好像也未必一定不能丢出来），老板的愿望单物品，老板的高价值物品，老板的特定物品（尝试实现全物品搜索器）。这些开关都可以按需关闭
- - 实现配置管理器调整上缴物品阈值，只有当护航获取了配置以上总价值的物品时，就会跑到老板前方丢出背包、或背心
- - 实现配置管理器调整护甲过滤内部/插板的防护等级，武器筛选只要其中某几种武器类型
- - 战局开始前，想办法收集每个人的护航配置，随后由房主接收汇总并使配置生效

- 掠夺第一阶段重做
- 1. 绿护需要逐个对每个物品的RootItem进行一次检视，检视会将内部的物品全部记录，检视情况全队共通
- 2. 实际上还是根据战利品价值优先顺序去走，走到并检视后，说话提醒，在老板处于安全距离内只要敌人不出现，就停留在此直到老板接近，当然如果老板走远了就只停留2秒
- 3. 检视过的物品如果被要求带路，则先判断当前可以选择的目标其RootTransform是否还在，不在说明已经变动，则将此LootData从已检视过的物品中移除
- 4. 可以通过快捷键触发多个Action选项来选择全体护航或单个护航(后期实现: 也可以通过对着活着的护航)，并以此对指定护航下达指令，如带我找高价值战利品，此时指定数量的护航就会去带路找出已检视过的其中内含目标战利品的RootItem位置，然后说话：这里有符合XX条件的战利品，XX
- - 最顶部选项除非已是最顶层否则固定为上一级，最底部选项一定是取消
- 实现护航根据已检视过的战利品做出后续行为（掠夺第二阶段）
- - 1. 根据自身是否缺医疗物品、手术包去进行掠夺对应物品
- - 1. 根据自身是否缺水缺能量、去进行掠夺对应物品
- - 3. 根据自身已掠夺的战利品价值，判断是否应该上贡
- 实现太接近老板时自己让开
- 实现阵型系统，可以在配置管理器里自定义新阵型，每格间隔
- - `-1`代表自身，`1,2,3,4`代表
```
[
  0, 1, 0, 0, 0,
  0, 0, 2, 0, 0,
  0, 0, 0, 3, 0,
  0, -1, 0, 0, 4,
  0, 0, 0, 0, 0
]
```

## 疑难杂症

- - ~~当前只要导致切到过一次scav，再切回Pmc时，仍会以scav状态进入~~
- - - 竟然是`MatchmakerAcceptScreenShowPatch.Postfix`,`___raidSettings_0.RaidMode = ERaidMode.Local;`导致的
- - 无`___raidSettings_0.RaidMode = ERaidMode.Local`时, 战局设置不生效
- - 无`___raidSettings_0.RaidMode = ERaidMode.Local`, 有`MatchMakerAcceptScreenReadyStatusPatch`时，战局设置不生效
- 有`___raidSettings_0.RaidMode = ERaidMode.Local`和`___eraidMode_0 = ERaidMode.Local`时，先选scav再选pmc会以scav进
- - 单人状态不论pmc/scav下不改设置RaidMode为Online，改了战局设置就会变成Local
- - 组队状态scav不改设置为Online，改设置为Local，pmc则改不改都是online
- - 那么理论上scav组队改设置进战局，应该是不会刷新bot的（确实）
- - ~~而pmc因为组队时无论如何改设置都是online，所以战局设置无法生效，而强行设置为Local就会触发scav的Local设置而以scav进入~~即便让组队时不强制为Online也无效
- `GClass3926<T>.Gparam_1`(实质为GClass3926<RaidSettings>)这个RaidSettings仍然`isPmc`为`false`,`isScav`为`true`的原因
- 其直接原因是`RaidSettings.Apply`被调用，即Side为`Savage`的设置覆盖了原本isPmc为true的设置
- - `MainMenuControllerClass.method_27`中因为`if (this.RaidSettings_0.RaidMode != ERaidMode.Online && !this.RaidSettings_0.IsPveOffline)`因此最终调用了一次`this.RaidSettings_0.Apply(this.RaidSettings_1);`导致覆盖了Scav的设置
- 目前打算是弄清楚在不将RaidMode设为Local，组队不强制为Online的情况下，是什么原因导致战局设置无效
- - 观察`MatchMakerAcceptScreen.Show`在不设Local，不强制Online的情况下，会传进来什么raidSettings的RaidMode,正常来说组队调整设置的情况下应该为Local，如果还是传入Online那可能局势会明朗很多（确实还是Online）
- 打断点`UpdateMatchmakerSettings`可最清晰观察到点击开始战局后的一次该函数调用
- ~~尝试记录`MatchMakerPlayerPreview`中的玩家Side，只根据此阵营来在`GClass3926<RaidSettings>.UpdateMatchmakerSettings`选择RaidSettings~~

## TODO

```md
- ~~实现好友位的TsukiyukiMiyako~~
- 实现~~自定义商人~~和~~发放行动任务~~
- - ~~弄清如何构建行动任务~~
- - ~~实现临时指令来触发发放行动任务~~
- - ~~根据人数修改条件数量~~。
> 参考`GenerateAvailableForFinish`
- - ~~别忘了修改任务接取过期时长为15分钟~~
- - 新的AI队友处于好友列表的时长才为指定的时间
- ~~能否改掉寻物上交这些内容~~
- ~~确认一下现在的Order类型任务是否还能更改任务~~
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
- ~~实现能够邀请加入队伍的~~
> `SendGroupInvite`, `AcceptGroupInvite`, `acceptInvite`, `"/client/match/group/invite/send"`
- ~~BUG:在拥有过期护航存档、过期订单信息时，新开启服务端时没有把过期护航存档删除~~
- ~~并且需要一并将好友删除~~
- ~~BUG:加载不了好友Miyako~~
- ~~BUG:由于Patch了接受小队邀请函数，导致拉取ChatBot时会报错~~
- - ~~护航没有准备~~
> `[未处理] client/match/raid/ready | not-ready`, 或直接`参考friendlypmc`
- ~~如果在将护航拉入小队中时过期清除删好友了，则应该一并让护航退队~~
> 参考`GClass2512`, 以及似乎需要自建ws record类，必需要aid, nickname
- ~~Ws相关消息的构建转移到MCSNotifyXXXHelper中进行~~
- ~~服务端启动时，检查存档，若`orderinfo.json`中无对应的订单号，则应该将存档中的friends清除~~
- - 因为当存档中有好友，但是orderinfo.json内没有订单时，就无法正确移除好友
- ~~BUG:若服务端不关闭，在玩家下线之时订单过期，护航删好友，此时玩家上线，应该会触发一次服务端的检测机制来将好友移除，此时会导致一次服务端致命报错~~
开始进入客户端为主的阶段
- ~~移除Fika，实现原生的开始战局~~
- - ~~若使用`RaidSettingsLocalPatch`, 则会使原本服务端传来的准备就绪状态不被使用,导致匹配界面的准备按钮无法交互~~
> 参考friendlyPMC `MatchmakerPlayerControllerClassAddMemberPatch`, `MainMenuControllerPatch`, 特别是`MainMenuControllerPatch.GroupPlayers`
- ~~实现在战局中生成拉入小队的护航~~
> 参考friendlyPMC如何实现BOT的生成
- - ~~应该Patch玩家的邀请入队请求，若对象为护航则应该在`mcsRaidService`中被`AddGroupMember`~~, 本地战局开始时，包括使用Fika联机时，则根据战局中的所有加入玩家调取其sessionId的护航小队成员来依次返回护航的BotBase数据进行生成
- ~~实现检查Fika是否存在~~
- - ~~postPatch`BotsEventsController.SpawnAction`函数进行异步的从服务端`/mcs/client/game/bot/generate`获取护航bot数据~~
- - - ~~若使用Fika，则需要服务端获取Fika的`MatchServices`的`Matches`，并根据其找到指定的Match，再获得其中所有Player的MongoId，以此获取这些老板的全部护航~~
> 参考战局开始回调`locationLifecycleService.StartLocalRaid`
- - ~~应该Patch战局结束、重新进入游戏时的老板sessionId并在`mcsRaidService`中`ClearGroupMember`~~
- - ~~并且踢出队伍的对象若为护航也要进行在`mcsRaidService`中的`RemoveGroupMember`~~
> 参考战局结束回调`locationLifecycleService.EndLocalRaid`，但要注意先判断是否为转移，如果是则不进行Clear
- ~~去除MCS类的前缀~~
- ~~对生成Bot情况进行Debug~~
- - ~~SpawnProfileData为null~~
- - ~~推测原因是ActivateBot没有使用到任何botCreationDataClass的_profileData~~
- ~~BUG:`BotOwner.StartCorePoint`为null~~
- ~~生成的AI不与我同一位置~~
- ~~AI仍对我抱有敌意~~
- - ~~BUG: 虽然其中一个能做到友好，但一个以上时不会，且会一直盯着我~~
- - ~~剔除队友无效，说明当前其仍然不是真正意义上的队友，只是其对我态度为友好~~
- - ~~对其开了一枪之后就会恢复敌意，可能AddEnemyPatch不是那么好用~~
- ~~以实现AI与我同队，无敌意，会跟随我，能正常攻击其他AI~~ 作为第一阶段目标
- - ~~主机和护航的BotsGroup为null, 需要填补~~
- - ~~研究SAIN是如何实现屏蔽原生的CombatLayer的?并使用SAINComponents与BotOwner并行执行的~~
> `StandartBotBrain.Activate()`中有完整的Brain与WildSpawnType的对应关系
- - - 通过`BrainManager`对指定的Brain列表添加自定义的Layer
- - ~~弄清优先级数字是如何影响决策的~~
- ~~OnGameStarted需要改到GameWorld被创建之后~~
- ~~服务端generate返回的内容需要额外附带此Bot的老板信息~~
- ~~BUG: 护航的BotsGroup似乎与我不同，之前是否相同?~~
- ~~BUG: 目前所有人都不会攻击我~~
- ~~BUG: BotsGroup还是会将老板加入至Enemy中，只是会被清除记忆~~
- 现在看来的想法是：不添加任何Brain，创建自己的自定义Layer，~~只收集属于Mcs生成的BotOwner，然后借鉴`BigBrain.BrainManager`的做法再将Layer添加至这几个BotOwner当中~~(长远来看可以让所有Brain都加入Layer)，以实现对AI能够兼容SAIN的战斗Layer的同时，还能执行自己的一些Layer以实现会跟随自己
- ~~具体实现FollowMcsLeadLayer~~
- - 当前`IsActive`判断相当宽松，只要是护航 就一定会一直停留在这个Layer，需要优化
- - - ~~如何高性能地判断玩家是否是护航玩家呢?friendlyPmc是通过替换Brain，但我是不打算替换Brain的，这就有可能不太适用`BotOwnerExtensions`获取McsData~~
- - ~~复用NotCheater`BaseData`及其子类，并用`McsBotPlayerData`继承`PlayerData`~~
- - 总共需要设计跟随、巡逻、掠夺、丢出指定物品(如高价值物品、医疗物品、吃喝)给老板、帮老板理包5大主要行为，并且这些都或多或少可从本体代码中得到参考
> 重点从`GClass133`(PatrolAssault Layer)中获取灵感
- - 参考`PatrollingData`、`PatrolLootPointsData`(巡逻、掠夺有关)；
- 如果是队友的手雷，AI不会进行躲避
- - 需要实现躲避队友带来的危险的Logic
- ~~如果AddEnemyPatch执行非常频繁，则应该想办法避免~~
> `BotNodeAbstractClass CreateNode`写明有`BotLogicDecision`的对应Logic
- `friendlyPmc`的`BotFollowerPlayer.Init()`中写有移除`skwizzy.LootingBots`和`SPTQuestingBots`的代码
- `FollowerPatrolBaseLogic`作为巡逻Logic，但其需要`BotFollower`相关信息，应该不能直接使用。`friendlyPmc`中也是需要先判断`BotFollower.HaveBoss`且`BossToFollow.IsMe`为true才执行的
- - ~~找出如何设置BotFollower为McsLeadPlayer~~
- - - `AIBossPlayer.cs`的`AddFollower`
- - - ~~必须要实现McsAILeadPlayer才能设置BotFollower~~
- ~~BUG:当没有设置护航时，就会造成generate错误~~
- 复刻`FollowerLayer`的`GetDecision`函数中的决策转换为`Logic`后至`McsBotPlayerCommonLayer`中
> 其中诸如`BotLogicDecision.runToCover`将得到的决策Action可以从`global using RunToCoverBaseLogic = GClass228;`找到对应原版的基础Logic
- ~~生成的护航第一时间Layer是无法获取的，可能需要参考BotDebug来获取BigBrain相关的Layer才行~~(看来是单纯的Layer没写对)
- 如果不加CustomLayer，是否还能正确跟随（可以，但需要自己先遇敌，他们才会跑过来，否则会先在四周巡逻，且会远离boss）
- ~~BUG：如果发生了异常，会导致某个护航存档未能被删除，然后启动服务端之后还会一直被加载，显示在好友列表中~~
- - ~~调整加载存档的逻辑，只有在orderinfo中PlayerIds内有的id才能够被加载至服务端，同时还需要检查所有文件夹内的护航存档文件，如果发现其文件名未处于orderinfo内，则需要将该护航存档删除~~
- - ~~如果不修复，则会导致只要随便放置一个存档在profiles中，就能得到一个永久生效的护航~~
- ~~BUG：当尝试下订单生成2个护航，哪怕不拉人进队，只要进入战局，就会生成所有好友中的护航~~
- 生成的护航开局傻傻的，感觉是自定义Layer的问题（是。必须先击中其一枪，他就会从此Layer中离开到原版Layer，但是后续再也没见过回到CustomLayer）
- ~~基本进入到AI的逻辑编写阶段，但还没完全弄清楚这个Layer到底如何生效，是否必须要Remove一个Layer?~~
- ~~总之模仿`BrainTest.PatrolAssaultLayer`把所有Logic套上去再说~~
- ~~BUG:BotOwner.BotFollower.BossToFollow.PatrollingData为null~~(没问题，因为本人玩家不是AI，不会有PatrollingData，因此需要避免让Bot调用玩家的PatrollingData)
- ~~当前Layer会使护航永远激活，导致不会进入其他Layer来接敌~~，当然也可以直接让Layer自带接敌Logic以实现高强度的瞄准能力
- ~~让Bot无敌，随后让其与排山倒海敌人战斗，看其是否会自动从安全箱中拿子弹，如果不会，就需要自己手动~~（会拿，因此子弹还是有限的）
- ~~让Bot免疫友伤~~
- ~~参考SAIN实现避免危险Layer~~（既然没有友伤了就不需要此Layer了）
- ~~抄袭`SetFollowerSettings`修改Bot的参数~~
- 如何修改Bot的安全箱
> `friendlyPmc的AddExtraAmmoForWeapon()`
- ~~找出让Bot生成时调整其安全箱的代码~~（在服务端生成bot存档时就已经一并生成Boss安全箱和弹药等药品了）
- 尝试在服务端额外添加针剂包和针剂(搁置)
> 参考服务端`BotLootGenerator.GenerateLoot()` -> `AddForcedMedicalItemsToPmcSecure()`
- 尝试让护航学会使用针剂(搁置)
- `AddEnemyPatch`看代码似乎可以移除，但是可能以后还会在联机时被用上
- ~~尝试参考`friendlyPmc`看是否有自身处于护航枪线上防止开枪误伤的代码~~
- - ~~将代码上传至github，让deepwiki帮我检查~~
- ~~检查新的Scav存档是否有安全箱内容，以及修复生成护航Bot时的异常~~
- ~~尝试移除Layer，看是否即便没有这些普通的Action是否也能正常跟随~~不太能
- ~~根据思想钢印开始进行Logic细化~~
- - ~~继续学习Bot如何对物品进行操作~~
- GameLoop间隔对每个护航进行一次周围检测
- ~~Aiming设置仍需调整~~
- ~~`BotOwner.Boss.IamBoss`不应该为true~~
- ~~`BossFollowLogic`不好用，检查为何~~
- - ~~应以"distToBoss"的Logic为主~~
- Layer不要保留待在掩体的
- ~~邀请入队后队员的图标都变成默认了，尝试优化~~
- ~~Bug:如果先将护航拉入小队，再移交队长，自己退出小队后再邀请就会被认为护航仍在小队中，不会接受~~
- ~~先实现全场的战利品数据更新、护航的检索相关功能~~
- - ~~实现LootDataMgr和PlayerDataMgr的allDatas更新~~
- - ~~先让代码正常运作~~
- ~~HoldPosition不能完全满足要求, 额外参考`GClass145.method_18()`~~
- ~~BUG:在`PlayerDataMgr.RefreshMcsBotPlayersInterestingLoop`中`ItemData.get_Transform`可能异常~~
- ~~BUG：`McsBotPlayerData`的`McsAILeadPlayer`为null, 其实是因为`GetMcsAILeadPlayerByMcsLeadId`没有正确传递bossId的参数，而是传成了此player自身的Id~~
- 在周围巡逻时应该尽量让护航与老板处于同一高度
- ~~检查`GetRangeOwnerItemData`~~(会重复)
- ~~还是没有实现好跟随~~
- ~~护航总是对近在咫尺的敌人视而不见~~
- - ~~现在变成不攻击了~~
- ~~学习以下Patch~~
- ~~应该替换GoToXX改为RunToXX~~
- ~~补全`AddEnemyPatch`使其可以阻止不合理的敌对关系建立、并建立双向敌对关系~~
- ~~护航攻击性太低，不应该总是待在老板附近，而是应该主动出击~~
- ~~护航会因为战局时间自己跑去撤离~~
- ~~对Action继承，以实现字幕检查当前Type是否未变化，变化时才会出字幕~~
- ~~实现单人下的字幕系统~~
- - ~~对每个McsBotPlayer都实例化一个专用的字幕~~
- - ~~就剩下字幕内容没有正确显示，以及位置和展示时机可能有问题~~
- ~~BUG:当前非常容易出现Memory异常~~(好像与McsExfiltrationLayer有关)
- ~~BUG:非常容易出现持续射击尸体的情况~~(计算敌人bug没了后这个也没了)
- ~~BUG:似乎带护航开无Bot生成会导致Bot依旧生成~~
- ~~先修复Bug，再适配转移、Scav模式、SAIN适配、完成Fika适配、~~
- ~~转移时会导致小队成员信息丢失，战局开始时也不会生成小队成员~~
- ~~拉护航进队时不要直接准备就绪，而是等到进入准备界面再准备就绪~~
- ~~为什么现在进不了准备界面?~~
- ~~重新加载玩家模型资源后图标又会消失~~
- ~~开始战局又取消时应该发送请求清理小队~~
- ~~护航死后，不应该在转移后还能再次生成，应记录下死亡情况，当再次获取小队信息时则跳过死亡的成员~~
- ~~Scav模式下，似乎会由于无法获取到bossPlayers，而导致无护航生成~~
- - ~~由于Scav模式下bossPlayerId是Pmc的Id，因此无法从GameWorld中获取到scavId的Player~~
- - ~~生成Bot时要根据是否是scav状态来发送对应Profile，老板的id也需要是Scav的Id~~
- ~~现在是会生成一个与老板完全相同的Bot，同时无法生成护航~~
- - ~~原因是现在生成的Scav Id跟老板的Scav Id是相同的。需要调整Scav生成的方式~~
- ~~当前Scav已可以正常生成护航，但是转移时明明正确完成了转移成员的添加，却依然不会展示Scav成员~~
- ~~如果在没开启服务端的时候订单任务过期了，就会导致服务端认为有删除的商人而使存档被标记~~
- ~~解散小队报错~~
- ~~当同一个老板有两个订单时导致字典重复添加~~
- ~~清除存档报错~~(已尝试修复，但效果难以验证)
- 已经拒绝的入队邀请还会额外发送一个已邀请成功，这不应该
- ~~生成的护航存档名称不是指定名单中的人~~
- ~~Fika下生成Bot报错~~
- ~~健壮客户端没成功接收到机器人存档时的异常处理~~
- ~~Fika副机报错~~
- ~~fika下AddEnemyPatch会导致溢出~~
- ~~实现服务端本地化~~
- ~~让宫子的聊天消息能够有头像~~
- - ~~继承`IDialogueChatBot`重新实现一个机器人~~
- ~~换个头像再发布~~
- ~~实现生成Boss~~
- - ~~Scav的存档没有变成Boss存档~~
- ~~修改聊天机器人的名称，然后Patch(如果有必要)名称使其以本地化内容显示聊天信息~~
> 参考`GetDialogueUsers`, `GetDialogByIdFromProfile`
- ~~修复与护航聊天会导致User内为空~~
- ~~APBS生成Bot时添加额外字段Tier报错~~
- ~~BUG:第二次进行动任务就完成不了下单的任务~~(无法复现)
- ~~Mcs也新增一个大调查，点击直接复制错误日志到剪贴板~~
- ~~BUG:其他人没拉护航就不会被判定为老板，导致护航攻击联机玩家~~
- 快速压弹可能导致触发更新Data数据过于频繁
- ~~BUG:一准备就服务器繁忙~~
- ~~下单护航的价格不再几乎免费，而是恢复为需要正常的价格~~
- ~~help新插入一条价格表至2，同时Bot类型中标注出Boss类型~~
- ~~计算currentRequestedItemCount的算法可以优化成1条~~
- ~~新增一个机制：每误伤到一次护航，宫子商人好感-0.15；每误杀一个护航，宫子商人好感-1.56。若宫子商人好感低于0，则下单价格会翻10.7倍，但是下单获得的好感会翻倍。宫子商人好感从非负数变成负数时，所有已成为好友的护航会全部主动删除好友~~
```
- 反馈:1、有次我下单了但是没任务，我退出重开服务端报错没找到护航存档，第二次重开就正常了，手快没截图。2、~~ai锁尸体了站着不动射大老远的尸体。~~3、~~会打BTR~~
- - 查清ai锁尸体的原因(set_GoalEnemy属性中发生异常导致报错)
- ~~BUG:存在一个未接受的订单，在护航存档没实际生成时主动进行订单过期处理会尝试删除所有订单，其中未开始的订单里的存档id仍会被收集，最终导致该存档发生无法找到异常~~
- ~~取消信用机制，该为下单永久全局涨价~~
- ~~对机器人可用护航类型的用json加载，以此实现非硬编码的列表展示~~
~~BUG: 服务端和客户端的BotDifficulty不同，导致会出现服务端的难度字符串导入到客户端中：2级服务端`medium`，客户端`normal`~~
- *如何小队数量已经为4个人，此时却还有添加小队函数被执行的话，就直接清空小队再加入（已尝试修复）*
- ~~当召唤的护航是Savage类型时似乎主动攻击同Savage阵营的敌人，只有当玩家或护航被其攻击后才会视为敌人~~
- ~~优化`Side`的选择逻辑，当前支持其他类型的`WildSpawnType`后，Common已经不足以反应Side是否该为`Savage`和`Bear`,`Usec`了~~
- ~~Zryachiy似乎会攻击护航~~
- ~~护航TK护航会惩罚老板~~
- ~~修复小队邀请面板的Aid重复问题~~
- ~~更新赞助者名单~~
- ~~BUG:惩罚的值为负值时会报错~~
- ~~护航识别敌人不够灵敏，因为进入到了原版的Layer中，某些Layer不会主动出击，导致原版Layer迟迟无法退出~~
- - ~~可能需要添加Mcs的战斗层级，以主动出击为目的并且需要高优先级~~
- - ~~找一个原版的通用战斗层级然后在没有SAIN的情况下进行添加至Brain中~~
- ~~在原地HoldPosition3秒后需要换一个位置~~
- ~~即便所有类型都加入了敌对，Killa在遇到某些Boss时仍不会主动攻击(BossBully)~~
- ~~`AvoidDanger`的`BewareBTR`似乎会导致护航完全停止行动，应该想办法处理~~
- ~~想办法让护航在无法射击敌人时冲刺前往地点~~
- ~~增加敌人优先级机制，让护航集火特定目标~~
- ~~开局护航会自带仇恨，应该先清除一下~~
- ~~卡脚太久的护航进行传送~~
- 我发现其实原版在小队成员超出4人时，Scav角色会显示人数超额，但这只在Scav冷却中才会显示，是否可以利用
- 先恢复说话，然后立即开始Fika兼容的新dll的开发，确定一系列的同步流程后，再进行后续计划

## Logic思想指导

- 思想钢印：跟随、巡逻、**掠夺、丢出指定物品(如高价值物品、医疗物品、吃喝)给老板、帮老板理包**5大主要行为
- - `HealAnotherTargetBaseLogic`治疗他人，可能需要
- 护航能够开所有的门，为护航添加订阅老板的射线检测的事件，检测老板Ray的可交互物体是否是上锁的门，如果是就要跑去帮老板开门
- 当需要吃喝、医疗品时要能够在安全的情况下寻找周围的吃喝、医疗品并使用。因此这也将能够做到你丢出医疗品给他，他会自己去取来用
- - 应当以物品每格子平均价值作为标准，尽可能将高单格价值的物品拿走
- ~~往安全箱里塞针剂包，让护航学会打针(搁置)~~

### 更新日志

#### 0.1.8.0

- 通过新增战斗层级、集火机制，调整AI参数、提高索敌能力，大幅提升了多数护航类型的攻击性、侵略性
- 尝试修复护航锁尸体的问题
- 好友位的护航名称新增护航类型后缀

#### 0.1.7.3

- 尝试修复宫子商人涨价可能导致的一些报错
- 尝试修复`SPT`指令聊天机器人没有显示在好友列表的问题
- 尝试修复护航攻击护航时会触发涨价惩罚的问题
- 调整涨价惩罚数值，现在下限为0%，上限为500%
- 尝试修复BTR、Zryachiy与护航之间会相互攻击的问题
- 尝试修复当生成护航等级2级的护航时会报错无法生成的问题
- 新增新版本检查更新机制、赞助者名单检查更新机制

#### 0.1.7.0

- 开放其他护航类型（默认支持Kaban, Reshala, Glukhar,Killa, Knight, BigPipe, BirdEye, Kollontay, Shturman, Sanitar, Tagilla, Partisan, Zryachiy, Tagilla Agro, Killa Agro, Infected Tagilla, Rouge, Infected Pmc, Infected Assault, Raider）
- 支持生成自定义护航类型（目标是允许生成第三方Mod提供的AI类型，比如BlackDivision，此功能需要重点测试）
- 下单护航的价格不再几乎免费，而是恢复为需要正常的价格
- 新增误伤惩罚机制：每误伤到一次护航，宫子商人下单价格永久涨价1.07%；每误杀一个护航，宫子商人下单价格永久涨价15.6%，此数值将会不断累加。
每误杀一个护航，若击杀者为老板，则对该老板进行惩罚，其所有订单立即提前过期，所有已加为好友的护航会立刻主动删除好友（但处于战局中的护航不会立即消失）；若击杀者不是老板，但是是真人玩家，则对战局内所有拥有订单的玩家都进行立即过期、删除好友惩罚。
- - 本机制在于维持真实组队情况下需要尽量避免TeamKill的游玩环境，请像线上组队时一样注意记忆队友的装备，做好各种防TeamKill措施
- 为了平衡护航不像真实队友一样可以报点的缺陷，后续版本将会给予护航一定的交流机制以供老板分辨身份

#### 0.1.5.X

- 修复APBS兼容问题
- 底部任务栏右下角新增了一个收集日志的按钮，游戏过程中当你看到有提示报错了，点击一下就会复制错误日志文本，然后发到Discord里来
- 修复Fika联机时，副机的真人玩家如果没有点护航就会被其他护航攻击的问题
- 尝试修复Fika联机下副机护航在第一次撤离后第二次无法被邀请入队的问题（如果解决了记得发出来告知我）
- 修复战局开始报错问题

# WildSpawnType

每个类型对应谁自己查

## 原版

- marksman
- assault
- bossTest
- bossBully
- followerTest
- followerBully
- bossKilla
- bossKojaniy
- followerKojaniy
- pmcBot
- cursedAssault
- bossGluhar
- followerGluharAssault
- followerGluharSecurity
- followerGluharScout
- followerGluharSnipe
- followerSanitar
- bossSanitar
- test
- assaultGroup
- sectantWarrior
- sectantPriest
- bossTagilla
- followerTagilla
- exUsec
- gifter
- bossKnight
- followerBigPipe
- followerBirdEye
- bossZryachiy
- followerZryachiy
- bossBoar
- followerBoar
- arenaFighter
- arenaFighterEvent
- bossBoarSniper
- crazyAssaultEvent
- peacefullZryachiyEvent
- sectactPriestEvent
- ravangeZryachiyEvent
- followerBoarClose1
- followerBoarClose2
- bossKolontay
- followerKolontayAssault
- followerKolontaySecurity
- shooterBTR
- bossPartisan
- spiritWinter
- spiritSpring
- peacemaker
- pmcBEAR
- pmcUSEC
- skier
- sectantPredvestnik
- sectantPrizrak
- sectantOni
- infectedAssault
- infectedPmc
- infectedCivil
- infectedLaborant
- infectedTagilla
- bossTagillaAgro
- bossKillaAgro
- tagillaHelperAgro

## WTT - Black Division Home

> Author: TacticalToaster [Forge](https://forge.sp-tarkov.com/mod/2511/wtt-black-division-redacted-home)

- blackDivLead
- blackDivAssault
- blackDivBreacher
- blackDivSupport

## UNTAR Go Home!

> Author: TacticalToaster [Forge](https://forge.sp-tarkov.com/mod/2342/untar-go-home)

- followeruntar
- bossuntarlead
- followeruntarmarksman
- bossuntaroffice

## RUAF Come Home!

> Author: TacticalToaster [Forge](https://forge.sp-tarkov.com/mod/2427/ruaf-come-home)

- ruafRifleman
- ruafRiflemanSenior
- ruafAutorifleman
- ruafGrenadier
- ruafMarksman
- ruafMachinegunne