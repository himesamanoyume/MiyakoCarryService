# MiyakoCarryService / 宫子护航店

- [Forge描述示例](https://github.com/sp-tarkov/forge/blob/c7bf0e31232205d351afc954450c40fcc50f0b3f/resources/markdown/exampleModDescription.md)

其中以下代码为Tab导航栏
```
## Image Tabset {.tabset}

### Minion Image

![Minion](https://octodex.github.com/images/minion.png)

### Stormtroopocat

![Stormtroopocat](https://octodex.github.com/images/stormtroopocat.jpg 'The Stormtroopocat')

### Image Footnote Format

Like links, Images also have a footnote style syntax

![Alt text][id]

With a reference later in the document defining the URL location.

[id]: https://octodex.github.com/images/dojocat.jpg 'The Dojocat'

{.endtabset}
```

## 已知问题

- null

## 低优先级

- 成为打手。大型更新
- - 需要接取考核任务：比如需要下图击杀指定数量Pmc
- - 考核完成服务端记录下通过考核的id，才能进行接单。发送指令开始接单。`mcs exam 5` 代表考取护航级别5的护航
- - - 护航级别有等级要求，20~30,30~40,40~50,50~60,60~79(根据当时实际生成的AI护航等级来限定)
- - - 考核任务一样有费用，是给考官的
- - - 接取考核任务后，会有考官`姫様の夢`加入好友列表，无装备，需要带着考官一起进图，考官进图就会自杀
- - Fika联机下，新增真人护航订单：在宫子消息发送`mcs order 2 -1 5 1` -1类型则为人数2人，真人玩家类型，1小时
- - Fika联机下，真人玩家在宫子商人发送开始接单的指令后，若有另一个真人玩家下单真人护航，则会广播给所有在线的真人护航并附带一串代码，真人护航输入代码视为接单
- - 接单之后，真人护航获取到一个任务，X小时内不得死亡（任务条件可以尝试使用到达指定地点，或者自己新增条件，若触发服务端战斗结算时撤离状态为迷失或死亡一定次数，或是老板死亡一定次数，则触发炸单，记录炸单时间），至少撤离成功X次(1小时1次)，每次进入战局时，若判断到老板也处于战局中，则任务条件+1。
- - AI老板会去随机做任务，吃周围的战利品，遭遇战斗会躲进掩体，不会主动进攻，需要保证其不死亡
- - 炸单在一定时间内达到一定次数会被暂时取消接单，需要再通过一次同级别考核

## IDEA

- 实现护航自动寻找食物、医疗品
- 最好能使用上大模型，语音识别。游戏中通过语音识别、或扩展指令系统来发出指令
- 解锁红护：护航AI使用魔法子弹、无视障碍索敌，帮你吸物资
- 我发现其实原版在小队成员超出4人时，Scav角色会显示人数超额，但这只在Scav冷却中才会显示，是否可以利用
- 战局中增配护航功能吧。。。如果有护航减员可以在转移点重新增添进来
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

## 疑难杂症

- **平衡隐患：拥有了护航库存模式宫子商人0元购，战局中打开护航背包指令后，实际上玩家可以通过护航带入大量物品并能十分轻易地转移全物品**
- **突然发现虽然支持生成第三方AI类型，但是实际上忘记将Layer添加到其类型中了，需要补全相关逻辑。如果第三方的AI没有使用到自带的Brain的话，就会无法作为护航行动(在spawntype中加入string类型的自定义BrainName，并且初始化BrainMgr时从服务端接收自定义BrainName)**
- **小鹿还是不会攻击，只会跟随**
- **下单后刷不出任务的问题，疑似还在，数个版本后再发起一次调查问卷吧**
- **安装SAIN后护航战斗结束后经常卡手。背包显示他正在操作某些东西，但是卡住不动**

## TODO

- **当护航锁定目标战利品时，若RootItem为容器，则应该检索检查内部是否有其他符合要求的战利品，然后也一并进行拾取**
- **交罚款消惩罚涨价，mcs xxx 15 => 消除15%的涨价，罚150w，1%/10w**
- - 要根据当前涨价来生成罚款任务，比如当前只有11%涨价，若指令下发50%的罚款任务，实际也只会生成11%的罚款任务
- **落实护航级别在AI能力数值上的差距，还有生成等级范围重新设定**
- 需要借鉴SAIN实现护航翻越
- 实现太接近老板时自己让开
- 重新实现Custom生成护航装备时的耐久度函数以满值生成
- ~~BUG: 转移物品后东西消失了~~
- 两个容器之间，如果外部容器的MaxSingleGridCount大于等于当前容器的ItemGridCount，且当前容器具有IsContainerWithAdditionalGrid，那么应该尝试将当前容器放入外部容器

## Logic思想指导

- 思想钢印：跟随、巡逻、**帮老板理包**
- - `HealAnotherTargetBaseLogic`治疗他人，可能需要
- 护航能够开所有的门，为护航添加订阅老板的射线检测的事件，检测老板Ray的可交互物体是否是上锁的门，如果是就要跑去帮老板开门
- 当需要吃喝、医疗品时要能够在安全的情况下寻找周围的吃喝、医疗品并使用。因此这也将能够做到你丢出医疗品给他，他会自己去取来用
- - ~~应当以物品每格子平均价值作为标准，尽可能将高单格价值的物品拿走~~（当前暂时使用的是整体价格）

## 更新日志

#### 0.5.X.0 计划

- 新增玩法：成为护航打手

#### 0.4.X.X 计划

- 正式版

#### 0.3.5.0

- **实现护航替换穿戴更好地胸挂、背包。同时实现连续拾取、套包**
- **实现翻越**

#### 0.3.4.0

- 优化了护航的战斗逻辑，并且现在护航还学会了切刀近战、切枪射击。
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