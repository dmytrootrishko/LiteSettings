using System.Text.Json;

namespace LiteSettings.Xml.Tests.Samples;

internal static class SampleData
{
	private static readonly JsonSerializerOptions _snapshotOptions = new() { WriteIndented = true };

	/// <summary>
	/// One value of every supported kind, including the characters that need escaping.
	/// </summary>
	public static SampleSettings Create()
	{
		return new SampleSettings
		{
			Text = "a & b <tag attr=\"1\"> 'single' back\\slash &amp; &sl; Привіт, світе!",
			EmptyText = "",
			NullText = null,
			Count = -42,
			Big = 9_000_000_000,
			Ratio = 1234.5678,
			Price = 1234567.89m,
			Enabled = true,
			Letter = 'ж',
			Timestamp = new DateTime(2026, 11, 14, 20, 59, 7),
			Mode = SampleMode.Second,
			Names = ["first", "", "with <angle> & ampersand"],
			Numbers = [1, -2, 3],
			Scores = new Dictionary<string, int> { ["alice"] = 10, ["bob & co"] = -5 },
			Labels = new Dictionary<int, string> { [1] = "one", [2] = "two\\three" },
			Pair = new KeyValuePair<string, int>("answer", 42),
			Profile = new SampleProfile
			{
				Name = "root",
				Age = 30,
				Weight = 70.25,
				Tags = ["x", "y"],
				Inner = new SampleProfile { Name = "inner", Age = 1, Weight = 0.5 },
				NotSaved = "never written",
			},
			Profiles =
			[
				new SampleProfile { Name = "p1", Age = 1, Weight = 1.5, Tags = [] },
				new SampleProfile { Name = "p2", Age = 2, Weight = -2.75 },
			],
			Factor = 0.1f,
			Level = 200,
			Flags = SampleFlags.Read | SampleFlags.Delete,
			Point = new SamplePoint { X = 3, Y = -4 },
			Matrix = [[1, 2], [], [3]],
			Groups = new Dictionary<string, List<string>> { ["letters"] = ["a", "b"], ["empty"] = [] },
			ProfilesByName = new Dictionary<string, SampleProfile>
			{
				["first"] = new SampleProfile { Name = "by name", Age = 5, Weight = 5.5 },
			},
			Pairs = [new KeyValuePair<string, int>("one", 1), new KeyValuePair<string, int>("two", 2)],
			NotSaved = "never written",
		};
	}

	/// <summary>
	/// Culture-independent text form of everything that is saved, for comparing two settings objects.
	/// </summary>
	public static string Snapshot(SampleSettings settings)
	{
		return JsonSerializer.Serialize(settings, _snapshotOptions);
	}
}
