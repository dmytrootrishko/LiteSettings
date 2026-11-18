using System.Text;
using LegacyReference;
using LiteSettings.Xml.Tests.Samples;
using LiteSettings.Xml.Tests.TestSupport;

namespace LiteSettings.Xml.Tests;

/// <summary>
/// Runs the original ProgramSettings code and LiteSettings side by side: an application that switches
/// libraries must find its settings in the same folder, in a file both versions read and write identically.
/// </summary>
/// <remarks>
/// The tests use the real settings folder of the test assembly (under the local application data folder)
/// and remove it afterwards. ProgramSettings keeps its state in static fields, so everything that touches it
/// lives in this one class and therefore runs sequentially.
/// </remarks>
public sealed class LegacyCompatibilityTests : IDisposable
{
	private readonly SettingsStore<SampleSettings> _store;

	public LegacyCompatibilityTests()
	{
		LegacyEnvironment.Initialize();
		_store = XmlSettingsStore.Create<SampleSettings>();
	}

	public void Dispose()
	{
		LegacyEnvironment.DeleteSettingsFolder();
	}

	[Fact]
	public void Folder_IsTheOneProgramSettingsUsed()
	{
		Assert.Equal(LegacySampleSettings.DirectoryPath, _store.DirectoryPath);
		Assert.Equal(Path.Combine(LegacySampleSettings.DirectoryPath, "settings.xml"), _store.FilePath);
		Assert.Equal(LegacySampleSettings.TempDirectoryPath, _store.TempDirectoryPath);
		Assert.Equal(LegacySampleSettings.UniquePostfix, _store.UniquePostfix);
	}

	[Theory]
	[InlineData("")]
	[InlineData("en-US")]
	[InlineData("uk-UA")]
	[InlineData("ru-RU")]
	[InlineData("de-DE")]
	[InlineData("fr-FR")]
	public void Save_WritesTheSameBytesAsProgramSettings(string culture)
	{
		using CultureScope scope = new(culture);
		SampleSettings sample = SampleData.Create();

		LegacySampleSettings.Assign(sample);
		Assert.True(LegacySampleSettings.SaveToFile());
		byte[] legacyBytes = File.ReadAllBytes(_store.FilePath);
		File.Delete(_store.FilePath);

		Assert.True(_store.Save(sample));
		byte[] newBytes = File.ReadAllBytes(_store.FilePath);

		// Compared as text first only to get a readable message when they differ.
		Assert.Equal(Encoding.UTF8.GetString(legacyBytes), Encoding.UTF8.GetString(newBytes));
		Assert.Equal(legacyBytes, newBytes);
	}

	[Theory]
	[InlineData("")]
	[InlineData("en-US")]
	[InlineData("uk-UA")]
	[InlineData("ru-RU")]
	[InlineData("de-DE")]
	[InlineData("fr-FR")]
	public void Serialize_WritesTheSameBytesAsProgramSettings_ForLessCommonTypes(string culture)
	{
		using CultureScope scope = new(culture);
		ExoticSettings sample = ExoticSettings.Create();

		Assert.True(LegacySampleSettings.SaveObject(sample, "exotic.xml"));
		byte[] legacyBytes = File.ReadAllBytes(Path.Combine(LegacySampleSettings.DirectoryPath, "exotic.xml"));

		using MemoryStream stream = new();
		new XmlSettingsSerializer().Serialize(stream, sample);
		byte[] newBytes = stream.ToArray();

		Assert.Equal(Encoding.UTF8.GetString(legacyBytes), Encoding.UTF8.GetString(newBytes));
		Assert.Equal(legacyBytes, newBytes);
	}

	[Theory]
	[InlineData("")]
	[InlineData("en-US")]
	[InlineData("uk-UA")]
	[InlineData("ru-RU")]
	[InlineData("de-DE")]
	[InlineData("fr-FR")]
	public void Load_ReadsFileWrittenByProgramSettings(string culture)
	{
		using CultureScope scope = new(culture);
		SampleSettings sample = SampleData.Create();
		LegacySampleSettings.Assign(sample);
		Assert.True(LegacySampleSettings.SaveToFile());

		SampleSettings loaded = _store.Load();

		// The same values ProgramSettings itself reads from that file...
		LegacySampleSettings.Reset();
		LegacySampleSettings.ReloadFromFile();
		Assert.Equal(SampleData.Snapshot(LegacySampleSettings.ToSample()), SampleData.Snapshot(loaded));
		// ...which are the values that were saved.
		Assert.Equal(SampleData.Snapshot(sample), SampleData.Snapshot(loaded));
		Assert.Null(loaded.NotSaved);
	}

	[Theory]
	[InlineData("")]
	[InlineData("en-US")]
	[InlineData("uk-UA")]
	[InlineData("ru-RU")]
	[InlineData("de-DE")]
	[InlineData("fr-FR")]
	public void ProgramSettings_ReadsFileWrittenByLiteSettings(string culture)
	{
		using CultureScope scope = new(culture);
		SampleSettings sample = SampleData.Create();
		Assert.True(_store.Save(sample));

		LegacySampleSettings.Reset();
		LegacySampleSettings.ReloadFromFile();

		Assert.Equal(SampleData.Snapshot(sample), SampleData.Snapshot(LegacySampleSettings.ToSample()));
	}

	[Fact]
	public void Save_ReplacesFileWrittenByProgramSettings()
	{
		LegacySampleSettings.Assign(SampleData.Create());
		Assert.True(LegacySampleSettings.SaveToFile());

		SampleSettings settings = _store.Load();
		settings.Count = 1000;
		settings.Names = ["changed"];
		Assert.True(_store.Save(settings));

		SampleSettings reloaded = _store.Load();
		Assert.Equal(1000, reloaded.Count);
		Assert.Equal(new[] { "changed" }, reloaded.Names);
		Assert.Equal(SampleData.Create().Text, reloaded.Text);
	}
}
