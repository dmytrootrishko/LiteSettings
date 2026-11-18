using System.Reflection;
using LiteSettings.Internal;

namespace LiteSettings;

/// <summary>
/// Loads and saves one settings object of type <typeparamref name="T"/> in the application's settings folder.
/// </summary>
/// <remarks>
/// Creating a store touches nothing on disk. An instance can be shared between threads:
/// loading and saving through the same store never overlap.
/// </remarks>
/// <typeparam name="T">
/// Settings class. Its properties marked with <see cref="SaveAttribute"/> are saved.
/// </typeparam>
public sealed class SettingsStore<T> where T : class, new()
{
	private const string TempFolderName = "Temp";

	private readonly ISettingsSerializer _serializer;
	private readonly Lock _sync = new();
	private readonly string _tempDirectoryPath;
	private readonly int _saveAttempts;
	private readonly TimeSpan _saveRetryDelay;

	/// <summary>
	/// Creates a store that reads and writes the settings file with <paramref name="serializer"/>.
	/// </summary>
	/// <param name="serializer">File format.</param>
	/// <param name="options">Optional overrides; <see langword="null"/> means the defaults.</param>
	/// <exception cref="InvalidOperationException">
	/// The folder has to be computed, but the assembly has no company or product attribute.
	/// </exception>
	public SettingsStore(ISettingsSerializer serializer, SettingsStoreOptions? options = null)
	{
		ArgumentNullException.ThrowIfNull(serializer);
		options ??= new SettingsStoreOptions();

		if (options.SaveAttempts < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(options), options.SaveAttempts, "SaveAttempts must be at least 1.");
		}
		if (options.SaveRetryDelay < TimeSpan.Zero)
		{
			throw new ArgumentOutOfRangeException(nameof(options), options.SaveRetryDelay, "SaveRetryDelay must not be negative.");
		}
		if (options.DirectoryPath is not null && string.IsNullOrWhiteSpace(options.DirectoryPath))
		{
			throw new ArgumentException("DirectoryPath must not be empty.", nameof(options));
		}

		string fileName = options.FileName ?? serializer.DefaultFileName;
		if (string.IsNullOrWhiteSpace(fileName))
		{
			throw new ArgumentException("The settings file name must not be empty.", nameof(options));
		}

		Assembly assembly = options.Assembly ?? typeof(T).Assembly;

		_serializer = serializer;
		_saveAttempts = options.SaveAttempts;
		_saveRetryDelay = options.SaveRetryDelay;

		UniquePostfix = SettingsPathResolver.GeneratePostfix(assembly);
		DirectoryPath = options.DirectoryPath is not null
			? Path.GetFullPath(options.DirectoryPath)
			: SettingsPathResolver.GetDirectoryPath(assembly, UniquePostfix);
		FilePath = Path.Combine(DirectoryPath, fileName);
		_tempDirectoryPath = Path.Combine(DirectoryPath, TempFolderName);
	}

	/// <summary>
	/// Folder that holds the settings file. It is created by the first successful <see cref="Save"/>.
	/// </summary>
	public string DirectoryPath { get; }

	/// <summary>
	/// Full path of the settings file.
	/// </summary>
	public string FilePath { get; }

	/// <summary>
	/// Folder for the application's temporary files, located next to the settings file.
	/// Reading this property creates the folder if it does not exist.
	/// </summary>
	/// <remarks>
	/// Nothing is deleted automatically. Call <see cref="ClearTempDirectory"/> when the application starts
	/// if leftovers from a previous run must go away.
	/// </remarks>
	public string TempDirectoryPath
	{
		get
		{
			Directory.CreateDirectory(_tempDirectoryPath);
			return _tempDirectoryPath;
		}
	}

	/// <summary>
	/// Suffix of the settings folder name. It is derived from the location of the application's assembly,
	/// so it differs between two copies of the same application installed in different folders.
	/// </summary>
	public string UniquePostfix { get; }

	/// <summary>
	/// Creates a new <typeparamref name="T"/> and fills it from the settings file.
	/// </summary>
	/// <returns>
	/// The loaded settings. Properties that are not in the file keep the values assigned by the constructor
	/// of <typeparamref name="T"/>; this is also the result when the file is missing or unreadable.
	/// </returns>
	public T Load()
	{
		T settings = new();
		Load(settings);
		return settings;
	}

	/// <summary>
	/// Fills an existing object from the settings file, for example to discard unsaved changes.
	/// </summary>
	/// <remarks>
	/// Properties that are not in the file keep their current values.
	/// Nothing changes when the file is missing or unreadable.
	/// </remarks>
	/// <param name="target">Object to fill.</param>
	public void Load(T target)
	{
		ArgumentNullException.ThrowIfNull(target);

		lock (_sync)
		{
			if (!File.Exists(FilePath))
			{
				return;
			}

			try
			{
				// FileShare.Delete lets another process replace the file while this one is still reading the old content.
				using FileStream stream = new(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
				_serializer.Populate(stream, target);
			}
			catch (InvalidDataException)
			{
				// A damaged file is treated like a missing one; the next Save replaces it.
			}
			catch (FileNotFoundException)
			{
			}
			catch (DirectoryNotFoundException)
			{
			}
		}
	}

	/// <summary>
	/// Writes <paramref name="settings"/> to the settings file, creating the folder when needed.
	/// </summary>
	/// <remarks>
	/// The file is replaced in one step: readers see either the old or the new content, never a part of it.
	/// When the file or folder is not accessible, the write is repeated up to
	/// <see cref="SettingsStoreOptions.SaveAttempts"/> times, and the calling thread waits in between.
	/// </remarks>
	/// <param name="settings">Object to save.</param>
	/// <returns><see langword="true"/> when the file was written; <see langword="false"/> when every attempt failed.</returns>
	public bool Save(T settings)
	{
		ArgumentNullException.ThrowIfNull(settings);

		lock (_sync)
		{
			byte[] content;
			using (MemoryStream buffer = new())
			{
				_serializer.Serialize(buffer, settings);
				content = buffer.ToArray();
			}

			return SettingsFileWriter.TryWrite(FilePath, content, _saveAttempts, _saveRetryDelay);
		}
	}

	/// <summary>
	/// Deletes everything inside <see cref="TempDirectoryPath"/>. Does nothing when the folder does not exist.
	/// </summary>
	public void ClearTempDirectory()
	{
		if (!Directory.Exists(_tempDirectoryPath))
		{
			return;
		}

		foreach (string file in Directory.GetFiles(_tempDirectoryPath))
		{
			File.Delete(file);
		}
		foreach (string directory in Directory.GetDirectories(_tempDirectoryPath))
		{
			Directory.Delete(directory, true);
		}
	}
}
