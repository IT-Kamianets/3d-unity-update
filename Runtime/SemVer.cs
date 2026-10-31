using System;

namespace ITKamianets.Engine.Update
{
	/// <summary>Compares release tags such as <c>v0.3.1</c> with a game's <c>Application.version</c>.</summary>
	public static class SemVer
	{
		/// <summary>Reads <c>v1.2.3</c>, <c>1.2</c> or <c>1.2.3-beta</c> into three numbers. Missing parts are zero.</summary>
		public static bool TryParse(string text, out int major, out int minor, out int patch)
		{
			major = minor = patch = 0;
			if (string.IsNullOrWhiteSpace(text)) return false;

			var s = text.Trim();
			if (s.Length > 0 && (s[0] == 'v' || s[0] == 'V')) s = s.Substring(1);
			var cut = s.IndexOfAny(new[] { '-', '+', ' ' });
			if (cut >= 0) s = s.Substring(0, cut);

			var parts = s.Split('.');
			if (parts.Length == 0 || parts.Length > 4) return false;
			var numbers = new int[3];
			for (var i = 0; i < parts.Length && i < 3; i++)
				if (!int.TryParse(parts[i], out numbers[i]) || numbers[i] < 0) return false;

			major = numbers[0];
			minor = numbers[1];
			patch = numbers[2];
			return true;
		}

		/// <summary>True when <paramref name="latest"/> is a higher version than <paramref name="current"/>. Anything unreadable is not newer.</summary>
		public static bool IsNewer(string latest, string current)
		{
			if (!TryParse(latest, out var a, out var b, out var c)) return false;
			if (!TryParse(current, out var x, out var y, out var z)) return true;
			if (a != x) return a > x;
			if (b != y) return b > y;
			return c > z;
		}
	}
}
