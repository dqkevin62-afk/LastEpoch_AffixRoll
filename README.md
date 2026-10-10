# 最后纪元老版词缀显示MOD（支持 LE 1.5.2.x）

Last Epoch（最后纪元）词缀显示 MOD，基于 MelonLoader 0.7.3 / IL2CPP / net6.0。
在 **Last Epoch 1.5.2.2**（2026-10-10）实机验证可用，日志确认全部钩子正常挂载、无异常。

> 来源：从第三方 `ScreenPro.dll`（原作者 Sepake）抽取重写。原 MOD 的镜头、迷雾、场景显形等功能
> 在 S4 已失效，本 MOD 只保留词缀相关部分，并改为纯反射实现以适配当前版本。

## 效果

**装备悬停提示** — 每行词缀前面加 T 级与评级，后面加精确 Roll 百分比：

```
[<color=#A807FF>T5</color><color=#77ACFF>中</color>] +12% 攻击速度 <color=#77ACFF>[63.4%]</color>
[<color=#FA9E3D>T7</color><color=#FA9E3D>优</color>] +58% 暴击伤害加成 <color=#FA9E3D>[91.2%]</color>
[<color=#16FF0E>T3</color><color=#D2D2D2>差</color>] +9 物理伤害 <color=#E1E1E1>[22%]</color>
```

评级阈值：<50 差 / 50~70 中 / 70~80 良 / 80~95 优 / ≥95 极。

**地面掉落标签** — 暗金与套装件显示孔数和编织意志，崇高与传奇物品把高阶词缀直接拼在名字前：

```
虚空之触 [3] 刮 [二孔]
[T7][暴击伤害加成][T6][攻击速度] 巨神之弓
[T7][剧毒][T6][秘术] 陨星棱杖      ← v2.0.0 起支持传奇
```

> ⚠️ 传奇物品在 **v1.2.0 及更早版本完全不显示词缀**，原因是漏判了传奇稀有度。
> - 玩家（怎么更新、怎么调）：[更新说明-v2.0.0.md](更新说明-v2.0.0.md)
> - 技术细节（根因、改动清单）：[BUGFIX-Legendary-GroundLabel.md](BUGFIX-Legendary-GroundLabel.md)

## 功能

| 功能 | 说明 |
|---|---|
| 词条 Roll 与分级 | 普通词缀、暗金词条、套装词条、隐式词条都显示 |
| 地面掉落标签 | 暗金 / 传奇 / 未腐化套装件显示传奇潜能孔数与编织意志；**传奇与崇高**物品拼出 T6+ 词缀 |
| 纯反射实现 | 不硬引用游戏程序集，游戏小版本更新不易失效 |

## 安装

1. 装好 MelonLoader 0.7.3（0.7.2 也可用，但建议跟随新版）
2. 把 `LastEpoch_AffixRoll.dll` 放进游戏目录的 `Mods/` 下
   ```
   Last Epoch/
   └── Mods/
       └── LastEpoch_AffixRoll.dll
   ```
3. **重启游戏**（MelonLoader 只在启动时加载 DLL）

## ⚠️ 必须同时装 CoreModuleAutoFix（否则所有 MOD 都不生效）

Unity 6000.4.x 之后，MelonLoader 生成的 `UnityEngine.CoreModule.dll` 里会出现同名类型，
启动即报：

```
System.BadImageFormatException: Duplicate type with name '<>O' in assembly 'UnityEngine.CoreModule'
Support Module [Il2Cpp.dll] threw an Exception
No Support Module Loaded!
```

**表现就是：MOD 显示"已加载"，但一个都不生效。**

- 这是 **Unity 6000.4 的已知问题，MelonLoader 官方至今没修**
  （最新正式版仍是 v0.7.3；`LavaGang/MelonLoader#1218` 记录了完全相同的症状，
  被提 issue 者自己关闭，结论是 "the issue was plugin related" —— 即公认靠插件修）。
- 解法：把 `CoreModuleAutoFix.dll` 放进 **`Plugins/`**（不是 `Mods/`），
  它在 `OnPreModsLoaded()` 里自动修复，无需手动操作。装好后日志会看到：

  ```
  [CoreModuleAutoFix] 检测到 1 组重名类型（Unity 6000.4 已知问题），开始修复 ...
  [CoreModuleAutoFix] 已修复 2 个重复类型。
  [CoreModuleAutoFix] 复核通过：已无重名类型。
  ```

- ⚠️ **游戏每次大更新都会重新生成 interop，这个坏程序集必定复发**，
  但插件每次启动都会自动再修一次，不用管。
- v2.1.0 起本 MOD 还会做**兼容性自检**，启动时在日志里打印
  `兼容性自检: isUnique=有 isLegendary=有 ...`，哪一项变成「缺」就是游戏版本变了。

详细说明见 [更新说明-v2.1.0.md](更新说明-v2.1.0.md)。

## ⚠️ 与 LastEpoch_ItemTier 互斥

如果你在用另一个自己开发的 `LastEpoch_ItemTier` MOD，**不要同时启用**。
两者 patch 的目标方法完全相同（`TooltipItemManager.AffixFormatter / UniqueBasicModFormatter / ImplicitFormatter`
与 `GroundItemLabel.SetLabelText` / `SetGroundTooltipText`），同时装会让每行词缀被加两遍标记。

## 配置

首次启动后生成 `UserData/LastEpoch_AffixRoll.cfg`：

| 键 | 默认 | 说明 |
|---|---|---|
| `TooltipRoll` | true | 装备提示词条总开关 |
| `GroundLabel` | true | 地面标签总开关 |
| `SetItems` | true | 套装件按暗金处理（仅未腐化的套装，腐化套装会跳过） |
| `GroundMinTier` | 6 | 地面标签只显示 T 级 ≥ 此值的词缀，6 = T6/T7/T8。**只作用于崇高物品** |
| `LegendaryMinTier` | 1 | 地面标签词缀门槛，**只作用于传奇物品**（v2.1.0 新增）。传奇的熔铸词条常见只有 T1~T5，若沿用 6 会被整条滤空 |
| `ImplicitDivisor` | 255 | 隐式 Roll 的分母；若隐式百分比明显偏低，改小（如 100） |
| `Diag` | false | 排查日志，不显示时先开这个 |

## 自行编译

```bash
cd src && dotnet build -c Release
```

`src/LastEpoch_AffixRoll.csproj` 里的 `LastEpochPath` 指向游戏根目录，换机器需要改。
工程只引用 MelonLoader / 0Harmony / Il2CppInterop.Runtime，**不引用 Il2CppLE.dll**（纯反射设计）。

## 版本

| 版本 | 说明 |
|---|---|
| v1.0.0 | 三项基础功能，实机验证有效 |
| v1.1.0 | 增加套装件与腐化词缀；套装有效保留，腐化已回滚 |
| v1.2.0 | 套装件支持，移除腐化词缀显示 |
| v2.0.0 | 修复传奇（Legendary）物品地面标签完全不显示词缀的 BUG；`ShortName()` 改为取最后一段，修掉多词缀前缀把物品名砍坏的隐藏问题。详见 [BUGFIX-Legendary-GroundLabel.md](BUGFIX-Legendary-GroundLabel.md) |
| v2.0.1 | 传奇地面标签改走独立门槛 `LegendaryMinTier`（熔铸词条常见 T1~T5，原门槛会滤空）；新增只读诊断钩子 `GetUniqueDescription` |
| **v2.1.0** | **当前版本**：适配 LE 1.5.2.x（实测 1.5.2.2）；新增**兼容性自检**（游戏更新后方法改名会静默失效，现在直接报出来）。详见 [更新说明-v2.1.0.md](更新说明-v2.1.0.md) |

> 版本号直接跳到 2.0.0：那次补上了一整类稀有度（传奇）的支持，属于功能级变更而非补丁。

## 游戏侧接口备忘

已反编译核实的成员名，游戏更新后如需修复可对照：

| 用途 | 成员 |
|---|---|
| 词条 Roll / T 级 / 名称 | `ItemAffix.getRollFloat()` / `DisplayTier` / `affixName` |
| 暗金 / 稀有 / 崇高 / 套装 | `ItemData.isUnique()` / `isRare()` / `isExaltedItem()` / `isSet()` |
| **传奇（独立稀有度）** | `ItemData.isLegendary()` / `isUniqueOrLegendary()` / `isUniqueSetOrLegendary()` / `isLegendaryPotentialItem()` |
| 套装件 UniqueId | `ItemData.getSetItemUniqueId()` → ushort |
| 孔数 / 编织意志 | `ItemData.legendaryPotential` / `weaversWill` |
| 隐式 Roll | `ItemData.implicitRolls`（byte 数组）。旧接口 `getImplictRollFloat` 已被官方删除 |
| 暗金 / 套装 mods | `UniqueList.instance.uniques` → `Entry.mods` → `UniqueItemMod.value/maxValue` |
| 地面标签 | `GroundItemLabel.getItemData()` / `itemText` / `emphasized` |
| 地面标签钩子（S5） | `SetLabelText(bool)` 与 `SetGroundTooltipText` **都挂**（见下方说明） |

> **S5 起 `SetGroundTooltipText` 变成了无参 private**，真正往标签上写文本的是
> `SetLabelText(bool recalculateItemName)`。本 MOD 对两个方法名都做了 postfix 挂载，
> 参数个数接受 0 个或 1 个。`Apply()` 每次都从 `ItemData.FullName` 重新拼接，重复执行结果一致，不会叠加。
