# MiyakoCarryService / 宫子护航店

## 低优先级

- 您能加入一个类似于旧版“Friendly PMC”模组的命令系统吗？我希望我们能够向人工智能队友发出直接指令，比如命令他们投掷手雷、攻击敌人、在处于劣势时撤退，或者一旦受到伤害就自动自我治疗

## IDEA

- 同步行动指令：玩家与护航同步扔雷、同步开火
- 最好能使用上大模型，语音识别。游戏中通过语音识别、或扩展指令系统来发出指令
- 解锁红护：护航AI使用魔法子弹、无视障碍索敌，帮你吸物资
- 战局中增配护航功能。如果有护航减员可以在转移点生成其他已点的护航进入战局
- 实现阵型系统，可以在配置管理器里自定义新阵型，并且可以设置每格间隔距离
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

## 已知问题

- *ABPS似乎仍可能导致护航刷不出来*（没复现过）
- *当护航等级较低并进入过其护航库存模式后，跳蚤市场上架会受到护航等级影响没有复原而导致无法上架*（没复现过）
- 当前的护航因为生成的口袋容器不同，会导致不同口袋的装备套装之间无法应用
- 宫子若作为商人与玩家聊天，会留下商人类型的聊天记录，当移除Mcs后就会导致EFT进行商人聊天类型分支的代码随后报错。只要还使用商人类型的消息，这个问题就无法解决
- *`UpdateProfile()`可能因为异步时序导致返回主角色时仍然获取到护航的数据，导致没能成功更新商人信息*（没复现过）

## TODO

- 有些Data在副机是可以不用加载的，需要对各个DataMgr做出区分
- **护航针对迷宫进行适配：避开迷宫陷阱**
- **护航能够跳过绊雷**
- **实现丢出的物品指向时额外增加数个选项：指定某个护航拿取此物品**

## 更新日志

#### 1.0.8.0

- 新增护送至开关
- 新增指令：改变瞄准部位

#### 1.0.7.0

- 新增多种配置选项
- - `BalanceRestriction`: 平衡性限制，开启后将禁用打开背包指令，并会使护航死亡时删除其几乎所有带入战局的物品，默认关闭
- - `TicketPricePerPercent`: 罚单每百分比价格，默认300000卢布/百分比
- - `PunishmentMultiMax`: 涨价惩罚上限，默认为1，即100%
- - `OrderPendingPaymentTime`: 订单/罚单等待付款时间，单位为秒，默认900，即900秒、15分钟
- - `CompensationPrice`: 护航误杀老板赔偿价格，默认300000，即误杀一次赔偿300000卢布
- - `CarryServiceLevelPrice`: 护航等级价格范围，单位为卢布
- 一些修复和优化

#### 1.0.6.0

- 新增护航的击杀经验、部分任务进度共享
- 优化了护航的治疗逻辑
- 修复了罚单在非中文服务端环境时会导致异常的问题、修复护航没有正确进行撤离行为的问题
- 实现护航保存装备预设
- 提高了护航的存款，可用于一键购买套装预设装备

#### 1.0.5.0

- 宫子商人聊天时能够额外进行提醒版本更新
- 优化ActionPanel显示大量选项时的布局
- 改进了和第三方Mod的兼容性、修复了护送指令在使用SAIN时无法生效的问题
- 改进了护送指令的目的地位置精度、修复了护送指令的一些Bug
- 新增玩家信息列表的滚动视图显示
- 新增刷新好友列表选项

#### 1.0.4.1

- 新增指令：护送
- 增加新的短语对话
- 修复"前往"指令在某些情况下无效的问题、修复基础配置在主机没有正确引用的问题、修复某些情况下`McsBotPlayer`会穿门而过的问题、修复特定情况下下发罚单时的异常问题
- 提高与老版本Fika的兼容性问题
- 削弱`McsBotPlayer`并微调其部分逻辑，同时调整了其对敌人信息的处理逻辑
- 大量其他调整

#### 1.0.3.0

- 新增指令：自主行动
- 战斗状态下指令具备更高优先级
- 尝试修复Fika联机载入地图时保留护航玩家信息时出现的异常
- 调整商人任务配置数值，避免被误用后发生异常

#### 1.0.2.0

- Fika联机载入地图时保留护航玩家信息
- 新增Scav模式小队人数检测限制提示

#### 1.0.1.1

- 修复与Fika Headless兼容性Bug

#### 1.0.1.0

- 新增其他语言的本地化内容
- 完善修复Aid重复的问题
- 优化护航的移动
- 进一步优化与SAIN的适配

#### 1.0.0.X 正式版

- null

#### 0.3.6.3

- 优化并修复了护航库存模式在特定情况下会导致的bug
- 使护航生成时自带一定的技能等级

#### 0.3.6.2

- 修复好几个bug

#### 0.3.6.1

- 延后了生成护航的时机。防止因Fika联机时加入房间的速度差异产生的某些时机错失导致没有正常生成护航
- 优化了屏蔽战利品类型的分类
- 大量bug修复
- Boss类型的护航价格不再翻倍

#### 0.3.6.0

- 再次适配Zyriachy，修复其不攻击的问题
- 新增罚单功能。输入对应指令，可花钱消除涨价惩罚，具体方式同样使用`help`指令查看
- 调整护航生成装备的耐久度全部为满值
- 修复安装SAIN下的护航在战斗结束后会卡住无法进行下一步决策的问题

#### 0.3.5.0

- 大幅改进了护航的战利品操作逻辑
- 实现护航在卡脚时尝试翻越和跳跃

#### 0.3.4.0

- 优化了护航的战斗逻辑
- 为防止生成护航时因无枪械武器时而卡死，对护航库存模式返回主角色时增加了合法性检查
- 修复护航库存模式下保存护航存档时的一些Bug

#### 0.3.3.0

- 增加了一些护航的对话
- 配置项新增护航是否进行掠夺开关、护航字幕新增开关是否关闭
- 修复了一些bug，调整了护航的一些行为

#### 0.3.2.0

- 护航个人指令新增打开护航背包
- 实现基础的护航根据要求拾取战利品功能
- 修复Scav模式下会错误地显示投保界面的问题

#### 0.3.1.0

- 为鼓励下单长期护航，护航每小时基础价格`-70%`(手动绿色),当前最贵基础价格为每小时~~100000~~**30000**卢布，惩罚涨价最大值`-80%`(手动绿色), 当前惩罚涨价最大值为~~500%~~**100%**
- 增加新的本地化适配
- 修复`护航库存模式`下的藏身处异常、购买商品后出现某些异常的问题
- 修复生成护航存档时的仓库内物品存放问题
- `护航库存模式`下宫子商人全物品供货适配跳蚤市场，便于更好地对护航的武器装备进行自定义
- 调整了生成护航时的藏身处数据，直接全部设施满级，便于更好地对护航的武器装备进行自定义
- 现在当处于`护航库存模式`下时，直接点击底部绿色标识也可以返回主角色了

#### 0.3.0.0

- 修复带有护航的战局无法使用转移的问题
- 新增`护航库存模式`。你将能够完全自定义护航的一切。
1. 你需要在"邀请至队伍"界面右键护航玩家，点击"打开库存"来切换至`护航库存模式`
2. 处于`护航库存模式`下时，你可以为你的护航做绝大多数平时你能做的事
3. 处于`护航库存模式`下时，宫子商人将提供全物品购买，供你自由地搭配护航的武器装备。（若没有加载则手动刷新商品即可）
4. 若要返回主角色，也是一样通过"邀请至队伍"界面右键护航玩家，点击"返回主角色"来切换回主角色
5. `护航库存模式`主要用于玩家自定义护航的武器装备，且`护航库存模式`是为了后续更新新类型的护航打下基础，因此目前只适合搭配下单长期的护航来使用。

> 本模式影响范围巨大，若发生报错请及时反馈

### 老版本

#### 0.2.5.2

- 尝试修复已完成的任务被退款的问题
- 修复邀请护航进队再踢出队伍时护航状态会显示为邀请中的问题
- 修复战局设置不生效的问题
- 让护航不再会随机丢东西

#### 0.2.5.1

- 当安装SAIN时，无法使用全队强制传送指令
- 调整了与SAIN的兼容性
- 修复了Scav模式下使用护航的很多Bug
- 调整了Scav模式下护航的仇恨机制：不会像Pmc模式下主动进攻所有目标，以避免某些特定类型的护航主动攻击Scav导致老板变成坏兄弟，但也意味着可能会被某些特定类型的敌人主动攻击后护航才会开始反击
- 新增报告敌人方位指令

#### 0.2.5.0

- 实现集结、前往指定地点、驻守指令
- 新增一些护航的对话

#### 0.2.4.0

- 新增误杀老板时触发赔偿30w的机制
- 修复邀请至队伍界面中无法加载护航的问题

#### 0.2.3.0

- 实现基础的护航对指令的回应对话字幕功能
- 再次尝试修复Aid重复问题
- 护航装备中不会再自带任何手雷，以避免误伤
- 提升了传送指令的成功率
- 现在当安装SAIN时，使用传送指令不会消除护航的仇恨，以防止SAIN的AI逻辑卡住

#### 0.2.2.0

- 首次对Fika进行强关联的适配工作，可能存在Bug：传送指令支持Fika联机下的副机使用（特别提醒：AI在做某些事的时候似乎会阻止传送，所以并不是指令本身的问题，以后再尝试解决）

#### 0.2.1.0

- 新增护航高亮

#### 0.2.0.0

- 新增指令系统功能**雏形**（联机状态时当前应该是只有主机才可以完全正常地使用，下一步目标是适配联机，让副机也能使用完全正常地指令）
- 尝试修复异常退款金额的问题
- 其他一些小调整

#### 0.1.9.3

- 新增退款机制（下单之后如果任务未完成或过期，会返回你在该任务中提交的卢布）
- 现在下单之后无需再重开任务界面应该也能刷新出任务了
- 大幅提升护航的准度、反应力
- 再次尝试修复下单后刷不出任务

#### 0.1.9.1

- 调整护航的战斗行为逻辑，下调了护航的进攻性，不再大老远跑去攻击敌人，注重保护老板和清理近距离威胁（注意：安装SAIN后Mcs自带的战斗行为无法使用）
- 再次尝试修复下单刷新不出任务的问题
- 服务端新增了两条调试信息，注意调查问卷的具体内容

#### 0.1.9.0

- 新增了Mcs自己的Pmc装备池（如果有APBS则使用APBS）
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

---

# 短语Note

### 通用/无触发
- **None**：无语音触发。
- **Mooing**：可能是调试用的占位符或玩笑（“哞哞叫”），实际游戏中不太可能出现。
- **PhraseNone = 8**：显式指定值为 8 的“无短语”状态，可能用于对齐或预留。

---

### 战斗/受伤相关反应
- **OnAgony**：角色处于极度痛苦中（如重伤濒死）。
- **OnGoodWork**：表扬队友做得好。
- **OnEnemyGrenade**：发现敌人投掷手雷。
- **OnFirstContact**：首次与敌人接触（发现敌人）。
- **OnLostVisual**：丢失敌人视野。
- **OnFriendlyDown**：友军倒下。
- **OnBeingHurt**：被击中/受伤时的反应。
- **OnBeingHurtDissapoinment**：受伤时表达失望（比如“啊，真倒霉”）。
- **OnEnemyConversation**：听到敌人对话（AI 监听敌方语音）。
- **OnEnemyDown**：敌人被击倒。
- **OnEnemyShot**：听到敌人开枪。
- **OnOutOfAmmo**：弹药耗尽。
- **OnRepeatedContact**：多次遭遇敌人。
- **OnGrenade**：发现手雷（可能是自己或敌人的）。
- **OnWeaponReload**：换弹。
- **OnWeaponJammed**：武器卡壳。
- **OnWeaponMisfired**：武器哑火。
- **OnDeath**：角色死亡。
- **OnFight**：进入战斗状态。
- **OnMutter**：低声嘟囔（可能是随机闲话）。
- **OnBreath**：喘息声（可能在奔跑或紧张时）。

---

### 武器切换
- **OnSwitchToMeleeWeapon = 115**：切换到近战武器（如刀）。

---

### 指令类（战术命令）
这些通常是玩家或 AI 发出的战术指令：
- **CoverMe = 30**：掩护我。
- **FollowMe**：跟我来。
- **GetBack**：后退。
- **GoForward**：前进。
- **Gogogo**：快速推进（常用于突击）。
- **Look**：看那边！
- **HoldPosition**：坚守位置。
- **GoLoot**：去搜刮。
- **Stop**：停止行动。
- **Silence**：保持安静。
- **OnYourOwn**：各自为战 / 靠你自己了。
- **Fire**：开火！
- **HoldFire**：停火！
- **Suppress**：压制火力（让敌人不敢抬头）。
- **Spreadout**：散开。
- **GetInCover**：找掩体！
- **KnifesOnly**：只用刀（近战模式）。
- **Regroup**：重新集结。

---

### 敌情报告
- **LocateHostiles = 114**：定位敌人。

---

### 角色状态（受伤/负面状态）
- **HandBroken = 48**：手部骨折（影响持枪）。
- **LegBroken**：腿部骨折（影响移动）。
- **Bleeding**：正在流血。
- **Dehydrated**：脱水（游戏机制中的负面状态）。
- **Exhausted**：精疲力尽（体力耗尽）。
- **HurtLight**：轻伤。
- **HurtMedium**：中度伤害。
- **HurtHeavy**：重伤。
- **HurtNearDeath**：濒临死亡。

---

### 医疗/交互
- **StartHeal = 106**：开始治疗。

---

### 通用回应/确认
- **DontKnow = 57**：不知道。
- **Clear**：区域安全（清点完毕）。
- **Going**：我去了（回应指令）。
- **Covering**：我在掩护。
- **BadWork**：干得不好（批评）。
- **Negative**：否定（“不行”、“没有”）。
- **Ready**：准备就绪。
- **OnPosition**：已到达指定位置。
- **OnLoot**：正在搜刮。
- **GoodWork**：干得好。
- **Roger**：收到（无线电确认）。
- **Repeat**：重复一遍。

---

### 特殊状态/事件
- **Toxic = 107**：中毒？或指“毒舌”？在《逃离塔科夫》中可能指化学伤害或辐射。
- **Greetings**：打招呼。
- **Warning = 111**：警告！
- **Mine**：这是我的（如战利品）。

---

### 战术方位/敌人类型
- **LeftFlank = 69**：左翼。
- **Scav**：指“拾荒者”（Scavenger，游戏中的 NPC 敌人）。
- **SniperPhrase**：狙击手相关语音（如发现狙击手）。
- **RightFlank**：右翼。
- **InTheFront**：前方有敌人。
- **OnSix**：背后有敌人（“I’ve got your six” 是军事俚语，意为“我掩护你背后”）。

---

### 战斗反馈
- **UnderFire = 105**：遭到火力压制。
- **EnemyDown = 75**：敌人倒下（与 OnEnemyDown 类似，但可能是主动宣告）。
- **ScavDown**：拾荒者被击倒。
- **LostVisual**：再次强调丢失视野（可能用于不同上下文）。
- **EnemyHit**：击中敌人。
- **KnifeKill**：用刀击杀。
- **NoisePhrase**：制造噪音（如故意吸引注意）。

---

### 道德/行为相关（《逃离塔科夫》特有）
- **LowKarmaAttack = 109**：低道德值攻击（如攻击队友或平民）。
- **Provocation**：挑衅。
- **FriendlyFire = 81**：误伤友军。

---

### 简短战斗呼喊
- **Rat**：可能指“老鼠”，贬义称呼敌人。
- **Down**：倒下！（命令或报告）
- **Hit**：命中！

---

### 请求支援/物资
- **NeedFrag**：需要破片手雷。
- **NeedSniper**：需要狙击手支援。
- **NeedAmmo**：需要弹药。
- **NeedHelp**：需要帮助。
- **NeedWeapon**：需要武器。
- **NeedMedkit**：需要医疗包。

---

### 地图/目标相关
- **ExitLocated**：找到撤离点了。
- **LootKey**：找到钥匙。
- **LockedDoor**：门锁了。
- **LootBody**：搜刮尸体。
- **LootContainer**：搜刮容器（箱子等）。
- **LootGeneric**：一般性搜刮。
- **LootMoney**：找到钱。
- **LootWeapon**：找到武器。
- **Cooperation**：合作（可能指与队友协作）。
- **LootNothing**：什么都没搜到。

---

### 装备问题
- **WeaponBroken**：武器损坏。

---

### 交互指令
- **OpenDoor**：开门。
- **CheckHim**：检查他（尸体或可疑人物）。
- **MumblePhrase**：含糊不清的低语（背景语音）。

---

### 特殊状态
- **Frozen = 113**：冻僵了（寒冷环境下的负面状态）。