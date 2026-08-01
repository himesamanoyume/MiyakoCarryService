
<div>
<h1>
    <img src="./Server/Assets/database/traders/6952ced4bcc1dd1e3c80dfcb/6952ced4bcc1dd1e3c80dfcb.jpg" align="right" style="border-radius: 30px;">
    宫子护航店<br>
</h1>

简体中文 | [English](README-EN.md) | [Русский](README-RU.md)

宫子护航店 (MiyakoCarryService) 是一个生成AI队友的模组

<p>
    <img alt="" src="https://img.shields.io/github/v/release/himesamanoyume/MiyakoCarryService?style=flat-square&logo=github&labelColor=40405f&color=66ccff" />
    <img alt="" src="https://img.shields.io/github/downloads/himesamanoyume/MiyakoCarryService/total?style=flat-square&logo=github&labelColor=40405f&color=66ccff" />
</p>
</div>

## 描述

本文档会把 `Miyako Carry Service` 简称为 `Mcs`；你将会被称为 `McsLeadPlayer`，由 Mcs 生成的 AI 队友会被称为 `McsBotPlayer`。

支持通过 `Mcs Inventory Mode` 对 `McsBotPlayer` 的装备进行完整自定义，支持把任意类型的 AI 生成为 `McsBotPlayer`，甚至可以是其他模组提供的 AI 类型；支持 `McsBotPlayers` 按指定规则拾取物资；支持通过指令系统对 `McsBotPlayers` 下达命令；兼容 `SAIN`，兼容 `Fika`。

---

![Fika](/Preview/fika.webp)

在 `Fika` 多人模式中，每个玩家都可以拥有自己的 `McsBotPlayer` 小队。

---

![custombottype](https://ods5.oddba.cn/user_files/23687/bbs/29648841_1776483848.png)

让任意类型的 AI 都能成为你的队友，甚至是 Boss。

只要你的配置文件正确，你甚至可以生成由第三方模组提供的 AI 类型，例如 [Black Division Home](https://forge.sp-tarkov.com/mod/2511/wtt-black-division-redacted-home)、[UNTAR Go Home!](https://forge.sp-tarkov.com/mod/2342/untar-go-home)、[RUAF Come Home!](https://forge.sp-tarkov.com/mod/2427/ruaf-come-home)。

![customspawntype](https://ods5.oddba.cn/user_files/23687/bbs/35371127_1771827285.png)

---

### Mcs 能为你带来什么？

答案是：极致舒适的服务体验。

#### 强大且灵活的指令系统

除了少量基础移动指令外，Mcs 在 `EFT` 自带的动作菜单基础之上，进一步为指令系统注入了无限可能。

这意味着你可以让一个 `McsBotPlayer` 带你前往各种目的地——任务地点、撤离点、转运点、开关点，尤其是你知道要去哪里，但却不知道具体位置的地点。你只需要让它在你前面带路，跟着它的脚步走就能到达目的地（前提当然是该地点本身允许 AI 前往）。

你甚至可以把某些动作委托给它们自行完成，而不必你自己亲自处理。比如在支持的任务条件下，Mcs 允许代理动作——通过单次指令，它们就可以独自前往任务地点执行修理、前往任务点、安装物品等操作。你可以在不亲自前往现场的情况下完成任务。

或者你想打开某个触发撤离的开关，你可以直接命令它们自己去操作，而你则直接前往撤离点；开关打开后，你就可以直接撤离，不需要再先去开开关、再回去撤离的老路子了。

此外，如果你身处复杂区域，周围可能潜伏着敌人，你也可以命令它们彻底搜索指定地点的每个角落。

#### 让一切收益都归你所有

作为一款追求极致服务体验的模组，你的 `McsBotPlayer` 自然应该能够根据你的设置主动搜刮物资。你可以设置屏蔽物品类型、最低价值阈值、是否拾取包含特定关键词的物品，以及更多内容。它们也会在一定程度上对拾取物资进行整理，尽可能多地拿走你需要的物资。不过，最终这些收益仍然需要你亲自收集并带出战局后才能真正属于你。

#### 成就伟业的最后一块拼图

**编队系统**

该系统允许你配置一个 `7×7` 的编队矩阵，在其中设置你自己的位置、队友的位置，以及它们之间的间距。现在你就拥有一个训练有素、强大的小队，它会在任何时候保持编队姿态。你还可以通过快捷键将当前编队保存为预设，并给它自定义名称，然后通过指令系统快速应用编队预设来应对各种场景。

#### 可按玩家独立生效的配置

众所周知，Mcs 支持安装 `Fika` 附加组件，以便在 `Fika` 多人模式下使用。我们也知道，在 `Fika` 多人模式中，几乎所有战局计算都由主机完成。因此，我们并不希望每个 `McsBotPlayer` 的行为都只由主机决定。别担心——Mcs 也支持同步部分配置项，因此作为主机的用户可以让不同小队使用各自的配置进行计算，包括拾取物资配置、编队站位配置等。我保证你可以获得和本地单人战局中使用 Mcs 一样的体验。

## 教程

### 安装

1. 将下载到的压缩包中的文件夹放入你的主 `SPT` 文件夹中。
2. 模组默认会被激活。

![Installation](https://i.imgur.com/afNPjAw.gif)

---

### 如何生成 `McsBotPlayer`

Mcs 模仿现实中的搬运服务流程。在这个过程中，你需要选择所需的服务，下单并支付，然后与来为你提供服务的玩家一起游玩。因此，Mcs 提供了一个名为 `Tsukiyuki Miyako` 的商人来处理这些事务。

![tutorial](https://ods5.oddba.cn/user_files/23687/bbs/35529095_1776484381.png)

如果你看不到分组面板，请先检查是否安装了 `UI Fixes`，然后在配置管理器中打开 `Advanced Settings`，进入 `Interface -> Show Group Panel`，并启用它。

_目前 Mcs 不提供长期服务。当你设置的服务期限结束后，`McsBotPlayer` 会自动删除该队友。_

---

### 如何自定义 `McsBotPlayer` 装备

1. 你需要先把某个已加入好友列表的 `McsBotPlayer` 作为好友。
2. 从底部栏左下角打开 `Invite to group` 界面。
3. 右键点击 `McsBotPlayer` 打开右键菜单，选择 `OPEN INVENTORY`。
4. 此时你将进入 `Mcs Inventory Mode`。
5. 在 `Mcs Inventory Mode` 中，你可以像正常游戏一样通过操作来自定义 `McsBotPlayer` 的装备。
6. 进入 `Mcs Inventory Mode` 后，Miyako 商人会提供所有物品供你以 `1` 卢布购买。
7. 想回到主角色，有两种方式：一种是再次通过 `Invite to group` 界面右键点击 `McsBotPlayer`，选择 `RETURN TO MAIN CHARACTER`；另一种是直接点击底部栏上的绿色 `Mcs Inventory Mode` 按钮，也能返回主角色。

![McsInventoryMode](/Preview/mcsinventorymode.webp)

_1. `Mcs Inventory Mode` 仅在 `PMC` 模式下修改 `McsBotPlayer` 的装备。众所周知，`PMC` 模式和 `SCAV` 模式是两种不同角色。根据游戏设定，你无法控制 `SCAV` 角色的装备，因此 Mcs 也会保持这一设定。_

_2. `McsBotPlayer` 的好友槽位在服务到期后会变得不可用。你可以选择结算或续订订单。只有在成功结算后，已过期 `McsBotPlayer` 的全部数据才会被永久删除；而续订会发放同等时长的新订单；一旦续订任务完成，服务到期时间也会相应延长。_

### 如何续订与结算

1. 同样地，打开底部栏左下角的 `Invite to group` 界面。
2. 同样地，右键点击 `McsBotPlayer` 打开右键菜单，选择 `RENEW ORDER`。
3. 然后会发放一项同等时长的新任务，完成后即可完成续订。
4. 对于已过期的 `McsBotPlayer`，选择 `SETTLE ORDER` 会永久删除该订单下所有 `McsBotPlayer` 的全部数据。
5. 过期 `McsBotPlayer` 的数据会被保留到你主动结算为止。你可以随时续订任何 `McsBotPlayer`，让过期的 `McsBotPlayer` 再次可用，或者延长尚未过期 `McsBotPlayer` 的服务时间。

---

### 如何在战局中获取 `McsBotPlayers` 拾取的物资

当 `McsBotPlayer` 拾取目标物资时，如果你不想等到它死亡后再通过尸体捡回这些物品，那么除了 `McsBotPlayer` 死亡后清理尸体外，你还可以在 `Mcs` 指令菜单中对单个 `McsBotPlayer` 使用 `OPEN INVENTORY` 指令，在它还活着的时候远程打开它的背包；或者使用 `DROP TARGET LOOT` 指令，让它在战局中把当前已经拾取的目标物资直接丢到你面前。

根据游戏设定，`McsBotPlayer` 会模仿真实玩家的行为，因此在它拾取物资后，如果你自己不亲自拿走，那么这些物资在战局结束后都不会属于你。

> 如果你想要，那你就得自己拿走，和你已经知道的一样。

---

### 如何自定义 `McsBotPlayer` 类型

1. 先启动一次服务器，此时会在 `MiyakoCarryServiceServer/configs` 目录下生成 `spawntype.json`。
2. 如果你不理解下面的内容，建议不要随意改动。错误的修改会导致报错。如果你真的想改，继续往下看。你也可以交给 AI 来处理。如果改得还是不对，就删除 `spawntype.json` 并重新启动服务器，新的文件会重新生成。
3. 首先，你需要了解 JSON 格式规则。最好使用像 VS Code 这样的编辑器来检查 JSON 格式。
4. 然后，你需要了解由第三方模组提供或原版游戏内置的 `WildSpawnType` 名称。
5. 以 [Black Division Home](https://forge.sp-tarkov.com/mod/2511/wtt-black-division-redacted-home) 为例。它提供了一个黑暗分部的 `WildSpawnType`：`blackDivAssault`。
6. 现在你需要按照正确格式，在 `spawntype.json` 中添加另一种类型。

```jsonc
{
    "WildSpawnType": "sectantOni",
    "IsBoss": true,
    "DisplayName": "Oni"
}, 
    // 上面的内容是 EFT 原生的 `WildSpawnType`。之后再添加新类型时，
    // 需要在 `}` 后补上 `,`。
{
    "WildSpawnType": "blackDivAssault", 
    // 在这里写 `WildSpawnType` 名称。
    "IsBoss": false, 
    // 是否作为 Boss 类型。
    // 在使用帮助命令时，会为此类型添加 `[Boss]` 前缀。
    "DisplayName": "Black Division Assault" 
    // 显示名称。在使用帮助命令时，
    // 只会显示这个名称，不显示 `WildSpawnType` 名称。
} 
    // 最后一个 `}` 不要加 `,`。
```

7. 确认 JSON 格式正确且数据填写无误后，重新启动服务器。这时，在 Miyako 商人的 `MESSENGER` 界面中使用帮助命令，就能在可用类型列表中看到全部类型。
8. 如果在生成自定义类型时发生异常，你会在服务器日志中收到错误提示，并会回退到默认生成 `pmc` 类型 `McsBotPlayer`。此时，你需要检查你填写的自定义类型是否有误。

## 配置

| 配置项 | 说明 | 默认值 |
| --- | --- | --- |
| `BalanceRestriction` | 启用后，`McsBotPlayer` 的 `OPEN INVENTORY` 指令将不可用，且所有带入战局的物品会获得 `Curse of Vanishing` 附魔；当 `McsBotPlayer` 死亡后，带入物品会立即在任何位置消失。 | `false` |
| `CheckUpdate` | 是否在线检查版本更新。 | `true` |
| `CheckIfdian` | 是否检查赞助者列表更新。 | `true` |
| `TicketPricePerPercent` | 用于计算申请票据移除涨价惩罚时所支付罚金的单价，单位为卢布/百分比。 | `300000` 卢布/百分比 |
| `PunishmentMultiMax` | 最大涨价惩罚倍数。 | `1`（即 `100%`） |
| `OrderPendingPaymentTime` | 通过 Miyako 商人下单 `McsBotPlayer` 或发放补票时，动作任务的有效等待时间，单位为秒。 | `900` 秒（15 分钟） |
| `CompensationPrice` | 当 `McsBotPlayer` 意外击杀 `McsLeadPlayer` 时的补偿金额，单位为卢布。 | `300000` 卢布 |
| `CarryServiceLevelPrice` | 搬运服务等级基础定价，共 5 个等级，每个等级都设有上下限。 | 由等级区间决定 |

## 功能

### 基础

| 功能项 | 说明 |
| --- | --- |
| `Enable Looting` | 如果 `McsBotPlayer` 当前未处于战斗中，是否尝试拾取物品。 |
| `Price Threshold` | 低于该价格的物品将被忽略。 |
| `Loot Name Keyword` | 关键词为物品完整名称或缩写中包含的文本，支持使用 `||`、`,` 或 `，` 分隔多个关键词进行搜索。 |
| `Loot Keyword Items` | `McsBotPlayer` 是否尝试拾取带有关键词的物品。 |
| `Blocked Item Types` | 支持全选/全不选：`Ammo`、`Barter`、`Info`、`Container`、`Food`、`Backpack`、`Goggles`、`Pocket`、`Tactical Vest`、`Armor`、`Grenade`、`Headphone`、`Keys`、`Knife`、`Magazine`、`Meds`、`Mod`、`Special`、`Weapon`、`Other`。 |
| `Enable Keep Formation` | 是否基于编队矩阵配置来决定 `McsBotPlayer` 的位置。 |
| `Formation Matrix` | 用于配置每个 `McsBotPlayer` 的位置。`“★”` 表示你的所在位置，`“★”` 上方表示你的视线方向。 |
| `Formation Spacing` | 编队成员之间的间距设置。 |
| `Formation Sequential Fill` | 启用后，若编队中某个 `McsBotPlayer` 死亡，则下一个同队成员会按简写编码顺序补上空缺位置。 |
| `Save Formation Preset Hotkey` | 保存当前编队预设并绑定快捷键。*在安装 Fika 时，战局中设置改动会自动同步到宿主，但前提是需先安装 [MiyakoCarryServiceFika](https://forge.sp-tarkov.com/addon/86/miyako-carry-service-fika-addon) 插件。* |

### 指令

#### MemberCommand

| 指令 | 说明 |
| --- | --- |
| `Report Enemy Position` | 若 `McsBotPlayer` 当前处于战斗中，则命令其汇报已知敌方位置。 |
| `Report Self Status` | 命令 `McsBotPlayer` 汇报自身生命值与补给状态。 |
| `On Your Own` | 命令 `McsBotPlayer` 自行行动。 |
| `Regroup` | 命令 `McsBotPlayer` 停止当前动作并重新集结。 |
| `Follow Me` | 命令 `McsBotPlayer` 跟随 `McsLeadPlayer`。在此状态下，`McsBotPlayer` 不会主动攻击敌人或拾取物资。 |
| `Exclude / Takeover` | `Exclude` 指令会阻止 `McsBotPlayer` 接收队友指令；`Take Over` 指令则恢复对其的团队指令控制。 |
| `Go To` | 若 `McsBotPlayer` 当前未处于战斗中，则命令其前往指定位置。 |
| `Hold Position` | 若 `McsBotPlayer` 当前未处于战斗中，则命令其原地停留。 |
| `Force Teleport` | 清除 `McsBotPlayer` 的仇恨并尝试将其瞬移到当前位置。 |
| `Open Inventory` | 远程打开 `McsBotPlayer` 背包，用于转移其拾取到的物品。 |
| `Change Aiming Body Part Type` | 命令 `McsBotPlayer` 切换偏好的战斗瞄准部位。 |
| `Escort` | 若 `McsBotPlayer` 当前未处于战斗中，则命令其护送到目标地点。子项包括：`Quest Escort`、`Exfil Escort`、`Transit Escort`、`Switch Escort`、`Stationary Weapon Escort`、`BTR Escort`、`Airdrop Escort`。 |
| `Proxy Action` | 若 `McsBotPlayer` 当前未处于战斗中，则命令其代理执行相关动作。子项包括：`Quest Proxy Action`、`Door Proxy Action`、`Loot Proxy Action`、`Switch Proxy Action`。 |
| `Drop Target Loot` | 若 `McsBotPlayer` 当前未处于战斗中，则命令其丢下此次战局中已拾取的目标物资。 |
| `Clear Area` | 若 `McsBotPlayer` 当前未处于战斗中，则命令其清理指定地点周围区域。 |

#### TeamCommand

| 指令 | 说明 |
| --- | --- |
| `Team Report Enemy Position` | 若当前队伍中存在处于战斗中的 `McsBotPlayers`，则命令它们汇报已知敌方位置。 |
| `Team Report Self Status` | 命令所有 `McsBotPlayers` 汇报自身生命值与补给状态。 |
| `Team On Your Own` | 命令所有 `McsBotPlayers` 自行行动。 |
| `Team Regroup` | 命令所有 `McsBotPlayers` 停止动作并重新集结。 |
| `Team Follow Me` | 命令所有 `McsBotPlayers` 跟随 `McsLeadPlayer`。在此状态下，它们不会主动攻击敌人或拾取物资。 |
| `Team Go To` | 若当前队伍中有未处于战斗中的 `McsBotPlayers`，则命令它们前往指定位置。 |
| `Team Hold Position` | 若当前队伍中有未处于战斗中的 `McsBotPlayers`，则命令它们原地停留。 |
| `Team Force Teleport` | 清除整个队伍 `McsBotPlayers` 的仇恨，并尝试将它们瞬移到当前位置。 |
| `Team Change Aiming Body Part Type` | 命令所有 `McsBotPlayers` 切换偏好的战斗瞄准部位。 |
| `Team Escort` | 若当前队伍中存在未处于战斗中的 `McsBotPlayers`，则命令它们护送到目标地点。子项包括：`Team Quest Escort`、`Team Exfil Escort`、`Team Transit Escort`、`Team Switch Escort`、`Team Stationary Weapon Escort`、`Team BTR Escort`、`Team Airdrop Escort`。 |
| `Team Drop Target Loot` | 若当前队伍中有未处于战斗中的 `McsBotPlayers`，则命令它们丢下本次战局中已拾取的目标物资。 |
| `Team Clear Area` | 若当前队伍中有未处于战斗中的 `McsBotPlayers`，则命令它们清理指定地点周围区域。 |
| `Change Formation` | 立即应用已保存的编队预设。 |

> 在安装 Fika 时，指令系统也可正常工作，但前提是需要先安装 [MiyakoCarryServiceFika](https://forge.sp-tarkov.com/addon/86/miyako-carry-service-fika-addon) 插件。

### 玩家

| 设置项 | 说明 |
| --- | --- |
| `Teammate Highlight` | 是否在战局中高亮所有 `McsBotPlayer` 角色。 |
| `Teammate Highlight Hotkey` | 高亮快捷键设置。 |
| `Teammate Highlight Color` | 高亮颜色设置。 |
| `Enable Mcs Subtitles` | 是否使用字幕显示 `McsBotPlayer` 的报告信息。 |
| `Show Brevity Code` | 是否用简短口令替换原始昵称进行显示。 |

## Language

- English (AI Translate)
- 简体中文
- русский язык (Contributed by NotDifficult)

## WildSpawnType

<details>

<summary>EFT Native</summary>

- marksman
- assault
- bossTest
- bossBully (Mcs default support)
- followerTest
- followerBully
- bossKilla (Mcs default support)
- bossKojaniy (Mcs default support)
- followerKojaniy
- pmcBot (Mcs default support)
- cursedAssault
- bossGluhar (Mcs default support)
- followerGluharAssault
- followerGluharSecurity
- followerGluharScout
- followerGluharSnipe
- followerSanitar
- bossSanitar (Mcs default support)
- test
- assaultGroup
- sectantWarrior
- sectantPriest
- bossTagilla (Mcs default support)
- followerTagilla
- exUsec (Mcs default support)
- gifter
- bossKnight (Mcs default support)
- followerBigPipe (Mcs default support)
- followerBirdEye (Mcs default support)
- bossZryachiy (Mcs default support)
- followerZryachiy
- bossBoar (Mcs default support)
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
- bossKolontay (Mcs default support)
- followerKolontayAssault
- followerKolontaySecurity
- shooterBTR
- bossPartisan (Mcs default support)
- spiritWinter
- spiritSpring
- peacemaker
- pmcBEAR (Mcs default support)
- pmcUSEC (Mcs default support)
- skier
- sectantPredvestnik (Mcs default support)
- sectantPrizrak (Mcs default support)
- sectantOni (Mcs default support)
- infectedAssault (Mcs default support)
- infectedPmc (Mcs default support)
- infectedCivil
- infectedLaborant
- infectedTagilla (Mcs default support)
- bossTagillaAgro (Mcs default support)
- bossKillaAgro (Mcs default support)
- tagillaHelperAgro

</details>

### [Black Division Home](https://forge.sp-tarkov.com/mod/2511/wtt-black-division-redacted-home)

- blackDivLead
- blackDivAssault
- blackDivBreacher
- blackDivSupport

### [UNTAR Go Home!](https://forge.sp-tarkov.com/mod/2342/untar-go-home)

- followeruntar
- bossuntarlead
- followeruntarmarksman
- bossuntaroffice

### [RUAF Come Home!](https://forge.sp-tarkov.com/mod/2427/ruaf-come-home)

- ruafRifleman
- ruafRiflemanSenior
- ruafAutorifleman
- ruafGrenadier
- ruafMarksman
- ruafMachinegunner

---

### 鸣谢

[SPT](https://github.com/sp-tarkov)

[SPT-PitFireTeam](https://github.com/pitAlex/SPT-PitFireTeam)

[Fika](https://github.com/project-fika)

[SAIN 3.11.X](https://github.com/Solarint/SAIN) / [SAIN 4.0.X](https://github.com/ArchangelWTF/SAIN)

[SPT-LootingBots](https://github.com/Skwizzy/SPT-LootingBots)

[SPT-BigBrain](https://github.com/DrakiaXYZ/SPT-BigBrain)

### 支持我

[Ko-Fi](https://ko-fi.com/himesamanoyume) | [Ifdian](https://ifdian.net/a/himesamanoyume)
