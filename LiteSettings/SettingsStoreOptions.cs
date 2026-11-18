using System.Reflection;

namespace LiteSettings;

/// <summary>
/// Optional settings of <see cref="SettingsStore{T}"/>. The defaults reproduce the behaviour of the original ProgramSettings library.
/// </summary>
public sealed class SettingsStoreOptions
{
	/// <summary>
	/// Assembly that identifies the application: its company, product and location make up the settings folder.
	/// </summary>
	/// <remarks>
	/// Defaults to the assembly that declares the settings class. Set it when the settings class lives in
	/// a different assembly than the one the folder must be derived from.
	/// Ignored for the folder when <see cref="DirectoryPath"/> is set.
	/// </remarks>
	public Assembly? Assembly { get; init; }

	/// <summary>
	/// Folder for the settings file, used instead of the computed one.
	/// </summary>
	public string? DirectoryPath { get; init; }

	/// <summary>
	/// Name of the settings file inside the folder. Defaults to <see cref="ISettingsSerializer.DefaultFileName"/>.
	/// </summary>
	public string? FileName { get; init; }

	/// <summary>
	/// How many times <see cref="SettingsStore{T}.Save"/> tries to write the file before giving up. Default is 10.
	/// </summary>
	public int SaveAttempts { get; init; } = 10;

	/// <summary>
	/// Pause between two attempts to write the file. Default is 200 milliseconds.
	/// </summary>
	public TimeSpan SaveRetryDelay { get; init; } = TimeSpan.FromMilliseconds(200);
}
