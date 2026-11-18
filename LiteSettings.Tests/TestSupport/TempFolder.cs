namespace LiteSettings.Tests.TestSupport;

/// <summary>
/// A private folder under the system temp directory, removed together with its content on dispose.
/// The folder itself is not created: tests decide whether it should exist.
/// </summary>
internal sealed class TempFolder : IDisposable
{
	public TempFolder()
	{
		Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "LiteSettings.Tests", Guid.NewGuid().ToString("N"));
	}

	public string Path { get; }

	public string Combine(params string[] parts)
	{
		return System.IO.Path.Combine([Path, .. parts]);
	}

	public void Dispose()
	{
		if (Directory.Exists(Path))
		{
			Directory.Delete(Path, true);
		}
	}
}
