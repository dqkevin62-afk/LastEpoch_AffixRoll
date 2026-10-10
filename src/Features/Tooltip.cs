using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using LastEpoch_AffixRoll.Core;

namespace LastEpoch_AffixRoll.Features;

public static class Tooltip
{
	private enum Kind
	{
		Affix,
		UniqueMod,
		Implicit
	}

	private sealed class Slot
	{
		public string Label = "";

		public Kind Which;

		public int Item = -1;

		public int Affix = -1;

		public int UniqIdx = -1;

		public int ModVal = -1;

		public int ImplNo = -1;
	}

	public static string HookName = "none";

	private static HarmonyLib.Harmony _harmony;

	private static readonly Dictionary<string, Slot> _slots = new Dictionary<string, Slot>(StringComparer.Ordinal);

	private static bool _resolved;

	private static int _tries;

	public static void Resolve()
	{
		if (_resolved || _tries++ > 240)
		{
			return;
		}
		Reflect.Refresh();
		Type type = Reflect.T("TooltipItemManager");
		if (type == null)
		{
			if (_tries % 40 == 1)
			{
				Log.WarnOnce("Tooltip:nomgr", "[AffixRoll] 找不到 TooltipItemManager，装备提示词缀暂不可用。");
			}
			return;
		}
		PatchAll(type, "AffixFormatter", Kind.Affix);
		PatchAll(type, "UniqueBasicModFormatter", Kind.UniqueMod);
		PatchAll(type, "ImplicitFormatter", Kind.Implicit);
		PatchUniqueDescDiag(type);
		if (_slots.Count > 0)
		{
			_resolved = true;
			Log.Msg("[AffixRoll] Tooltip 钩子已挂载: " + HookName);
		}
	}

	private static void PatchAll(Type owner, string name, Kind kind)
	{
		MethodInfo[] methods;
		try
		{
			methods = owner.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		}
		catch
		{
			return;
		}
		MethodInfo[] array = methods;
		foreach (MethodInfo methodInfo in array)
		{
			if (!string.Equals(methodInfo.Name, name, StringComparison.Ordinal) || !methodInfo.IsStatic || methodInfo.ReturnType != typeof(string))
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
			if (parameters == null || parameters.Length == 0)
			{
				continue;
			}
			Slot slot = new Slot();
			slot.Label = owner.Name + "." + methodInfo.Name + "(" + parameters.Length + ")";
			slot.Which = kind;
			slot.Item = Idx(parameters, "item", "itemData");
			slot.Affix = Idx(parameters, "affix", "itemAffix");
			slot.UniqIdx = Idx(parameters, "uniqueModIndex", "uniqueModIdx");
			slot.ModVal = Idx(parameters, "modifierValue", "value");
			slot.ImplNo = Idx(parameters, "implicitNumber", "implicitIndex");
			if (slot.Item < 0)
			{
				slot.Item = IndexOfItem(parameters);
			}
			if (slot.Affix < 0)
			{
				slot.Affix = IndexOfType(parameters, "ItemAffix");
			}
			if (slot.Item < 0)
			{
				Log.WarnOnce("Tooltip:noitem:" + name, "[AffixRoll] " + name + " 定位不到 item 参数，已跳过。");
				continue;
			}
			try
			{
				if (_harmony == null)
				{
					_harmony = new HarmonyLib.Harmony("LastEpoch_AffixRoll.Tooltip");
				}
				MethodInfo method = typeof(Tooltip).GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public);
				_harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(method));
				_slots[methodInfo.Name] = slot;
				HookName = ((HookName == "none") ? slot.Label : (HookName + " + " + slot.Label));
				Log.Msg("[AffixRoll]   " + slot.Label + "  item=" + slot.Item + " affix=" + slot.Affix + " uniqIdx=" + slot.UniqIdx + " modVal=" + slot.ModVal + " implNo=" + slot.ImplNo);
			}
			catch (Exception ex)
			{
				Log.WarnOnce("Tooltip:patch:" + name, "[AffixRoll] 挂载 " + name + " 失败: " + ex.Message);
			}
		}
	}

	// 诊断用（只读，不改显示）：暗金/传奇的「固定词条」整块由
	// TooltipItemManager.GetUniqueDescription(...) 生成一个 List<string>，
	// 我们那几个 Formatter 只负责逐行加标记。所以只要问它「产出了几行」，
	// 就能区分是「游戏根本没产出词条」还是「产出了却被滤掉」。
	private static void PatchUniqueDescDiag(Type owner)
	{
		try
		{
			MethodInfo[] methods = owner.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo methodInfo in methods)
			{
				if (!string.Equals(methodInfo.Name, "GetUniqueDescription", StringComparison.Ordinal))
				{
					continue;
				}
				if (_harmony == null)
				{
					_harmony = new HarmonyLib.Harmony("LastEpoch_AffixRoll.Tooltip");
				}
				MethodInfo method = typeof(Tooltip).GetMethod("PostfixUniqueDesc", BindingFlags.Static | BindingFlags.Public);
				_harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(method));
				Log.Msg("[AffixRoll]   诊断钩子已挂载(只读): TooltipItemManager.GetUniqueDescription");
				return;
			}
		}
		catch (Exception ex)
		{
			Log.WarnOnce("Tooltip:patchdesc", "[AffixRoll] 挂载 GetUniqueDescription 诊断钩子失败: " + ex.Message);
		}
	}

	public static void PostfixUniqueDesc(object[] __args, object __result)
	{
		try
		{
			if (!Cfg.Diag.Value)
			{
				return;
			}
			object obj = ((__args != null && __args.Length > 0) ? __args[0] : null);
			Log.Msg("[AffixRoll] 暗金词条块 " + Reflect.ToStr(Reflect.Get(obj, "FullName", "fullName"))
				+ " | 产出行数=" + Reflect.Count(__result)
				+ " | 传入 base=" + Reflect.ToInt(Arg(__args, 1), -1)
				+ " sub=" + Reflect.ToInt(Arg(__args, 2), -1)
				+ " uniqueId=" + Reflect.ToInt(Arg(__args, 3), -1)
				+ " | 物品 rarity=" + Reflect.ToInt(Reflect.Get(obj, "rarity", "Rarity"), -1)
				+ " uniqueID=" + Reflect.ToInt(Reflect.Get(obj, "uniqueID", "uniqueId", "UniqueID"), -1)
				+ " 暗金=" + Items.IsUnique(obj) + " 传奇=" + Items.IsLegendary(obj) + " 崇高=" + Items.IsExalted(obj)
				+ " uniqueRolls=" + Reflect.Count(Reflect.Get(obj, "uniqueRolls", "UniqueRolls"))
				+ " 隐式=" + Reflect.Count(Reflect.Get(obj, "implicits", "Implicits"))
				+ " 词缀=" + Reflect.Count(Reflect.Get(obj, "affixes", "Affixes")));
		}
		catch (Exception ex)
		{
			Log.WarnOnce("Tooltip:descpost", "[AffixRoll] GetUniqueDescription 诊断异常: " + ex.Message);
		}
	}

	private static int Idx(ParameterInfo[] ps, params string[] names)
	{
		for (int i = 0; i < ps.Length; i++)
		{
			string name;
			try
			{
				name = ps[i].Name;
			}
			catch
			{
				continue;
			}
			if (string.IsNullOrEmpty(name))
			{
				continue;
			}
			for (int j = 0; j < names.Length; j++)
			{
				if (string.Equals(name, names[j], StringComparison.Ordinal))
				{
					return i;
				}
			}
		}
		return -1;
	}

	private static int IndexOfItem(ParameterInfo[] ps)
	{
		for (int i = 0; i < ps.Length; i++)
		{
			string name;
			try
			{
				name = ps[i].ParameterType.Name;
			}
			catch
			{
				continue;
			}
			if (name == "ItemDataUnpacked" || name == "ItemData")
			{
				return i;
			}
		}
		return -1;
	}

	private static int IndexOfType(ParameterInfo[] ps, string typeName)
	{
		for (int i = 0; i < ps.Length; i++)
		{
			string name;
			try
			{
				name = ps[i].ParameterType.Name;
			}
			catch
			{
				continue;
			}
			if (string.Equals(name, typeName, StringComparison.Ordinal))
			{
				return i;
			}
		}
		return -1;
	}

	public static void Postfix(object[] __args, ref string __result, MethodBase __originalMethod)
	{
		try
		{
			if (!Cfg.TooltipRoll.Value || __args == null || string.IsNullOrEmpty(__result) || __originalMethod == null)
			{
				return;
			}
			if (!_slots.TryGetValue(__originalMethod.Name, out var value))
			{
				return;
			}
			object obj = Arg(__args, value.Item);
			if (obj == null)
			{
				return;
			}
			switch (value.Which)
			{
			case Kind.Affix:
			{
				object obj3 = Arg(__args, value.Affix);
				if (obj3 != null)
				{
					float num2 = Rolls.Clamp01(Reflect.ToSingle(Reflect.Call(obj3, new string[2] { "getRollFloat", "GetRollFloat" }), -1f));
					if (!(num2 < 0f))
					{
						int tier = Reflect.ToInt(Reflect.Get(obj3, "DisplayTier", "displayTier", "affixTier"), 0);
						__result = Rolls.Modify(num2, tier, __result);
						Sample(value.Label, __result);
					}
				}
				break;
			}
			case Kind.UniqueMod:
			{
				if (SkipSet(obj))
				{
					break;
				}
				int num = Reflect.ToInt(Arg(__args, value.UniqIdx), -1);
				float num3 = Reflect.ToSingle(Arg(__args, value.ModVal), 0f);
				float num4 = UniqueRatio(obj, num, num3);
				if (!(num4 < 0f))
				{
					__result = Rolls.Modify(num4, -1, __result);
					Sample(value.Label, __result);
				}
				else if (Cfg.Diag.Value)
				{
					Log.Msg("[AffixRoll] " + value.Label + " 跳过(拿不到 roll) 物品=" + Reflect.ToStr(Reflect.Get(obj, "FullName", "fullName")) + " uniqId=" + Reflect.ToInt(Reflect.Get(obj, "uniqueID", "uniqueId", "UniqueID"), -1) + " idx=" + num + " 原行=" + Clip(__result, 60));
				}
				break;
			}
			case Kind.Implicit:
			{
				if (SkipSet(obj))
				{
					break;
				}
				int n = Reflect.ToInt(Arg(__args, value.ImplNo), -1);
				float num5 = ImplicitRatio(obj, n);
				if (!(num5 < 0f))
				{
					__result = Rolls.Modify(num5, -1, __result);
					Sample(value.Label, __result);
				}
				else if (Cfg.Diag.Value)
				{
					Log.Msg("[AffixRoll] " + value.Label + " 跳过(拿不到 roll) 物品=" + Reflect.ToStr(Reflect.Get(obj, "FullName", "fullName")) + " 隐式#" + n + " 原行=" + Clip(__result, 60));
				}
				break;
			}
			}
		}
		catch (Exception ex)
		{
			Log.WarnOnce("Tooltip:postfix", "[AffixRoll] Tooltip 改写异常: " + ex.Message);
		}
	}

	private static void Sample(string label, string after)
	{
		if (Cfg.Diag.Value)
		{
			Log.Msg("[AffixRoll] " + label + " -> " + Clip(after, 100));
		}
	}

	private static string Clip(string s, int max)
	{
		if (string.IsNullOrEmpty(s))
		{
			return "''";
		}
		string text = s.Replace("\n", "\\n");
		if (text.Length > max)
		{
			return "'" + text.Substring(0, max) + "...'";
		}
		return "'" + text + "'";
	}

	private static object Arg(object[] args, int i)
	{
		if (args == null || i < 0 || i >= args.Length)
		{
			return null;
		}
		return args[i];
	}

	private static bool SkipSet(object item)
	{
		if (!Items.IsSet(item))
		{
			return false;
		}
		if (!Cfg.SetItems.Value)
		{
			return true;
		}
		return Items.IsCorrupted(item);
	}

	private static float UniqueRatio(object item, int modIndex, float modifierValue)
	{
		if (modIndex < 0)
		{
			return -1f;
		}
		int num = Reflect.ToInt(Reflect.Get(item, "uniqueID", "uniqueId", "UniqueID"), -1);
		if (Items.IsSet(item))
		{
			int setId = Items.SetItemUniqueId(item);
			if (setId > 0)
			{
				num = setId;
			}
		}
		if (num < 0)
		{
			return -1f;
		}
		object obj = UniqueEntry(num);
		if (obj == null)
		{
			return -1f;
		}
		object obj2 = Reflect.At(Reflect.Get(obj, "mods", "Mods"), modIndex);
		if (obj2 == null)
		{
			return -1f;
		}
		float num2 = Reflect.ToSingle(Reflect.Get(obj2, "value", "Value"), 0f);
		float num3 = Reflect.ToSingle(Reflect.Get(obj2, "maxValue", "MaxValue"), 0f);
		if (num2 == 0f && num3 == 0f)
		{
			return -1f;
		}
		float num4 = ((num2 == num3 || modifierValue > num3) ? 1f : ((modifierValue - num2) / (num3 - num2)));
		return Rolls.Clamp01(num4);
	}

	private static object UniqueEntry(int uniqueId)
	{
		Type type = Reflect.T("UniqueList");
		if (type == null)
		{
			return null;
		}
		object obj = Reflect.Get(Reflect.GetStatic(type, "instance", "Instance"), "uniques", "Uniques");
		if (obj == null)
		{
			return null;
		}
		int num = Reflect.Count(obj);
		if (num >= 0 && uniqueId >= num)
		{
			return null;
		}
		return Reflect.At(obj, uniqueId);
	}

	private static float ImplicitRatio(object item, int n)
	{
		if (n < 0)
		{
			return -1f;
		}
		object obj = Reflect.Call(item, new string[3] { "getImplictRollFloat", "GetImplictRollFloat", "getImplicitRollFloat" }, (byte)n);
		if (obj != null)
		{
			float num = Reflect.ToSingle(obj, -1f);
			if (num >= 0f)
			{
				return Rolls.Clamp01(num);
			}
		}
		object obj2 = Reflect.At(Reflect.Get(item, "implicitRolls", "ImplicitRolls"), n);
		if (obj2 == null)
		{
			return -1f;
		}
		int num2 = Reflect.ToInt(obj2, -1);
		if (num2 < 0)
		{
			return -1f;
		}
		float num3 = Cfg.ImplicitDivisor.Value;
		if (num3 <= 0f)
		{
			num3 = 255f;
		}
		if (Cfg.Diag.Value)
		{
			Log.Msg("[AffixRoll] 隐式原始值 #" + n + " = " + num2 + "，除以 " + num3 + " = " + (float)num2 / num3);
		}
		return Rolls.Clamp01((float)num2 / num3);
	}
}
