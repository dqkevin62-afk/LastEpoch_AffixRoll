using System;
using System.Reflection;
using HarmonyLib;
using LastEpoch_AffixRoll.Core;

namespace LastEpoch_AffixRoll.Features;

public static class GroundLabel
{
	public static string HookName = "none";

	private static HarmonyLib.Harmony _harmony;

	private static bool _resolved;

	private static int _tries;

	public static void Resolve()
	{
		if (_resolved || _tries++ > 240)
		{
			return;
		}
		Reflect.Refresh();
		Type type = Reflect.T("GroundItemLabel");
		if (type == null)
		{
			if (_tries % 40 == 1)
			{
				Log.WarnOnce("Ground:nolabel", "[AffixRoll] 找不到 GroundItemLabel，地面标签暂不可用。");
			}
			return;
		}
		MethodInfo[] methods;
		try
		{
			methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
		}
		catch
		{
			return;
		}
		MethodInfo[] array = methods;
		foreach (MethodInfo methodInfo in array)
		{
			if (!string.Equals(methodInfo.Name, "SetGroundTooltipText", StringComparison.Ordinal))
			{
				continue;
			}
			ParameterInfo[] parameters;
			try
			{
				parameters = methodInfo.GetParameters();
			}
			catch
			{
				continue;
			}
			if (parameters == null || parameters.Length != 1)
			{
				continue;
			}
			try
			{
				if (_harmony == null)
				{
					_harmony = new HarmonyLib.Harmony("LastEpoch_AffixRoll.GroundLabel");
				}
				MethodInfo method = typeof(GroundLabel).GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public);
				_harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(method));
				HookName = "GroundItemLabel.SetGroundTooltipText(1)";
				_resolved = true;
				Log.Msg("[AffixRoll] 地面标签钩子已挂载。");
			}
			catch (Exception ex)
			{
				Log.WarnOnce("Ground:patch", "[AffixRoll] 挂载地面标签失败: " + ex.Message);
			}
		}
	}

	public static void Postfix(object __instance)
	{
		try
		{
			if (!Cfg.GroundLabel.Value || __instance == null)
			{
				return;
			}
			Apply(__instance);
		}
		catch (Exception ex)
		{
			Log.WarnOnce("Ground:postfix", "[AffixRoll] 地面标签改写异常: " + ex.Message);
		}
	}

	private static void Apply(object label)
	{
		object obj = Reflect.Call(label, new string[2] { "getItemData", "GetItemData" });
		if (obj == null)
		{
			return;
		}
		object obj2 = Reflect.Get(label, "itemText", "ItemText");
		if (obj2 == null)
		{
			obj2 = FindTextInLabel(label);
		}
		if (obj2 == null)
		{
			return;
		}
		string text = Reflect.ToStr(Reflect.Get(obj, "FullName", "fullName"));
		object obj3 = Reflect.Get(obj, "affixes", "Affixes");
		int num = Reflect.Count(obj3);
		bool flag = Items.IsUnique(obj);
		bool isSetItem = Items.IsSet(obj);
		bool isCorruptedItem = Items.IsCorrupted(obj);
		bool isSetLike = isSetItem && Cfg.SetItems.Value && !isCorruptedItem;
		if ((flag && num == 0) || isSetLike)
		{
			int num2 = Reflect.ToInt(Reflect.Get(obj, "weaversWill", "WeaversWill"), 0);
			if (num2 > 0)
			{
				text = text + " [" + num2 + "] 刮";
			}
			int num3 = Reflect.ToInt(Reflect.Get(obj, "legendaryPotential", "LegendaryPotential"), 0);
			if (num3 > 0)
			{
				text = text + " " + Rolls.Col(Rolls.PotentialHex(num3), "[" + Rolls.CnNumber(num3) + "孔]");
			}
		}
		if (Items.IsExalted(obj) && num > 0)
		{
			string text2 = "";
			foreach (object item in Reflect.Each(obj3))
			{
				if (item == null)
				{
					continue;
				}
				int num4 = Reflect.ToInt(Reflect.Get(item, "DisplayTier", "displayTier", "affixTier"), 0);
				if (num4 >= Cfg.GroundMinTier.Value)
				{
					string str = Reflect.ToStr(Reflect.Get(item, "affixName", "AffixName"));
					text2 += Rolls.Col(Rolls.TierHex(num4), "[T" + num4 + "][" + str + "]");
				}
			}
			text = text2 + ShortName(text);
		}
		bool flag2 = Reflect.ToBool(Reflect.Get(label, "emphasized", "Emphasized"));
		Reflect.Set(obj2, "text", flag2 ? text.ToUpper() : text);
		if (Cfg.Diag.Value)
		{
			Log.Msg("[AffixRoll] 标签 -> " + Short(text, 110));
		}
	}

	private static string ShortName(string full)
	{
		if (string.IsNullOrEmpty(full))
		{
			return full;
		}
		string[] array = full.Split(new string[1] { "的" }, StringSplitOptions.None);
		if (array != null && array.Length > 1)
		{
			return array[1];
		}
		return array[0];
	}

	private static string Short(string s, int max)
	{
		if (string.IsNullOrEmpty(s))
		{
			return "''";
		}
		if (s.Length > max)
		{
			return "'" + s.Substring(0, max) + "...'";
		}
		return "'" + s + "'";
	}

	private static object FindTextInLabel(object label)
	{
		object obj = Reflect.Get(label, "pickupableLabel");
		if (obj != null)
		{
			object obj2 = Reflect.Get(obj, "itemText", "ItemText");
			if (obj2 != null)
			{
				return obj2;
			}
		}
		return null;
	}
}
