using MelonLoader;
using MelonLoader.Preferences;

namespace LastEpoch_AffixRoll.Core;

public static class Cfg
{
	private static MelonPreferences_Category _cat;

	public static MelonPreferences_Entry<bool> TooltipRoll;

	public static MelonPreferences_Entry<bool> GroundLabel;

	public static MelonPreferences_Entry<bool> SetItems;

	public static MelonPreferences_Entry<int> GroundMinTier;

	public static MelonPreferences_Entry<float> ImplicitDivisor;

	public static MelonPreferences_Entry<bool> Diag;

	public static void Init()
	{
		_cat = MelonPreferences.CreateCategory("LastEpoch_AffixRoll");
		TooltipRoll = _cat.CreateEntry<bool>("TooltipRoll", true, "Tooltip affix roll（装备提示词条）", "在装备悬停提示的每行词条上，追加 [T级+评级] 与 Roll 百分比。", false, false, (ValueValidator)null, (string)null);
		GroundLabel = _cat.CreateEntry<bool>("GroundLabel", true, "Ground label rewrite（地面掉落标签）", "改写地面掉落标签：暗金/传奇显示孔数与编织意志，崇高与传奇物品把高阶词缀拼到名字前。", false, false, (ValueValidator)null, (string)null);
		SetItems = _cat.CreateEntry<bool>("SetItems", true, "Set items（绿色套装件）", "套装件按暗金一样显示词条 Roll、隐式 Roll，地面标签也显示孔数与编织意志。仅处理未腐化的套装（腐化套装的索引会错位）。", false, false, (ValueValidator)null, (string)null);
		GroundMinTier = _cat.CreateEntry<int>("GroundMinTier", 6, "Ground min tier（标签显示 T 几以上）", "地面标签只显示 T 级大于等于此值的词缀。6=只显示 T6/T7/T8。", false, false, (ValueValidator)null, (string)null);
		ImplicitDivisor = _cat.CreateEntry<float>("ImplicitDivisor", 255f, "Implicit roll divisor（隐式 Roll 分母）", "隐式词条的原始值是 0~N 的整数，除以这个数得到百分比。若隐式百分比明显偏低，把它调小（如 100）。", false, false, (ValueValidator)null, (string)null);
		Diag = _cat.CreateEntry<bool>("Diag", false, "Diagnostic log（排查日志）", "开启后在日志里输出每条词缀的改写结果，用于排查不显示的问题。", false, false, (ValueValidator)null, (string)null);
		_cat.SetFilePath("UserData/LastEpoch_AffixRoll.cfg", true);
		_cat.SaveToFile(false);
	}
}
