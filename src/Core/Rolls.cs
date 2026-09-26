using System;
using System.Globalization;

namespace LastEpoch_AffixRoll.Core;

public static class Rolls
{
	private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

	private static readonly string[] CnNum = new string[5] { "零", "一", "二", "三", "四" };

	private static readonly string[] LpHex = new string[5] { "#FFFFFF", "#16FF0E", "#77ACFF", "#A807FF", "#FF0000" };

	public static string Grade(double pct)
	{
		if (pct < 50.0)
		{
			return "差";
		}
		if (pct < 70.0)
		{
			return "中";
		}
		if (pct < 80.0)
		{
			return "良";
		}
		if (pct < 95.0)
		{
			return "优";
		}
		return "极";
	}

	public static string GradeHex(double pct)
	{
		if (pct < 50.0)
		{
			return "#D2D2D2";
		}
		if (pct < 70.0)
		{
			return "#77ACFF";
		}
		if (pct < 80.0)
		{
			return "#A807FF";
		}
		return "#FA9E3D";
	}

	public static string PctHex(double pct)
	{
		if (pct < 20.0)
		{
			return "#D2D2D2";
		}
		if (pct < 40.0)
		{
			return "#E1E1E1";
		}
		if (pct < 60.0)
		{
			return "#16FF0E";
		}
		if (pct < 70.0)
		{
			return "#77ACFF";
		}
		if (pct < 80.0)
		{
			return "#A807FF";
		}
		return "#FA9E3D";
	}

	public static string TierHex(int tier)
	{
		return tier switch
		{
			1 => "#D2D2D2",
			2 => "#E1E1E1",
			3 => "#16FF0E",
			4 => "#77ACFF",
			5 => "#A807FF",
			6 => "#A807FF",
			_ => "#FA9E3D",
		};
	}

	public static string CnNumber(int n)
	{
		if (n >= 0 && n < CnNum.Length)
		{
			return CnNum[n];
		}
		return n.ToString(Inv);
	}

	public static string PotentialHex(int lp)
	{
		if (lp >= 0 && lp < LpHex.Length)
		{
			return LpHex[lp];
		}
		return "#FF0000";
	}

	public static string Col(string hex, string text)
	{
		return "<color=" + hex + ">" + text + "</color>";
	}

	public static float Clamp01(float v)
	{
		if (float.IsNaN(v) || float.IsInfinity(v))
		{
			return -1f;
		}
		if (v < 0f)
		{
			return 0f;
		}
		if (v > 1f)
		{
			return 1f;
		}
		return v;
	}

	public static string Modify(float ratio, int tier, string finished)
	{
		if (finished == null)
		{
			return finished;
		}
		double num = Math.Round((double)ratio * 100.0, 1);
		string text = num.ToString("0.#", Inv);
		string text2 = ((tier > 0) ? Col(TierHex(tier), "T" + tier) : "");
		string text3 = Col(GradeHex(num), Grade(num));
		string text4 = " " + Col(PctHex(num), "[" + text + "%]");
		if (finished.IndexOf("[" + text + "%]", StringComparison.Ordinal) >= 0)
		{
			return finished;
		}
		string text5 = "[" + text2 + text3 + "] ";
		int num2 = finished.LastIndexOf('\n');
		if (num2 < 0)
		{
			return text5 + finished + text4;
		}
		return text5 + finished.Insert(num2, text4);
	}
}
