using LiteSettings.Xml.Tests.Samples;
using LiteSettings.Xml.Tests.TestSupport;

namespace LiteSettings.Xml.Tests;

/// <summary>
/// Reads settings files that were produced by the original ProgramSettings code and are kept in the repository,
/// so the format stays pinned even without the old sources at hand.
/// </summary>
public sealed class FixtureTests
{
	[Theory]
	[InlineData("en-US", "settings.en-US.xml")]
	[InlineData("uk-UA", "settings.uk-UA.xml")]
	public void Populate_ReadsFileWrittenByProgramSettings(string culture, string fileName)
	{
		using CultureScope scope = new(culture);
		SampleSettings expected = SampleData.Create();
		SampleSettings loaded = new();

		using (FileStream stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName)))
		{
			new XmlSettingsSerializer().Populate(stream, loaded);
		}

		// Date formats depend on the culture data installed on the machine, which changes over time,
		// so the timestamp is not part of the comparison.
		loaded.Timestamp = expected.Timestamp;
		Assert.Equal(SampleData.Snapshot(expected), SampleData.Snapshot(loaded));
	}
}
