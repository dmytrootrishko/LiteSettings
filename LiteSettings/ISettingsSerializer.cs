namespace LiteSettings;

/// <summary>
/// Converts a settings object to and from one file format.
/// </summary>
/// <remarks>
/// A serializer only deals with content. Where the file lives, how it is written
/// and what happens when it is locked is the job of <see cref="SettingsStore{T}"/>.
/// Use <see cref="SaveMetadata"/> to find out which properties must be saved.
/// </remarks>
public interface ISettingsSerializer
{
	/// <summary>
	/// File name used when <see cref="SettingsStoreOptions.FileName"/> is not set, for example <c>settings.xml</c>.
	/// </summary>
	string DefaultFileName { get; }

	/// <summary>
	/// Writes <paramref name="settings"/> to <paramref name="stream"/>.
	/// </summary>
	/// <param name="stream">Writable stream that receives the whole document.</param>
	/// <param name="settings">Object whose marked properties are written.</param>
	void Serialize(Stream stream, object settings);

	/// <summary>
	/// Reads a document from <paramref name="stream"/> and copies its values into <paramref name="target"/>.
	/// </summary>
	/// <remarks>
	/// Properties missing from the document keep their current values.
	/// A value that cannot be converted is skipped, the rest of the document is still applied.
	/// </remarks>
	/// <param name="stream">Readable stream positioned at the start of the document.</param>
	/// <param name="target">Object whose marked properties are assigned.</param>
	/// <exception cref="InvalidDataException">
	/// The stream does not contain a readable document of this format. Nothing has been assigned.
	/// </exception>
	void Populate(Stream stream, object target);
}
