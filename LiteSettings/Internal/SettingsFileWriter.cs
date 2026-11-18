namespace LiteSettings.Internal;

/// <summary>
/// Replaces the settings file in one step, so that a crash in the middle of saving never leaves a half-written file.
/// </summary>
internal static class SettingsFileWriter
{
	private const string TempFileSuffix = ".tmp";

	/// <summary>
	/// Writes <paramref name="content"/> next to the target and then moves it over the target.
	/// </summary>
	/// <returns><see langword="false"/> when every attempt failed because the file or folder was not accessible.</returns>
	public static bool TryWrite(string path, byte[] content, int attempts, TimeSpan retryDelay)
	{
		string tempPath = path + TempFileSuffix;

		for (int i = 0; i < attempts; ++i)
		{
			try
			{
				string? directory = Path.GetDirectoryName(path);
				if (!string.IsNullOrEmpty(directory))
				{
					Directory.CreateDirectory(directory);
				}

				using (FileStream stream = new(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					stream.Write(content);
					stream.Flush(true);
				}

				File.Move(tempPath, path, true);
				return true;
			}
			// Windows reports a target that is open in another process as "access denied" when a file
			// is moved over it, so both exception types can mean "busy, try again".
			catch (Exception e) when (e is IOException or UnauthorizedAccessException)
			{
				if (i != attempts - 1)
				{
					Thread.Sleep(retryDelay);
				}
			}
		}

		TryDelete(tempPath);
		return false;
	}

	private static void TryDelete(string path)
	{
		try
		{
			File.Delete(path);
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException)
		{
		}
	}
}
