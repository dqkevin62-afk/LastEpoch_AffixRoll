using System;

namespace LastEpoch_AffixRoll.Core;

public static class Items
{
	public static bool IsSet(object item)
	{
		return Reflect.ToBool(Reflect.Call(item, new string[2] { "isSet", "IsSet" }));
	}

	public static bool IsUnique(object item)
	{
		return Reflect.ToBool(Reflect.Call(item, new string[2] { "isUnique", "IsUnique" }));
	}

	public static bool IsExalted(object item)
	{
		return Reflect.ToBool(Reflect.Call(item, new string[2] { "isExaltedItem", "IsExaltedItem" }));
	}

	// 传奇（Legendary）在 LE 里是跟 Unique 并列的一套 rarity：暗金用传奇潜能(LP)
	// 升级后就成了传奇，游戏为此专门提供了 isLegendary / isUniqueOrLegendary /
	// isLegendaryPotentialItem 三个方法。也就是说 isUnique() 对传奇物品返回的是 false，
	// 只拿 IsUnique 去判，传奇物品会整个漏掉 —— 这就是传奇不显示词缀的根因。
	public static bool IsLegendary(object item)
	{
		return Reflect.ToBool(Reflect.Call(item, new string[2] { "isLegendary", "IsLegendary" }));
	}

	// 还没升级成传奇、但带着传奇潜能的暗金（也就是带 [N孔] 的那些）
	public static bool IsLegendaryPotentialItem(object item)
	{
		return Reflect.ToBool(Reflect.Call(item, new string[2] { "isLegendaryPotentialItem", "IsLegendaryPotentialItem" }));
	}

	// 暗金/传奇一视同仁时用这个（套装件不算，套装走 IsSet）
	public static bool IsUniqueOrLegendary(object item)
	{
		object obj = Reflect.Call(item, new string[2] { "isUniqueOrLegendary", "IsUniqueOrLegendary" });
		if (obj != null)
		{
			return Reflect.ToBool(obj);
		}
		return IsUnique(item) || IsLegendary(item);
	}

	public static bool IsCorrupted(object item)
	{
		if (Reflect.ToBool(Reflect.Call(item, new string[2] { "IsCorrupted", "isCorrupted" })))
		{
			return true;
		}
		return Reflect.ToBool(Reflect.Get(item, "corrupted", "Corrupted"));
	}

	public static int SetItemUniqueId(object item)
	{
		return Reflect.ToInt(Reflect.Call(item, new string[2] { "getSetItemUniqueId", "GetSetItemUniqueId" }), -1);
	}
}
