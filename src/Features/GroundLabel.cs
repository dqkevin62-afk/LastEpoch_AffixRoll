using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using LastEpoch_AffixRoll.Core;

namespace LastEpoch_AffixRoll.Features;

public static class GroundLabel
{
	public static string HookName = "none";

	// S5 之前只钩 SetGroundTooltipText（当时是 1 个参数）。
	// S5 起它变成无参 private，而真正往标签上写文本的是 SetLabelText(bool recalculateItemName)。
	// 两个都挂：Apply() 每次都从 ItemData.FullName 重新拼，重复执行结果一致，不会叠加。
	private static readonly string[] HookCandidates = new string[2] { "SetLabelText", "SetGroundTooltipText" };

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
		List<string> hooked = new List<string>();
		MethodInfo[] array = methods;
		foreach (MethodInfo methodInfo in array)
		{
			bool matched = false;
			string[] hookCandidates = HookCandidates;
			foreach (string candidate in hookCandidates)
			{
				if (string.Equals(methodInfo.Name, candidate, StringComparison.Ordinal))
				{
					matched = true;
					break;
				}
			}
			if (!matched)
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
			// 0 个或 1 个参数都接受：Postfix 只取 __instance，多出来的原参数可以不管
			if (parameters == null || parameters.Length > 1)
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
				hooked.Add(methodInfo.Name + "(" + parameters.Length + ")");
			}
			catch (Exception ex)
			{
				Log.WarnOnce("Ground:patch", "[AffixRoll] 挂载地面标签失败: " + ex.Message);
			}
		}
		if (hooked.Count > 0)
		{
			HookName = string.Join(" + ", hooked);
			_resolved = true;
			Log.Msg("[AffixRoll] 地面标签钩子已挂载: " + HookName);
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
		bool flag = Items.IsUniqueOrLegendary(obj);
		bool isSetItem = Items.IsSet(obj);
		bool isLegendaryItem = Items.IsLegendary(obj);
		bool isCorruptedItem = Items.IsCorrupted(obj);
		bool isSetLike = isSetItem && Cfg.SetItems.Value && !isCorruptedItem;
		int num2 = Reflect.ToInt(Reflect.Get(obj, "weaversWill", "WeaversWill"), 0);
		int num3 = Reflect.ToInt(Reflect.Get(obj, "legendaryPotential", "LegendaryPotential"), 0);
		if ((flag && num == 0) || isSetLike)
		{
			if (num2 > 0)
			{
				text = text + " [" + num2 + "] 刮";
			}
			if (num3 > 0)
			{
				text = text + " " + Rolls.Col(Rolls.PotentialHex(num3), "[" + Rolls.CnNumber(num3) + "孔]");
			}
		}
		// 传奇物品不是 isExaltedItem，之前只判 IsExalted 会把它的传奇词缀整个漏掉。
		// 另外传奇的熔铸词条是从崇高物品里随机抽的，经常只有 T1~T5；若沿用 GroundMinTier(默认 6)
		// 会把前缀整条滤空、标签只剩名字 —— 所以传奇单独走 LegendaryMinTier(默认 1)。
		int minTier = (isLegendaryItem ? Cfg.LegendaryMinTier.Value : Cfg.GroundMinTier.Value);
		if ((Items.IsExalted(obj) || isLegendaryItem) && num > 0)
		{
			string text2 = "";
			foreach (object item in Reflect.Each(obj3))
			{
				if (item == null)
				{
					continue;
				}
				int num4 = Reflect.ToInt(Reflect.Get(item, "DisplayTier", "displayTier", "affixTier"), 0);
				if (num4 >= minTier)
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
			Log.Msg("[AffixRoll] 标签 " + Short(text, 110) + " | 暗金=" + Items.IsUnique(obj) + " 传奇=" + isLegendaryItem + " 崇高=" + Items.IsExalted(obj) + " 套装=" + isSetItem + " 腐化=" + isCorruptedItem + " LP=" + num3 + " 编织=" + num2 + " 词缀数=" + num + " 门槛=" + minTier + " 词缀T=" + TierList(obj3));
		}
	}

	// 诊断用：把物品身上每条词缀的 T 级摊开，用来判断「没显示」是门槛滤掉了还是根本没有词缀。
	private static string TierList(object affixes)
	{
		string text = "";
		foreach (object item in Reflect.Each(affixes))
		{
			if (item == null)
			{
				continue;
			}
			int num = Reflect.ToInt(Reflect.Get(item, "DisplayTier", "displayTier", "affixTier"), 0);
			text = ((text.Length > 0) ? (text + ",") : text) + "T" + num;
		}
		return (text.Length > 0) ? text : "-";
	}

	private static string ShortName(string full)
	{
		if (string.IsNullOrEmpty(full))
		{
			return full;
		}
		// FullName 形如 "<词缀>的<物品名>"；但如果物品带多条前缀（传奇经常是 2~4 条），
		// 会是 "A的B的C的物品名"，这时候必须取最后一段，写死取 array[1] 会把真正的物品名砍没。
		string[] array = full.Split(new string[1] { "的" }, StringSplitOptions.None);
		if (array != null && array.Length > 1)
		{
			return array[array.Length - 1];
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
