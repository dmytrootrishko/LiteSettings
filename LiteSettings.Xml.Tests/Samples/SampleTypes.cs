using System.Text.Json.Serialization;
using LegacySave = ProgramSettings.SaveAttribute;
using NewSave = LiteSettings.SaveAttribute;

namespace LiteSettings.Xml.Tests.Samples;

public enum SampleMode
{
	First,
	Second,
	Third,
}

[Flags]
public enum SampleFlags
{
	None = 0,
	Read = 1,
	Write = 2,
	Delete = 4,
}

/// <summary>
/// Nested value type shared by both samples.
/// </summary>
[LegacySave, NewSave]
public struct SamplePoint
{
	[LegacySave, NewSave]
	public int X { get; set; }

	[LegacySave, NewSave]
	public int Y { get; set; }
}

/// <summary>
/// Nested object shared by the ProgramSettings sample and the LiteSettings sample, hence both attributes.
/// </summary>
[LegacySave, NewSave]
public class SampleProfile
{
	[LegacySave, NewSave]
	public string? Name { get; set; }

	[LegacySave, NewSave]
	public int Age { get; set; }

	[LegacySave, NewSave]
	public double Weight { get; set; }

	[LegacySave, NewSave]
	public List<string>? Tags { get; set; }

	[LegacySave, NewSave]
	public SampleProfile? Inner { get; set; }

	[JsonIgnore]
	public string? NotSaved { get; set; }
}
