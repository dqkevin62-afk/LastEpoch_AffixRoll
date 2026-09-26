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
