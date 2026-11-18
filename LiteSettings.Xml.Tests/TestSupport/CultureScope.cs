using System.Globalization;

namespace LiteSettings.Xml.Tests.TestSupport;

/// <summary>
/// Switches the current culture until disposed. An empty name means the invariant culture.
/// </summary>
internal sealed class CultureScope : IDisposable
{
	private readonly CultureInfo _previous = CultureInfo.CurrentCulture;

	public CultureScope(string name)
	{
		CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);
	}

	public void Dispose()
	{
		CultureInfo.CurrentCulture = _previous;
	}
}
