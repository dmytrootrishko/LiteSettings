namespace LiteSettings.Tests;

public sealed class SaveMetadataTests
{
	private class Sample
	{
		[Save]
		public int First { get; set; }

		public int NotMarked { get; set; }

		[Save]
		public string? Second { get; private set; }

		[Save]
		public int GetOnly { get; } = 1;

		[Save]
		public static int Static { get; set; }

		[Save]
		public int this[int index]
		{
			get => index;
			set { }
		}

		[Save]
		internal int NotPublic { get; set; }
	}

	[Save]
	private class Marked
	{
	}

	private sealed class DerivedFromMarked : Marked
	{
	}

	[Save]
	private struct MarkedStruct
	{
	}

	[Fact]
	public void GetProperties_ReturnsMarkedReadWriteInstanceProperties()
	{
		string[] names = SaveMetadata.GetProperties(typeof(Sample)).Select(p => p.Name).ToArray();

		Assert.Equal(new[] { "First", "Second" }, names);
	}

	[Fact]
	public void GetProperties_ReturnsTheCachedList()
	{
		Assert.Same(SaveMetadata.GetProperties(typeof(Sample)), SaveMetadata.GetProperties(typeof(Sample)));
	}

	[Fact]
	public void IsSaveType_IsTrueForMarkedTypesAndTheirDescendants()
	{
		Assert.True(SaveMetadata.IsSaveType(typeof(Marked)));
		Assert.True(SaveMetadata.IsSaveType(typeof(DerivedFromMarked)));
		Assert.True(SaveMetadata.IsSaveType(typeof(MarkedStruct)));
		Assert.False(SaveMetadata.IsSaveType(typeof(Sample)));
		Assert.False(SaveMetadata.IsSaveType(typeof(string)));
	}

	[Fact]
	public void Methods_RejectNull()
	{
		Assert.Throws<ArgumentNullException>(() => SaveMetadata.GetProperties(null!));
		Assert.Throws<ArgumentNullException>(() => SaveMetadata.IsSaveType(null!));
	}
}
