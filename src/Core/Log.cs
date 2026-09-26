using System.Collections.Generic;
using MelonLoader;

namespace LastEpoch_AffixRoll.Core;

public static class Log
{
	private static readonly HashSet<string> _once = new HashSet<string>();

	public static void Msg(string s)
	{
		try
		{
			MelonLogger.Msg(s);
		}
		catch
		{
		}
	}

	public static void Warn(string s)
	{
		try
		{
			MelonLogger.Warning(s);
		}
		catch
		{
		}
	}

	public static void Error(string s)
	{
		try
		{
			MelonLogger.Error(s);
		}
		catch
		{
		}
	}

	public static void MsgOnce(string key, string s)
	{
		if (_once.Add("m:" + key))
		{
			Msg(s);
		}
	}

	public static void WarnOnce(string key, string s)
	{
		if (_once.Add("w:" + key))
		{
			Warn(s);
		}
	}
}
