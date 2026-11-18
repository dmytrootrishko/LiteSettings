using System.Reflection;
using LegacyReference;

namespace LiteSettings.Xml.Tests.TestSupport;

/// <summary>
/// Runs the original ProgramSettings code against the real settings folder of the test assembly.
/// </summary>
internal static class LegacyEnvironment
{
	private static readonly string _localApplicationData = Environment.GetFolderPath(
		Environment.SpecialFolder.LocalApplicationData,
		Environment.SpecialFolderOption.DoNotVerify);

	public static void Initialize()
	{
		// On a fresh Unix account this folder may not exist yet, and the original code
		// then falls back to a path relative to the working directory.
		Directory.CreateDirectory(_localApplicationData);

		LegacySampleSettings.Initialize();
	}

	public static void DeleteSettingsFolder()
	{
		string directory = LegacySampleSettings.DirectoryPath;
		if (Directory.Exists(directory))
		{
			Directory.Delete(directory, true);
		}

		// Remove the company folder too, but only if it is really ours and empty.
		string company = typeof(LegacyEnvironment).Assembly.GetCustomAttribute<AssemblyCompanyAttribute>()!.Company;
		string companyDirectory = Path.Combine(_localApplicationData, company);
		if (company.Length > 0
			&& Path.GetDirectoryName(directory) == companyDirectory
			&& Directory.Exists(companyDirectory)
			&& !Directory.EnumerateFileSystemEntries(companyDirectory).Any())
		{
			Directory.Delete(companyDirectory);
		}
	}
}
