using System;

namespace LastEpoch_AffixRoll.Core;

public static class Items
{
	private static bool _probed;

	// 兼容性自检：游戏大版本更新后，ItemData 上的 isUnique / isLegendary 之类方法可能被改名或改签名。
	// 一旦消失，稀有度判定与地面标签会「静默失效」（不报错但也不生效），很难排查 ——
	// 所以启动时探测一次，缺哪个就直接记进日志。
	public static void Probe()
	{
		if (_probed)
		{
			return;
		}
		_probed = true;
		Reflect.Refresh();
		Type type = Reflect.T("ItemData");
		if (type == null)
		{
			Log.WarnOnce("Items:noitemdata", "[AffixRoll] 兼容性自检: 找不到 ItemData 类型，稀有度判定会全部失效（游戏版本可能变了）。");
			return;
		}
		string[] array = new string[5] { "isUnique", "isLegendary", "isUniqueOrLegendary", "isExaltedItem", "isSet" };
		string text = "";
		string text2 = "";
		for (int i = 0; i < array.Length; i++)
		{
			bool flag = Reflect.HasMethod(type, array[i]);
			text = text + ((text.Length > 0) ? " " : "") + array[i] + "=" + (flag ? "有" : "缺");
			if (!flag)
			{
				text2 = text2 + ((text2.Length > 0) ? ", " : "") + array[i];
			}
		}
		if (text2.Length > 0)
		{
			Log.WarnOnce("Items:probe", "[AffixRoll] 兼容性自检: 缺少 " + text2 + " —— 游戏版本可能变了，相关功能会失效。");
		}
		else
		{
			Log.Msg("[AffixRoll] 兼容性自检: " + text);
		}
	}

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
