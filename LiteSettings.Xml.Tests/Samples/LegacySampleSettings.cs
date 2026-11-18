using System.Reflection;
using LiteSettings.Xml.Tests.Samples;
using ProgramSettings;

// Deliberately outside the LiteSettings namespaces: inside them [Save] would bind to LiteSettings.SaveAttribute.
namespace LegacyReference;

/// <summary>
/// Settings class the ProgramSettings way: static properties on a class derived from <see cref="BaseProgramSettings"/>.
/// </summary>
public class LegacySampleSettings : BaseProgramSettings
{
	[Save]
	public static string? Text { get; set; }

	[Save]
	public static string? EmptyText { get; set; }

	[Save]
	public static string? NullText { get; set; }

	[Save]
	public static int Count { get; set; }

	[Save]
	public static long Big { get; set; }

	[Save]
	public static double Ratio { get; set; }

	[Save]
	public static decimal Price { get; set; }

	[Save]
	public static bool Enabled { get; set; }

	[Save]
	public static char Letter { get; set; }

	[Save]
	public static DateTime Timestamp { get; set; }

	[Save]
	public static SampleMode Mode { get; set; }

	[Save]
	public static List<string>? Names { get; set; }

	[Save]
	public static List<int>? Numbers { get; set; }

	[Save]
	public static Dictionary<string, int>? Scores { get; set; }

	[Save]
	public static Dictionary<int, string>? Labels { get; set; }

	[Save]
	public static KeyValuePair<string, int> Pair { get; set; }

	[Save]
	public static SampleProfile? Profile { get; set; }

	[Save]
	public static List<SampleProfile>? Profiles { get; set; }

	[Save]
	public static float Factor { get; set; }

	[Save]
	public static byte Level { get; set; }

	[Save]
	public static SampleFlags Flags { get; set; }

	[Save]
	public static SamplePoint Point { get; set; }

	[Save]
	public static List<List<int>>? Matrix { get; set; }

	[Save]
	public static Dictionary<string, List<string>>? Groups { get; set; }

	[Save]
	public static Dictionary<string, SampleProfile>? ProfilesByName { get; set; }

	[Save]
	public static List<KeyValuePair<string, int>>? Pairs { get; set; }

	public static string? NotSaved { get; set; }

	public static string DirectoryPath => GetDirectoryPath()!;

	public static string TempDirectoryPath => GetTempDirectoryPath()!;

	public static string UniquePostfix => GetUniquePostfix()!;

	public static void Initialize()
	{
		Init(typeof(LegacySampleSettings));
	}

	public static bool SaveToFile()
	{
		return InnerSave();
	}

	public static void ReloadFromFile()
	{
		InnerReload();
	}

	/// <summary>
	/// Saves any object the way ProgramSettings saved additional files: its instance properties
	/// marked with the original attribute go to <paramref name="fileName"/> in the settings folder.
	/// </summary>
	public static bool SaveObject(object source, string fileName)
	{
		List<PropertyInfo> properties = source
			.GetType()
			.GetProperties()
			.Where(p => p.GetCustomAttributes(typeof(SaveAttribute), true).FirstOrDefault() != null)
			.ToList();

		return InnerSave("Settings", properties, source, fileName);
	}

	public static void Assign(SampleSettings source)
	{
		Text = source.Text;
		EmptyText = source.EmptyText;
		NullText = source.NullText;
		Count = source.Count;
		Big = source.Big;
		Ratio = source.Ratio;
		Price = source.Price;
		Enabled = source.Enabled;
		Letter = source.Letter;
		Timestamp = source.Timestamp;
		Mode = source.Mode;
		Names = source.Names;
		Numbers = source.Numbers;
		Scores = source.Scores;
		Labels = source.Labels;
		Pair = source.Pair;
		Profile = source.Profile;
		Profiles = source.Profiles;
		Factor = source.Factor;
		Level = source.Level;
		Flags = source.Flags;
		Point = source.Point;
		Matrix = source.Matrix;
		Groups = source.Groups;
		ProfilesByName = source.ProfilesByName;
		Pairs = source.Pairs;
		NotSaved = source.NotSaved;
	}

	public static SampleSettings ToSample()
	{
		return new SampleSettings
		{
			Text = Text,
			EmptyText = EmptyText,
			NullText = NullText,
			Count = Count,
			Big = Big,
			Ratio = Ratio,
			Price = Price,
			Enabled = Enabled,
			Letter = Letter,
			Timestamp = Timestamp,
			Mode = Mode,
			Names = Names,
			Numbers = Numbers,
			Scores = Scores,
			Labels = Labels,
			Pair = Pair,
			Profile = Profile,
			Profiles = Profiles,
			Factor = Factor,
			Level = Level,
			Flags = Flags,
			Point = Point,
			Matrix = Matrix,
			Groups = Groups,
			ProfilesByName = ProfilesByName,
			Pairs = Pairs,
			NotSaved = NotSaved,
		};
	}

	/// <summary>
	/// Puts every property back to its default, so that a following reload proves the values came from the file.
	/// </summary>
	public static void Reset()
	{
		Assign(new SampleSettings());
	}
}
