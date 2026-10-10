using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace LastEpoch_AffixRoll.Core;

public static class Reflect
{
	public const BindingFlags All = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

	private static Assembly[] _asms;

	private static readonly Dictionary<string, Type> _typeCache = new Dictionary<string, Type>(StringComparer.Ordinal);

	public static void Refresh()
	{
		try
		{
			_asms = AppDomain.CurrentDomain.GetAssemblies();
		}
		catch
		{
			_asms = new Assembly[0];
		}
	}

	public static Type T(string name)
	{
		if (_asms == null)
		{
			Refresh();
		}
		if (_typeCache.TryGetValue(name, out var cached))
		{
			return cached;
		}
		for (int i = 0; i < _asms.Length; i++)
		{
			Type[] types;
			try
			{
				types = _asms[i].GetTypes();
			}
			catch
			{
				continue;
			}
			for (int j = 0; j < types.Length; j++)
			{
				if (types[j] != null && string.Equals(types[j].Name, name, StringComparison.Ordinal))
				{
					_typeCache[name] = types[j];
					return types[j];
				}
			}
		}
		_typeCache[name] = null;
		return null;
	}

	public static object Get(object target, params string[] names)
	{
		if (target == null || names == null)
		{
			return null;
		}
		for (int i = 0; i < names.Length; i++)
		{
			for (Type cur = target.GetType(); cur != null; cur = cur.BaseType)
			{
				PropertyInfo p;
				try
				{
					p = cur.GetProperty(names[i], All);
				}
				catch
				{
					p = null;
				}
				if (p != null)
				{
					try
					{
						object v = p.GetValue(target);
						if (v != null)
						{
							return v;
						}
					}
					catch
					{
					}
				}
				FieldInfo f;
				try
				{
					f = cur.GetField(names[i], All);
				}
				catch
				{
					f = null;
				}
				if (f != null)
				{
					try
					{
						object v2 = f.GetValue(target);
						if (v2 != null)
						{
							return v2;
						}
					}
					catch
					{
					}
				}
			}
		}
		return null;
	}

	public static object GetStatic(Type t, params string[] names)
	{
		if (t == null || names == null)
		{
			return null;
		}
		for (int i = 0; i < names.Length; i++)
		{
			PropertyInfo p;
			try
			{
				p = t.GetProperty(names[i], All);
			}
			catch
			{
				p = null;
			}
			if (p != null)
			{
				try
				{
					object v = p.GetValue(null);
					if (v != null)
					{
						return v;
					}
				}
				catch
				{
				}
			}
			FieldInfo f;
			try
			{
				f = t.GetField(names[i], All);
			}
			catch
			{
				f = null;
			}
			if (f != null)
			{
				try
				{
					object v2 = f.GetValue(null);
					if (v2 != null)
					{
						return v2;
					}
				}
				catch
				{
				}
			}
		}
		return null;
	}

	public static void Set(object target, string name, object value)
	{
		if (target == null || string.IsNullOrEmpty(name))
		{
			return;
		}
		for (Type cur = target.GetType(); cur != null; cur = cur.BaseType)
		{
			PropertyInfo p;
			try
			{
				p = cur.GetProperty(name, All);
			}
			catch
			{
				p = null;
			}
			if (p != null && p.CanWrite)
			{
				try
				{
					p.SetValue(target, value);
					return;
				}
				catch
				{
				}
			}
			FieldInfo f;
			try
			{
				f = cur.GetField(name, All);
			}
			catch
			{
				f = null;
			}
			if (f != null && !f.IsLiteral)
			{
				try
				{
					f.SetValue(target, value);
					return;
				}
				catch
				{
				}
			}
		}
	}

	public static object Call(object target, string[] names, params object[] args)
	{
		if (target == null || names == null)
		{
			return null;
		}
		for (int i = 0; i < names.Length; i++)
		{
			for (Type cur = target.GetType(); cur != null; cur = cur.BaseType)
			{
				MethodInfo[] ms;
				try
				{
					ms = cur.GetMethods(All);
				}
				catch
				{
					continue;
				}
				for (int k = 0; k < ms.Length; k++)
				{
					MethodInfo m = ms[k];
					if (!string.Equals(m.Name, names[i], StringComparison.Ordinal))
					{
						continue;
					}
					ParameterInfo[] ps;
					try
					{
						ps = m.GetParameters();
					}
					catch
					{
						continue;
					}
					if (ps == null || ps.Length != ((args == null) ? 0 : args.Length))
					{
						continue;
					}
					try
					{
						return m.Invoke(target, args);
					}
					catch
					{
					}
				}
			}
		}
		return null;
	}

	// 兼容性自检用：只问「这个方法在不在」，不调用。
	public static bool HasMethod(Type t, string name)
	{
		if (t == null || string.IsNullOrEmpty(name))
		{
			return false;
		}
		for (Type cur = t; cur != null; cur = cur.BaseType)
		{
			MethodInfo[] ms;
			try
			{
				ms = cur.GetMethods(All);
			}
			catch
			{
				continue;
			}
			if (ms == null)
			{
				continue;
			}
			for (int i = 0; i < ms.Length; i++)
			{
				if (ms[i] != null && string.Equals(ms[i].Name, name, StringComparison.Ordinal))
				{
					return true;
				}
			}
		}
		return false;
	}

	public static int Count(object collection)
	{
		if (collection == null)
		{
			return -1;
		}
		if (collection is Array arr)
		{
			return arr.Length;
		}
		if (collection is IList list)
		{
			return list.Count;
		}
		for (Type cur = collection.GetType(); cur != null; cur = cur.BaseType)
		{
			PropertyInfo p;
			try
			{
				p = cur.GetProperty("Count", All);
			}
			catch
			{
				p = null;
			}
			if (p != null)
			{
				try
				{
					return Convert.ToInt32(p.GetValue(collection), CultureInfo.InvariantCulture);
				}
				catch
				{
				}
			}
		}
		return -1;
	}

	public static object At(object collection, int index)
	{
		if (collection == null || index < 0)
		{
			return null;
		}
		if (collection is Array arr)
		{
			if (index < arr.Length)
			{
				return arr.GetValue(index);
			}
			return null;
		}
		if (collection is IList list)
		{
			if (index < list.Count)
			{
				return list[index];
			}
			return null;
		}
		for (Type cur = collection.GetType(); cur != null; cur = cur.BaseType)
		{
			PropertyInfo p;
			try
			{
				p = cur.GetProperty("Item", All);
			}
			catch
			{
				p = null;
			}
			if (p != null)
			{
				ParameterInfo[] ips;
				try
				{
					ips = p.GetIndexParameters();
				}
				catch
				{
					continue;
				}
				if (ips != null && ips.Length == 1 && ips[0].ParameterType == typeof(int))
				{
					try
					{
						return p.GetValue(collection, new object[1] { index });
					}
					catch
					{
					}
				}
			}
			MethodInfo m;
			try
			{
				m = cur.GetMethod("get_Item", All);
			}
			catch
			{
				m = null;
			}
			if (m != null)
			{
				try
				{
					return m.Invoke(collection, new object[1] { index });
				}
				catch
				{
				}
			}
		}
		return null;
	}

	public static IEnumerable<object> Each(object collection)
	{
		if (collection == null)
		{
			yield break;
		}
		int n = Count(collection);
		if (n < 0)
		{
			if (collection is IEnumerable e2)
			{
				foreach (object item in e2)
				{
					yield return item;
				}
			}
			yield break;
		}
		for (int i = 0; i < n; i++)
		{
			yield return At(collection, i);
		}
	}

	public static int ToInt(object v, int fallback)
	{
		if (v == null)
		{
			return fallback;
		}
		try
		{
			return Convert.ToInt32(v, CultureInfo.InvariantCulture);
		}
		catch
		{
			return fallback;
		}
	}

	public static float ToSingle(object v, float fallback)
	{
		if (v == null)
		{
			return fallback;
		}
		try
		{
			return Convert.ToSingle(v, CultureInfo.InvariantCulture);
		}
		catch
		{
			return fallback;
		}
	}

	public static bool ToBool(object v)
	{
		if (v == null)
		{
			return false;
		}
		try
		{
			return Convert.ToBoolean(v, CultureInfo.InvariantCulture);
		}
		catch
		{
			return false;
		}
	}

	public static string ToStr(object v)
	{
		if (v == null)
		{
			return "";
		}
		try
		{
			return v as string ?? Convert.ToString(v, CultureInfo.InvariantCulture) ?? "";
		}
		catch
		{
			return "";
		}
	}
}
