using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace ITKamianets.Engine.Update
{
	public enum UpdateState
	{
		Idle,
		Checking,
		UpToDate,
		Available,
		Downloading,
		Installing,
		Failed,
	}

	/// <summary>
	/// Keeps a game on its newest GitHub release. <see cref="Check"/> reads the latest release of <see cref="Repo"/>;
	/// when it is newer than <see cref="Application.version"/>, <see cref="Install"/> takes the player to it:
	/// on Windows the installer is downloaded and run silently (it replaces the game in place and starts it again),
	/// on Android and iOS the release or store page opens. Being offline is normal and never an error for the player.
	/// </summary>
	public sealed class AutoUpdater : MonoBehaviour
	{
		/// <summary>Owner and name of the GitHub repository, for example <c>WebArtWork/hippodrome.temnorid.com</c>.</summary>
		public string Repo = "";
		/// <summary>File name ending of the Windows installer asset.</summary>
		public string WindowsAssetSuffix = "-Setup.exe";
		/// <summary>File name ending of the Android package asset.</summary>
		public string AndroidAssetSuffix = ".apk";
		/// <summary>Where Android and iOS players go for the update when no store page is given: the release page.</summary>
		public string StoreUrl = "";
		/// <summary>Seconds to wait for the GitHub answer before giving up quietly.</summary>
		public int TimeoutSeconds = 8;

		public UpdateState State { get; private set; } = UpdateState.Idle;
		public ReleaseInfo Latest { get; private set; }
		public float Progress { get; private set; }
		public string Error { get; private set; } = "";
		public string CurrentVersion => Application.version;
		public bool Available => State == UpdateState.Available;

		public event Action Changed;

		/// <summary>Asks GitHub for the latest release. Safe to call again; a check already running is left alone.</summary>
		public void Check()
		{
			if (State == UpdateState.Checking || State == UpdateState.Downloading || State == UpdateState.Installing) return;
			StartCoroutine(CheckRoutine());
		}

		public IEnumerator CheckRoutine()
		{
			if (string.IsNullOrEmpty(Repo))
			{
				Fail("No repository set.");
				yield break;
			}

			Set(UpdateState.Checking);
			using (var request = UnityWebRequest.Get("https://api.github.com/repos/" + Repo + "/releases/latest"))
			{
				request.SetRequestHeader("User-Agent", "Temnorid-Updater");
				request.SetRequestHeader("Accept", "application/vnd.github+json");
				request.timeout = Mathf.Max(2, TimeoutSeconds);
				yield return request.SendWebRequest();

				if (request.result != UnityWebRequest.Result.Success)
				{
					Fail(request.error);
					yield break;
				}

				var release = ReleaseInfo.FromJson(request.downloadHandler.text);
				if (release == null || release.draft || release.prerelease)
				{
					Latest = null;
					Set(UpdateState.UpToDate);
					yield break;
				}

				Latest = release;
				Set(SemVer.IsNewer(release.tag_name, CurrentVersion) ? UpdateState.Available : UpdateState.UpToDate);
			}
		}

		/// <summary>Moves the player to the new version. Does nothing unless an update is <see cref="Available"/>.</summary>
		public void Install()
		{
			if (State != UpdateState.Available || Latest == null) return;

			switch (Application.platform)
			{
				case RuntimePlatform.WindowsPlayer:
					var asset = Latest.FindAsset(WindowsAssetSuffix);
					if (asset == null || string.IsNullOrEmpty(asset.browser_download_url)) OpenPage();
					else StartCoroutine(DownloadAndRun(asset));
					break;
				case RuntimePlatform.Android:
				{
					var apk = Latest.FindAsset(AndroidAssetSuffix);
					Application.OpenURL(!string.IsNullOrEmpty(StoreUrl) ? StoreUrl : apk != null ? apk.browser_download_url : Latest.html_url);
					break;
				}
				default:
					OpenPage();
					break;
			}
		}

		void OpenPage()
		{
			var url = !string.IsNullOrEmpty(StoreUrl) ? StoreUrl : Latest?.html_url;
			if (!string.IsNullOrEmpty(url)) Application.OpenURL(url);
		}

		IEnumerator DownloadAndRun(ReleaseInfo.Asset asset)
		{
			var path = Path.Combine(Application.temporaryCachePath, asset.name);
			Progress = 0f;
			Set(UpdateState.Downloading);

			using (var request = UnityWebRequest.Get(asset.browser_download_url))
			{
				request.downloadHandler = new DownloadHandlerFile(path) { removeFileOnAbort = true };
				request.SetRequestHeader("User-Agent", "Temnorid-Updater");
				var operation = request.SendWebRequest();
				while (!operation.isDone)
				{
					Progress = request.downloadProgress;
					Changed?.Invoke();
					yield return null;
				}

				if (request.result != UnityWebRequest.Result.Success)
				{
					Fail(request.error);
					yield break;
				}
			}

			Progress = 1f;
			Set(UpdateState.Installing);
			if (Application.isEditor)
			{
				Debug.Log("[Update] Downloaded " + path + " (not run in the editor).");
				yield break;
			}

			try
			{
				// The Inno Setup installer replaces the game in place and starts it again when it is done.
				Process.Start(new ProcessStartInfo(path, "/SILENT /SUPPRESSMSGBOXES /NOCANCEL /CLOSEAPPLICATIONS") { UseShellExecute = true });
			}
			catch (Exception exception)
			{
				Fail(exception.Message);
				yield break;
			}

			yield return new WaitForSecondsRealtime(0.5f);
			Application.Quit();
		}

		void Fail(string message)
		{
			Error = message ?? "";
			Set(UpdateState.Failed);
		}

		void Set(UpdateState state)
		{
			State = state;
			Changed?.Invoke();
		}
	}
}
