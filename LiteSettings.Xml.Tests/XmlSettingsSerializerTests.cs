using System.Text;
using LiteSettings.Xml.Tests.Samples;
using LiteSettings.Xml.Tests.TestSupport;

namespace LiteSettings.Xml.Tests;

public sealed class XmlSettingsSerializerTests : IDisposable
{
	// Expected texts below are written for the invariant culture.
	private readonly CultureScope _culture = new("");

	public void Dispose()
	{
		_culture.Dispose();
	}

	private static byte[] WriteBytes(object settings, XmlSettingsSerializer? serializer = null)
	{
		using MemoryStream stream = new();
		(serializer ?? new XmlSettingsSerializer()).Serialize(stream, settings);
		return stream.ToArray();
	}

	private static string Write(object settings, XmlSettingsSerializer? serializer = null)
	{
		return Encoding.UTF8.GetString(WriteBytes(settings, serializer));
	}

	private static void ReadInto(object target, string xml)
	{
		using MemoryStream stream = new(Encoding.UTF8.GetBytes(xml));
		new XmlSettingsSerializer().Populate(stream, target);
	}

	private static T Read<T>(string xml) where T : class, new()
	{
		T target = new();
		ReadInto(target, xml);
		return target;
	}

	private static T RoundTrip<T>(T settings) where T : class, new()
	{
		T target = new();
		using MemoryStream stream = new(WriteBytes(settings));
		new XmlSettingsSerializer().Populate(stream, target);
		return target;
	}

	#region Document format

	private sealed class Document
	{
		[Save]
		public string? Text { get; set; }

		[Save]
		public int Count { get; set; }

		[Save]
		public double Ratio { get; set; }

		[Save]
		public string? Missing { get; set; }

		[Save]
		public List<string>? Names { get; set; }

		[Save]
		public Dictionary<string, int>? Scores { get; set; }

		[Save]
		public Inner? Nested { get; set; }

		public string? NotMarked { get; set; }
	}

	[Save]
	private sealed class Inner
	{
		[Save]
		public string? Name { get; set; }

		public int NotMarked { get; set; }
	}

	[Fact]
	public void Serialize_WritesTheProgramSettingsDocument()
	{
		Document document = new()
		{
			Text = "a & b <c> \"d\" 'e' f\\g",
			Count = 5,
			Ratio = 1.5,
			Missing = null,
			Names = ["x", "y"],
			Scores = new Dictionary<string, int> { ["k"] = 1 },
			Nested = new Inner { Name = "n", NotMarked = 3 },
			NotMarked = "ignored",
		};

		string xml = Write(document);

		string expected = "\uFEFF" + string.Join(
			Environment.NewLine,
			"<?xml version=\"1.0\" encoding=\"utf-8\"?>",
			"<Settings Version=\"1.0.0.0\">",
			"  <Text>a &amp;amp; b &amp;lt;c&amp;gt; &amp;quot;d&amp;quot; &amp;apos;e&amp;apos; f&amp;sl;g</Text>",
			"  <Count>5</Count>",
			"  <Ratio>1.5</Ratio>",
			"  <Names>",
			"    <Item>x</Item>",
			"    <Item>y</Item>",
			"  </Names>",
			"  <Scores>",
			"    <Item>",
			"      <Key>k</Key>",
			"      <Value>1</Value>",
			"    </Item>",
			"  </Scores>",
			"  <Nested>",
			"    <Name>n</Name>",
			"  </Nested>",
			"</Settings>");
		Assert.Equal(expected, xml);
	}

	[Fact]
	public void Serialize_StartsWithUtf8ByteOrderMark()
	{
		byte[] bytes = WriteBytes(new Document());

		Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3).ToArray());
	}

	[Fact]
	public void Serialize_WritesNumbersInTheCurrentCulture()
	{
		using CultureScope scope = new("uk-UA");

		string xml = Write(new Document { Ratio = 1.5 });

		Assert.Contains("<Ratio>1,5</Ratio>", xml);
	}

	[Fact]
	public void DefaultFileName_IsSettingsXml()
	{
		Assert.Equal("settings.xml", new XmlSettingsSerializer().DefaultFileName);
	}

	[Fact]
	public void RootElementName_CanBeChanged()
	{
		XmlSettingsSerializer serializer = new("Profile");

		string xml = Write(new Document { Count = 1 }, serializer);

		Assert.Equal("Profile", serializer.RootElementName);
		Assert.Contains("<Profile Version=\"1.0.0.0\">", xml);
	}

	[Theory]
	[InlineData(" ")]
	[InlineData("two words")]
	[InlineData("1starts-with-digit")]
	public void Constructor_RejectsInvalidRootElementName(string name)
	{
		Assert.Throws<ArgumentException>(() => new XmlSettingsSerializer(name));
	}

	[Fact]
	public void Populate_AcceptsAnyRootElement()
	{
		Document document = Read<Document>("<Whatever><Count>7</Count></Whatever>");

		Assert.Equal(7, document.Count);
	}

	[Theory]
	[InlineData("&")]
	[InlineData("&amp;")]
	[InlineData("&amp;amp;")]
	[InlineData("&sl;")]
	[InlineData("\\")]
	[InlineData("C:\\Users\\name\\file.txt")]
	[InlineData("<a href=\"x\">'q'</a>")]
	[InlineData("line 1\nline 2")]
	[InlineData("  spaces around  ")]
	[InlineData("")]
	public void Text_SurvivesRoundTrip(string text)
	{
		Document loaded = RoundTrip(new Document { Text = text });

		Assert.Equal(text, loaded.Text);
	}

	#endregion

	#region What is saved

	private sealed class Selection
	{
		[Save]
		public static string? Shared { get; set; }

		[Save]
		public string Computed => "computed";

		[Save]
		public string? Marked { get; set; }

		[Save]
		public string? PrivateSetter { get; private set; }

		public string? NotMarked { get; set; }
	}

	[Fact]
	public void Serialize_WritesOnlyMarkedReadWriteInstanceProperties()
	{
		Selection.Shared = "shared";

		string xml = Write(new Selection { Marked = "marked", NotMarked = "not marked" });

		Assert.Contains("<Marked>marked</Marked>", xml);
		Assert.DoesNotContain("Shared", xml);
		Assert.DoesNotContain("Computed", xml);
		Assert.DoesNotContain("NotMarked", xml);
	}

	[Fact]
	public void Populate_AssignsOnlyMarkedReadWriteInstanceProperties()
	{
		Selection.Shared = "shared";

		Selection loaded = Read<Selection>(
			"<Settings><Shared>x</Shared><Computed>x</Computed><Marked>x</Marked><PrivateSetter>x</PrivateSetter><NotMarked>x</NotMarked></Settings>");

		Assert.Equal("x", loaded.Marked);
		Assert.Equal("x", loaded.PrivateSetter);
		Assert.Null(loaded.NotMarked);
		Assert.Equal("shared", Selection.Shared);
	}

	[Fact]
	public void Populate_KeepsValuesOfPropertiesMissingInTheFile()
	{
		Document document = new() { Text = "keep", Count = 9 };

		ReadInto(document, "<Settings><Count>1</Count></Settings>");

		Assert.Equal("keep", document.Text);
		Assert.Equal(1, document.Count);
	}

	#endregion

	#region Fixed defects of ProgramSettings

	private sealed class WithNestedName
	{
		[Save]
		public string? Name { get; set; }

		[Save]
		public Inner? Nested { get; set; }
	}

	[Fact]
	public void Populate_NestedElementDoesNotOverwriteTopLevelPropertyWithTheSameName()
	{
		WithNestedName loaded = Read<WithNestedName>(
			"<Settings><Name>top</Name><Nested><Name>nested</Name></Nested></Settings>");

		Assert.Equal("top", loaded.Name);
		Assert.Equal("nested", loaded.Nested?.Name);
	}

	private sealed class ReservedNames
	{
		[Save]
		public string? Item { get; set; }

		[Save]
		public string? Key { get; set; }

		[Save]
		public string? Value { get; set; }

		[Save]
		public List<string>? Names { get; set; }

		[Save]
		public Dictionary<string, string>? Map { get; set; }
	}

	[Fact]
	public void PropertiesMayBeNamedItemKeyAndValue()
	{
		ReservedNames loaded = RoundTrip(new ReservedNames
		{
			Item = "item",
			Key = "key",
			Value = "value",
			Names = ["list item"],
			Map = new Dictionary<string, string> { ["map key"] = "map value" },
		});

		Assert.Equal("item", loaded.Item);
		Assert.Equal("key", loaded.Key);
		Assert.Equal("value", loaded.Value);
		Assert.Equal(new[] { "list item" }, loaded.Names);
		Assert.Equal("map value", loaded.Map?["map key"]);
	}

	private sealed class Tolerant
	{
		[Save]
		public SampleMode Mode { get; set; } = SampleMode.Third;

		[Save]
		public int Count { get; set; } = 5;

		[Save]
		public List<int>? Numbers { get; set; }

		[Save]
		public string? Text { get; set; }
	}

	[Fact]
	public void Populate_UnknownEnumValue_KeepsTheDefaultAndLoadsTheRest()
	{
		Tolerant loaded = Read<Tolerant>("<Settings><Mode>Removed</Mode><Text>ok</Text></Settings>");

		Assert.Equal(SampleMode.Third, loaded.Mode);
		Assert.Equal("ok", loaded.Text);
	}

	[Fact]
	public void Populate_UnreadableValue_IsSkipped()
	{
		Tolerant loaded = Read<Tolerant>(
			"<Settings><Count>many</Count><Numbers><Item>1</Item><Item>x</Item><Item>3</Item></Numbers><Text>ok</Text></Settings>");

		Assert.Equal(5, loaded.Count);
		Assert.Equal(new[] { 1, 3 }, loaded.Numbers);
		Assert.Equal("ok", loaded.Text);
	}

	[Theory]
	[InlineData("")]
	[InlineData("not xml at all")]
	[InlineData("<Settings><Count>1</Count>")]
	public void Populate_DamagedDocument_ThrowsInvalidDataAndAssignsNothing(string content)
	{
		Tolerant target = new();

		Assert.Throws<InvalidDataException>(() => ReadInto(target, content));
		Assert.Equal(5, target.Count);
	}

	private sealed class WithNullableValues
	{
		[Save]
		public Dictionary<string, string?>? Map { get; set; }

		[Save]
		public List<string?>? Names { get; set; }
	}

	[Fact]
	public void Serialize_NullInsideDictionaryOrList_IsLeftOut()
	{
		WithNullableValues loaded = RoundTrip(new WithNullableValues
		{
			Map = new Dictionary<string, string?> { ["a"] = "1", ["b"] = null, ["c"] = "3" },
			Names = ["x", null, "z"],
		});

		Assert.Equal(new[] { "a", "c" }, loaded.Map?.Keys);
		Assert.Equal(new string?[] { "x", "z" }, loaded.Names);
	}

	private sealed class WithCharacters
	{
		[Save]
		public char Letter { get; set; }

		[Save]
		public string? Text { get; set; }
	}

	[Fact]
	public void Serialize_CharactersXmlCannotHold_AreDroppedInsteadOfFailing()
	{
		WithCharacters loaded = RoundTrip(new WithCharacters
		{
			Letter = '\0',
			Text = "a\u0001b\0c 😀 d\uD83De",
		});

		// A default char comes back as a default char; the text loses only what XML cannot store
		// (two control characters and an unpaired surrogate) and keeps the emoji.
		Assert.Equal('\0', loaded.Letter);
		Assert.Equal("abc 😀 de", loaded.Text);
	}

	[Save]
	private struct Point
	{
		[Save]
		public int X { get; set; }

		[Save]
		public int Y { get; set; }
	}

	private sealed class IntList : List<int>
	{
	}

	private sealed class NewlyReadable
	{
		[Save]
		public int? Maybe { get; set; }

		[Save]
		public int? Nothing { get; set; }

		[Save]
		public double? MaybeRatio { get; set; }

		[Save]
		public SampleMode? MaybeMode { get; set; }

		[Save]
		public Guid Id { get; set; }

		[Save]
		public TimeSpan Duration { get; set; }

		[Save]
		public DateTimeOffset Moment { get; set; }

		[Save]
		public int[]? Numbers { get; set; }

		[Save]
		public string[]? Words { get; set; }

		[Save]
		public IntList? Custom { get; set; }

		[Save]
		public Point Location { get; set; }

		[Save]
		public Point? MaybeLocation { get; set; }
	}

	[Theory]
	[InlineData("")]
	[InlineData("en-US")]
	[InlineData("uk-UA")]
	public void TypesThatProgramSettingsCouldNotReadBack_RoundTrip(string culture)
	{
		using CultureScope scope = new(culture);
		NewlyReadable original = new()
		{
			Maybe = 7,
			Nothing = null,
			MaybeRatio = 2.5,
			MaybeMode = SampleMode.Second,
			Id = Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e"),
			Duration = new TimeSpan(1, 2, 3, 4, 500),
			Moment = new DateTimeOffset(2026, 11, 14, 20, 59, 7, TimeSpan.FromHours(2)),
			Numbers = [1, 2, 3],
			Words = ["a", "b"],
			Custom = [4, 5],
			Location = new Point { X = 1, Y = -2 },
			MaybeLocation = new Point { X = 3, Y = 4 },
		};

		NewlyReadable loaded = RoundTrip(original);

		Assert.Equal(7, loaded.Maybe);
		Assert.Null(loaded.Nothing);
		Assert.Equal(2.5, loaded.MaybeRatio);
		Assert.Equal(SampleMode.Second, loaded.MaybeMode);
		Assert.Equal(original.Id, loaded.Id);
		Assert.Equal(original.Duration, loaded.Duration);
		Assert.Equal(original.Moment, loaded.Moment);
		Assert.Equal(original.Moment.Offset, loaded.Moment.Offset);
		Assert.Equal(new[] { 1, 2, 3 }, loaded.Numbers);
		Assert.Equal(new[] { "a", "b" }, loaded.Words);
		Assert.Equal(new[] { 4, 5 }, loaded.Custom);
		Assert.Equal(1, loaded.Location.X);
		Assert.Equal(-2, loaded.Location.Y);
		Assert.Equal(3, loaded.MaybeLocation?.X);
		Assert.Equal(4, loaded.MaybeLocation?.Y);
	}

	#endregion

	#region Store

	[Fact]
	public void Store_SavesAndLoadsSettingsXml()
	{
		using TempFolder folder = new();
		SettingsStore<SampleSettings> store = XmlSettingsStore.Create<SampleSettings>(
			new SettingsStoreOptions { DirectoryPath = folder.Path });
		SampleSettings sample = SampleData.Create();

		Assert.True(store.Save(sample));
		SampleSettings loaded = store.Load();

		Assert.Equal(Path.Combine(folder.Path, "settings.xml"), store.FilePath);
		Assert.Equal(SampleData.Snapshot(sample), SampleData.Snapshot(loaded));
	}

	[Theory]
	[InlineData("")]
	[InlineData("<Settings><Count>1")]
	public void Store_DamagedFile_LoadsDefaultsAndCanBeSavedOver(string content)
	{
		using TempFolder folder = new();
		SettingsStore<SampleSettings> store = XmlSettingsStore.Create<SampleSettings>(
			new SettingsStoreOptions { DirectoryPath = folder.Path });
		Directory.CreateDirectory(folder.Path);
		File.WriteAllText(store.FilePath, content);

		SampleSettings loaded = store.Load();
		loaded.Count = 3;

		Assert.Null(loaded.Text);
		Assert.True(store.Save(loaded));
		Assert.Equal(3, store.Load().Count);
	}

	#endregion
}
