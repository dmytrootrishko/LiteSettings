using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using LiteSettings.Internal;
using LiteSettings.Tests.TestSupport;

namespace LiteSettings.Tests;

/// <summary>
/// The settings folder must stay where ProgramSettings put it:
/// <c>{LocalApplicationData}\{Company}\{Product}_{postfix}</c>.
/// </summary>
public sealed class SettingsPathTests
{
	private readonly FakeSerializer _serializer = new();

	[Theory]
	// Values produced by the original ProgramSettings code.
	[InlineData("", "47DEQpj8HBSaTImW5JCeuQeRkm5NMpJW")]
	[InlineData(@"C:\Program Files\MyApp\MyApp.dll", "hh8xWASpNej8ueGlyYfdDPzHKnPxCztd")]
	[InlineData("/opt/my-app/MyApp.dll", "UZYL8fksBJgTpFal5B7UZZd3ARHQEjMo")]
	public void GeneratePostfix_MatchesOriginalImplementation(string location, string expected)
	{
		Assert.Equal(expected, SettingsPathResolver.GeneratePostfix(location));
	}

	[Fact]
	public void UniquePostfix_IsDerivedFromAssemblyLocation()
	{
		SettingsStore<FakeSettings> store = new(_serializer);

		Assert.Equal(ExpectedPostfix(typeof(FakeSettings).Assembly.Location), store.UniquePostfix);
	}

	[Fact]
	public void DefaultFolder_IsBuiltFromTheAssemblyOfTheSettingsClass()
	{
		SettingsStore<FakeSettings> store = new(_serializer);

		string expected = ExpectedFolder(typeof(FakeSettings).Assembly);
		Assert.Equal(expected, store.DirectoryPath);
		Assert.Equal(Path.Combine(expected, "settings.fake"), store.FilePath);
	}

	[Fact]
	public void AssemblyOption_ReplacesTheAssemblyOfTheSettingsClass()
	{
		Assembly other = typeof(SettingsStore<>).Assembly;
		SettingsStore<FakeSettings> store = new(_serializer, new SettingsStoreOptions { Assembly = other });

		Assert.Equal(ExpectedPostfix(other.Location), store.UniquePostfix);
		Assert.Equal(ExpectedFolder(other), store.DirectoryPath);
		Assert.NotEqual(ExpectedFolder(typeof(FakeSettings).Assembly), store.DirectoryPath);
	}

	[Fact]
	public void DirectoryPathOption_ReplacesTheComputedFolder()
	{
		using TempFolder folder = new();
		SettingsStore<FakeSettings> store = new(_serializer, new SettingsStoreOptions { DirectoryPath = folder.Path });

		Assert.Equal(folder.Path, store.DirectoryPath);
		Assert.Equal(ExpectedPostfix(typeof(FakeSettings).Assembly.Location), store.UniquePostfix);
	}

	// Written independently of the library code on purpose.
	private static string ExpectedPostfix(string location)
	{
		string base64 = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(location)));
		return new string(base64.Where(char.IsLetterOrDigit).Take(32).ToArray());
	}

	private static string ExpectedFolder(Assembly assembly)
	{
		string appData = Environment.GetFolderPath(
			Environment.SpecialFolder.LocalApplicationData,
			Environment.SpecialFolderOption.DoNotVerify);
		string company = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()!.Company;
		string product = assembly.GetCustomAttribute<AssemblyProductAttribute>()!.Product;
		return Path.Combine(appData, company, product + "_" + ExpectedPostfix(assembly.Location));
	}
}
