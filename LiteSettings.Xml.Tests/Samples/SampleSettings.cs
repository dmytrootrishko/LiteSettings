using System.Text.Json.Serialization;

namespace LiteSettings.Xml.Tests.Samples;

/// <summary>
/// Settings class the LiteSettings way. Property names, types and order mirror <see cref="LegacyReference.LegacySampleSettings"/>.
/// </summary>
public class SampleSettings
{
	[Save]
	public string? Text { get; set; }

	[Save]
	public string? EmptyText { get; set; }

	[Save]
	public string? NullText { get; set; }

	[Save]
	public int Count { get; set; }

	[Save]
	public long Big { get; set; }

	[Save]
	public double Ratio { get; set; }

	[Save]
	public decimal Price { get; set; }

	[Save]
	public bool Enabled { get; set; }

	[Save]
	public char Letter { get; set; }

	[Save]
	public DateTime Timestamp { get; set; }

	[Save]
	public SampleMode Mode { get; set; }

	[Save]
	public List<string>? Names { get; set; }

	[Save]
	public List<int>? Numbers { get; set; }

	[Save]
	public Dictionary<string, int>? Scores { get; set; }

	[Save]
	public Dictionary<int, string>? Labels { get; set; }

	[Save]
	public KeyValuePair<string, int> Pair { get; set; }

	[Save]
	public SampleProfile? Profile { get; set; }

	[Save]
	public List<SampleProfile>? Profiles { get; set; }

	[Save]
	public float Factor { get; set; }

	[Save]
	public byte Level { get; set; }

	[Save]
	public SampleFlags Flags { get; set; }

	[Save]
	public SamplePoint Point { get; set; }

	[Save]
	public List<List<int>>? Matrix { get; set; }

	[Save]
	public Dictionary<string, List<string>>? Groups { get; set; }

	[Save]
	public Dictionary<string, SampleProfile>? ProfilesByName { get; set; }

	[Save]
	public List<KeyValuePair<string, int>>? Pairs { get; set; }

	[JsonIgnore]
	public string? NotSaved { get; set; }
}
