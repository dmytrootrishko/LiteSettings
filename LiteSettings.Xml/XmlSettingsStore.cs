namespace LiteSettings.Xml;

/// <summary>
/// Shortcut for creating a <see cref="SettingsStore{T}"/> that keeps settings in <c>settings.xml</c>.
/// </summary>
public static class XmlSettingsStore
{
	/// <summary>
	/// Creates a store that uses <see cref="XmlSettingsSerializer"/> with the default root element.
	/// </summary>
	/// <typeparam name="T">Settings class.</typeparam>
	/// <param name="options">Optional overrides; <see langword="null"/> means the defaults.</param>
	public static SettingsStore<T> Create<T>(SettingsStoreOptions? options = null) where T : class, new()
	{
		return new SettingsStore<T>(new XmlSettingsSerializer(), options);
	}
}
