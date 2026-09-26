# 最后纪元老版词缀显示MOD（S4可用）

Last Epoch（最后纪元）词缀显示 MOD，基于 MelonLoader 0.7.2 / IL2CPP / net6.0。
在当前 S4 版本实机验证可用。

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

**地面掉落标签** — 暗金与套装件显示孔数和编织意志，崇高物品把高阶词缀直接拼在名字前：

```
虚空之触 [3] 刮 [二孔]
[T7][暴击伤害加成][T6][攻击速度] 巨神之弓
```

## 功能

| 功能 | 说明 |
|---|---|
| 词条 Roll 与分级 | 普通词缀、暗金词条、套装词条、隐式词条都显示 |
| 地面掉落标签 | 暗金 / 未腐化套装件显示传奇潜能孔数与编织意志；崇高物品拼出 T6+ 词缀 |
| 纯反射实现 | 不硬引用游戏程序集，游戏小版本更新不易失效 |

## 安装

1. 装好 MelonLoader 0.7.2
2. 把 `LastEpoch_AffixRoll.dll` 放进游戏目录的 `Mods/` 下
   ```
   Last Epoch/
   └── Mods/
       └── LastEpoch_AffixRoll.dll
   ```
3. **重启游戏**（MelonLoader 只在启动时加载 DLL）

## ⚠️ 与 LastEpoch_ItemTier 互斥

如果你在用另一个自己开发的 `LastEpoch_ItemTier` MOD，**不要同时启用**。
两者 patch 的目标方法完全相同（`TooltipItemManager.AffixFormatter / UniqueBasicModFormatter / ImplicitFormatter`
与 `GroundItemLabel.SetGroundTooltipText`），同时装会让每行词缀被加两遍标记。

## 配置

首次启动后生成 `UserData/LastEpoch_AffixRoll.cfg`：

| 键 | 默认 | 说明 |
|---|---|---|
| `TooltipRoll` | true | 装备提示词条总开关 |
| `GroundLabel` | true | 地面标签总开关 |
| `SetItems` | true | 套装件按暗金处理（仅未腐化的套装，腐化套装会跳过） |
| `GroundMinTier` | 6 | 地面标签只显示 T 级 ≥ 此值的词缀，6 = T6/T7/T8 |
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
| **v1.2.0** | 当前版本：套装件支持，移除腐化词缀显示 |

## 游戏侧接口备忘

已反编译核实的成员名，游戏更新后如需修复可对照：

| 用途 | 成员 |
|---|---|
| 词条 Roll / T 级 / 名称 | `ItemAffix.getRollFloat()` / `DisplayTier` / `affixName` |
| 暗金 / 稀有 / 崇高 / 套装 | `ItemData.isUnique()` / `isRare()` / `isExaltedItem()` / `isSet()` |
| 套装件 UniqueId | `ItemData.getSetItemUniqueId()` → ushort |
| 孔数 / 编织意志 | `ItemData.legendaryPotential` / `weaversWill` |
| 隐式 Roll | `ItemData.implicitRolls`（byte 数组）。旧接口 `getImplictRollFloat` 已被官方删除 |
| 暗金 / 套装 mods | `UniqueList.instance.uniques` → `Entry.mods` → `UniqueItemMod.value/maxValue` |
| 地面标签 | `GroundItemLabel.SetGroundTooltipText(bool)` / `getItemData()` / `itemText` / `emphasized` |
