using System.Text;

namespace LiteSettings.Tests.TestSupport;

internal sealed class FakeSettings
{
	[Save]
	public string Text { get; set; } = "default";
}

/// <summary>
/// Stores <see cref="FakeSettings.Text"/> as the whole file content.
/// The content <see cref="InvalidContent"/> plays the role of a damaged file.
/// </summary>
internal sealed class FakeSerializer : ISettingsSerializer
{
	public const string InvalidContent = "<<damaged>>";

	public string DefaultFileName => "settings.fake";

	public int SerializeCalls { get; private set; }

	public int PopulateCalls { get; private set; }

	public void Serialize(Stream stream, object settings)
	{
		++SerializeCalls;
		stream.Write(Encoding.UTF8.GetBytes(((FakeSettings)settings).Text));
	}

	public void Populate(Stream stream, object target)
	{
		++PopulateCalls;
		using StreamReader reader = new(stream, Encoding.UTF8);
		string content = reader.ReadToEnd();
		if (content == InvalidContent)
		{
			throw new InvalidDataException("Damaged file.");
		}
		((FakeSettings)target).Text = content;
	}
}
