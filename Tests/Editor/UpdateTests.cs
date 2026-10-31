using NUnit.Framework;

namespace ITKamianets.Engine.Update.Tests
{
	public class UpdateTests
	{
		[TestCase("v0.3.1", "0.3.0", true)]
		[TestCase("0.3.0", "0.3.0", false)]
		[TestCase("v0.2.9", "0.3.0", false)]
		[TestCase("v1.0.0", "0.9.9", true)]
		[TestCase("v0.10.0", "0.9.0", true)]
		[TestCase("v1.0.0-beta", "0.9.9", true)]
		[TestCase("not a version", "0.3.0", false)]
		[TestCase("v0.1.0", "", true)]
		public void Newer_versions_are_found(string latest, string current, bool expected)
		{
			Assert.AreEqual(expected, SemVer.IsNewer(latest, current));
		}

		const string Json = "{\"tag_name\":\"v0.4.0\",\"name\":\"Hippodrome 0.4.0\",\"body\":\"Notes\",\"html_url\":\"https://github.com/x/y/releases/tag/v0.4.0\",\"draft\":false,\"prerelease\":false,"
			+ "\"assets\":[{\"name\":\"Temnorid-Hippodrome-Setup.exe\",\"browser_download_url\":\"https://example.com/setup.exe\",\"size\":123},{\"name\":\"game.zip\",\"browser_download_url\":\"https://example.com/game.zip\",\"size\":9}]}";

		[Test]
		public void A_github_release_is_read_and_its_installer_found()
		{
			var release = ReleaseInfo.FromJson(Json);
			Assert.IsNotNull(release);
			Assert.AreEqual("0.4.0", release.Version);
			var installer = release.FindAsset("-Setup.exe");
			Assert.IsNotNull(installer);
			Assert.AreEqual("https://example.com/setup.exe", installer.browser_download_url);
			Assert.IsNull(release.FindAsset(".apk"));
		}

		[Test]
		public void Something_that_is_not_a_release_is_ignored()
		{
			Assert.IsNull(ReleaseInfo.FromJson(""));
			Assert.IsNull(ReleaseInfo.FromJson("{\"message\":\"Not Found\"}"));
			Assert.IsNull(ReleaseInfo.FromJson("<html>"));
		}
	}
}
