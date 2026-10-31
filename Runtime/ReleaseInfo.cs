using System;
using UnityEngine;

namespace ITKamianets.Engine.Update
{
	/// <summary>The parts of a GitHub release this package needs. Field names follow the GitHub API so <see cref="JsonUtility"/> fills them.</summary>
	[Serializable]
	public sealed class ReleaseInfo
	{
		[Serializable]
		public sealed class Asset
		{
			public string name;
			public string browser_download_url;
			public long size;
		}

		public string tag_name;
		public string name;
		public string body;
		public string html_url;
		public bool prerelease;
		public bool draft;
		public Asset[] assets = Array.Empty<Asset>();

		/// <summary>Reads the JSON that <c>GET /repos/{owner}/{repo}/releases/latest</c> returns. Null when it is not a release.</summary>
		public static ReleaseInfo FromJson(string json)
		{
			if (string.IsNullOrWhiteSpace(json)) return null;
			try
			{
				var release = JsonUtility.FromJson<ReleaseInfo>(json);
				if (release == null || string.IsNullOrEmpty(release.tag_name)) return null;
				release.assets ??= Array.Empty<Asset>();
				return release;
			}
			catch (ArgumentException)
			{
				return null;
			}
		}

		/// <summary>The first asset whose file name ends with <paramref name="suffix"/> (case-insensitive), or null.</summary>
		public Asset FindAsset(string suffix)
		{
			if (string.IsNullOrEmpty(suffix)) return null;
			foreach (var asset in assets)
				if (asset != null && !string.IsNullOrEmpty(asset.name) && asset.name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
					return asset;
			return null;
		}

		public string Version => tag_name != null && tag_name.Length > 0 && (tag_name[0] == 'v' || tag_name[0] == 'V') ? tag_name.Substring(1) : tag_name;
	}
}
