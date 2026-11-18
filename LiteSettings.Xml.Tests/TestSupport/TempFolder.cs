namespace LiteSettings.Xml.Tests.TestSupport;

/// <summary>
/// A private folder under the system temp directory, removed together with its content on dispose.
/// </summary>
internal sealed class TempFolder : IDisposable
{
	public TempFolder()
	{
		Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "LiteSettings.Xml.Tests", Guid.NewGuid().ToString("N"));
	}

	public string Path { get; }

	public void Dispose()
	{
		if (Directory.Exists(Path))
		{
			Directory.Delete(Path, true);
		}
	}
}
