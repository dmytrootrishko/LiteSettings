using System.Collections;
using System.Collections.ObjectModel;
using LegacySave = ProgramSettings.SaveAttribute;
using NewSave = LiteSettings.SaveAttribute;

namespace LiteSettings.Xml.Tests.Samples;

/// <summary>
/// Less common property types. ProgramSettings could write all of them (reading some back never worked),
/// so they only take part in the comparison of written files. Both attributes are applied
/// to let the old and the new code save the very same object.
/// </summary>
public class ExoticSettings
{
	[LegacySave, NewSave]
	public int[]? Ints { get; set; }

	[LegacySave, NewSave]
	public string[]? Strings { get; set; }

	[LegacySave, NewSave]
	public byte[]? Bytes { get; set; }

	[LegacySave, NewSave]
	public ArrayList? Mixed { get; set; }

	[LegacySave, NewSave]
	public Hashtable? Table { get; set; }

	[LegacySave, NewSave]
	public List<string?>? WithNulls { get; set; }

	[LegacySave, NewSave]
	public ObservableCollection<int>? Observable { get; set; }

	[LegacySave, NewSave]
	public Dictionary<string, Dictionary<string, int>>? NestedMap { get; set; }

	[LegacySave, NewSave]
	public List<SampleMode>? Modes { get; set; }

	[LegacySave, NewSave]
	public List<SamplePoint>? Points { get; set; }

	[LegacySave, NewSave]
	public KeyValuePair<int, SampleProfile> ProfilePair { get; set; }

	[LegacySave, NewSave]
	public DateTime Utc { get; set; }

	[LegacySave, NewSave]
	public DateTimeOffset Moment { get; set; }

	[LegacySave, NewSave]
	public TimeSpan Duration { get; set; }

	[LegacySave, NewSave]
	public Guid Id { get; set; }

	[LegacySave, NewSave]
	public Uri? Link { get; set; }

	[LegacySave, NewSave]
	public Version? Release { get; set; }

	[LegacySave, NewSave]
	public int? Maybe { get; set; }

	[LegacySave, NewSave]
	public int? Nothing { get; set; }

	[LegacySave, NewSave]
	public object? Boxed { get; set; }

	[LegacySave, NewSave]
	public sbyte Tiny { get; set; }

	[LegacySave, NewSave]
	public short Small { get; set; }

	[LegacySave, NewSave]
	public ushort UnsignedSmall { get; set; }

	[LegacySave, NewSave]
	public uint Unsigned { get; set; }

	[LegacySave, NewSave]
	public ulong UnsignedBig { get; set; }

	[LegacySave, NewSave]
	public double Huge { get; set; }

	[LegacySave, NewSave]
	public double NotANumber { get; set; }

	[LegacySave, NewSave]
	public double Infinite { get; set; }

	[LegacySave, NewSave]
	public decimal Maximum { get; set; }

	[LegacySave, NewSave]
	public string? Whitespace { get; set; }

	[LegacySave, NewSave]
	public string? Emoji { get; set; }

	public static ExoticSettings Create()
	{
		return new ExoticSettings
		{
			Ints = [1, 2, 3],
			Strings = ["a", "", "c & d"],
			Bytes = [0, 127, 255],
			Mixed = new ArrayList { 1, "two", 3.5, SampleMode.Third, true },
			Table = new Hashtable { ["key"] = 1.5 },
			WithNulls = ["x", null, "z"],
			Observable = [10, 20],
			NestedMap = new Dictionary<string, Dictionary<string, int>>
			{
				["outer"] = new Dictionary<string, int> { ["inner"] = 1 },
				["empty"] = [],
			},
			Modes = [SampleMode.First, SampleMode.Third],
			Points = [new SamplePoint { X = 1, Y = 2 }, new SamplePoint()],
			ProfilePair = new KeyValuePair<int, SampleProfile>(7, new SampleProfile { Name = "in pair", Weight = 0.25 }),
			Utc = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
			Moment = new DateTimeOffset(2026, 11, 14, 20, 59, 7, TimeSpan.FromHours(2)),
			Duration = new TimeSpan(1, 2, 3, 4, 500),
			Id = Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e"),
			Link = new Uri("https://example.com/path?a=1&b=2"),
			Release = new Version(1, 2, 3, 4),
			Maybe = 5,
			Nothing = null,
			Boxed = 42.5,
			Tiny = -100,
			Small = -30000,
			UnsignedSmall = 60000,
			Unsigned = 4_000_000_000,
			UnsignedBig = 18_000_000_000_000_000_000,
			Huge = 1.5e300,
			NotANumber = double.NaN,
			Infinite = double.NegativeInfinity,
			Maximum = decimal.MaxValue,
			Whitespace = " \ttab and\nnew line ",
			Emoji = "smile 😀",
		};
	}
}
