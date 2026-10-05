# BUG 修复说明：传奇物品（Legendary）地面标签不显示词缀

- **影响版本**：v1.2.0 及更早
- **修复版本**：**v2.0.0**
- **报告来源**：用户反馈「传奇物品不显示属性了」
- **涉及文件**：`Core/Items.cs`、`Features/GroundLabel.cs`、`Core/Cfg.cs`

---

## 一、症状

地面掉落的**传奇物品（Legendary）**，标签上只剩一个物品名，**没有任何词缀、也没有孔数/编织意志**。

对照：悬浮提示（把鼠标放到装备上）里传奇物品的词缀 Roll 显示是**正常的**，
只有地面掉落那一侧的标签是空的。

## 二、影响范围

| 显示位置 | 传奇物品表现 | 是否正常 |
|---|---|---|
| 装备悬浮提示（Tooltip） | 词缀带 `[T7优] … [91.2%]` | ✅ 正常 |
| **地面掉落标签** | **只有物品名** | ❌ 修复前异常 |

普通（白）、魔法（蓝）、稀有（黄）、**崇高（Exalted）**、**暗金（Unique）**、套装件均不受影响。

## 三、根因

`Features/GroundLabel.cs` 的 `Apply()` 里有两道判断：

```csharp
bool flag = Items.IsUnique(obj);          // ① 是不是暗金
bool isSetItem = Items.IsSet(obj);
if ((flag && num == 0) || isSetLike)      // ② 孔数 / 编织意志分支
{ ... }
if (Items.IsExalted(obj) && num > 0)      // ③ 高阶词缀分支
{ ... }
```

而 `Core/Items.cs` 当时只实现了四个判定：`IsSet` / `IsUnique` / `IsExalted` / `IsCorrupted`。

**关键在于 Last Epoch 把「传奇」当成跟「暗金」并列的一套独立稀有度。**
反编译 `Il2CppLE.dll` 的 `ItemData` 可以看到，游戏为此专门提供了四个方法：

```
ItemData.isLegendary()                  ItemData.isUniqueOrLegendary()
ItemData.isUniqueSetOrLegendary()       ItemData.isLegendaryPotentialItem()
```

既然官方要单独提供 `isUniqueOrLegendary`，就说明 **`isUnique()` 对传奇物品返回的是 `false`**。

于是传奇物品在 `Apply()` 里的两条路都走不通：

| 分支 | 传奇物品的值 | 结果 |
|---|---|---|
| ② `(flag && num == 0) \|\| isSetLike` | `flag = false`（不是暗金）<br>且传奇有传奇词缀 → `num > 0` | 进不去，不显示孔数 / 编织意志 |
| ③ `IsExalted(obj) && num > 0` | 传奇不是崇高 → `false` | 进不去，**词缀一行都不显示** |

两道门都关着，最后只能落到 `Reflect.Set(text)` 一句，输出的就是光秃秃的 `FullName`。

> 补充：这不是某次改动引入的回归。对比 v1.1.0 与 v1.2.0 的 `Apply()` 完全一致，
> 说明**传奇从一开始就没被处理过**。之所以近期才被注意到，是因为物品只有被
> 用传奇潜能升级成传奇之后才会暴露这个问题。

## 四、改动清单

### 1. `Core/Items.cs` — 补上传奇判定

```csharp
public static bool IsLegendary(object item)
    => Reflect.ToBool(Reflect.Call(item, new[] { "isLegendary", "IsLegendary" }));

public static bool IsLegendaryPotentialItem(object item)
    => Reflect.ToBool(Reflect.Call(item, new[] { "isLegendaryPotentialItem", "IsLegendaryPotentialItem" }));

// 暗金/传奇一视同仁时用这个（套装件不算，套装走 IsSet）
public static bool IsUniqueOrLegendary(object item)
{
    object obj = Reflect.Call(item, new[] { "isUniqueOrLegendary", "IsUniqueOrLegendary" });
    if (obj != null) return Reflect.ToBool(obj);
    return IsUnique(item) || IsLegendary(item);   // 游戏没这个方法时回退
}
```

走的是游戏原生方法而非自己拼稀有度枚举，游戏下次改枚举也不会失效（符合本 MOD 的纯反射设计）。
`Reflect.Call` 本身会沿 `BaseType` 遍历，所以在 `ItemDataUnpacked` 上调基类 `ItemData` 的方法没问题。

### 2. `Features/GroundLabel.cs` — 让传奇走进分支

```csharp
// 前
bool flag = Items.IsUnique(obj);
...
if (Items.IsExalted(obj) && num > 0)

// 后
bool flag = Items.IsUniqueOrLegendary(obj);
bool isLegendaryItem = Items.IsLegendary(obj);
...
if ((Items.IsExalted(obj) || isLegendaryItem) && num > 0)
```

### 3. `Features/GroundLabel.cs` — `ShortName()` 取最后一段

`FullName` 形如 `<词缀>的<物品名>`。`ShortName()` 用来把前缀砍掉、只留物品本名，但原来是写死取第 2 段：

```csharp
// 前：物品名有多个前缀时会砍错
return array[1];

// 后：取最后一段，几条前缀都对
return array[array.Length - 1];
```

传奇常常同时带 2~4 条前缀（`A的B的C的物品名`），旧写法会把**真正的物品名砍掉**，只剩中间某个词缀。
单前缀时新旧行为一致，所以这次改动对崇高物品没有副作用。

### 4. `Core/Cfg.cs`

`GroundLabel` 配置的中文说明补上「传奇」二字，纯文案。

## 五、修复前后对比

以真实 cfg（`GroundMinTier = 6`）渲染：

| 物品场景 | 修复前 | 修复后 |
|---|---|---|
| 传奇 · T7+T6 两条词缀<br>`FullName = 剧毒的秘术的陨星棱杖` | `剧毒的秘术的陨星棱杖` | `[T7][剧毒][T6][秘术]陨星棱杖` |
| 传奇 · 单条 T7 词缀<br>`FullName = 无情的陨星棱杖` | `无情的陨星棱杖` | `[T7][无情]陨星棱杖` |
| 传奇 · 词缀 T5（低于门槛） | `无情的陨星棱杖` | `无情的陨星棱杖`（见下方限制） |
| 暗金 · LP=3（对照） | `陨星棱杖 [三孔]` | `陨星棱杖 [三孔]`（行为不变） |
| 崇高 · T7（对照） | `[T7][剧毒]陨星棱杖` | `[T7][剧毒]陨星棱杖`（行为不变） |

## 六、怎么验证

1. 把编译出的 DLL 覆盖 `Mods/` 下同名文件，**重启游戏**
2. 找一个传奇物品掉在地上（或用现有传奇扔地上再捡起来）
3. 标签应该变成 `[T7][词缀名]物品名` 的形式

看不到的话，把 `UserData/LastEpoch_AffixRoll.cfg` 里 `Diag` 改成 `true`，
日志里会打出每一行标签的改写结果，据此判断是没进分支还是词缀被 tier 门槛挡了。

## 七、已知限制（需要用户自行决定）

`GroundMinTier` 默认 **6**，也就是地面标签只显示 T6 及以上的词缀。

传奇物品的词缀 tier 取决于做它时吞噬的那件装备，T1~T7 都可能出现。
因此 **T5 及以下的传奇词缀仍然不会显示** —— 这不是 BUG，是配置项默认值的取舍。

想让传奇的所有词缀都显示，有两个做法：

| 做法 | 影响 |
|---|---|
| 把 `GroundMinTier` 调到 `1` | 简单，但崇高物品的 T1~T5 词缀也会一起冒出来，标签变长 |
| 增加 `LegendaryMinTier`（尚未实现） | 传奇单独放宽，崇高仍按 T6 过滤，两类物品互不干扰 |

## 八、备注：为什么悬浮提示没坏

悬浮提示侧走的是 `TooltipItemManager` 的三个 formatter：

- 传奇物品的传奇词缀 → `AffixFormatter`（Kind.Affix 分支）
- 传奇物品保留的暗金词条 → `UniqueBasicModFormatter`（Kind.UniqueMod 分支）

这两条分支用的都是 item/affix 参数本身，不依赖 `isUnique` / `isExalted` 判定，
所以悬浮提示一直是对的，这次不用改。
