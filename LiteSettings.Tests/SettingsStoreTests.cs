using LiteSettings.Tests.TestSupport;

namespace LiteSettings.Tests;

public sealed class SettingsStoreTests : IDisposable
{
	private readonly TempFolder _folder = new();
	private readonly FakeSerializer _serializer = new();

	public void Dispose()
	{
		_folder.Dispose();
	}

	private SettingsStore<FakeSettings> CreateStore(string? fileName = null)
	{
		return new SettingsStore<FakeSettings>(
			_serializer,
			new SettingsStoreOptions
			{
				DirectoryPath = _folder.Path,
				FileName = fileName,
				SaveAttempts = 3,
				SaveRetryDelay = TimeSpan.FromMilliseconds(1),
			});
	}

	[Fact]
	public void Constructor_RejectsMissingSerializer()
	{
		Assert.Throws<ArgumentNullException>(() => new SettingsStore<FakeSettings>(null!));
	}

	[Fact]
	public void Constructor_RejectsInvalidOptions()
	{
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new SettingsStore<FakeSettings>(_serializer, new SettingsStoreOptions { SaveAttempts = 0 }));
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new SettingsStore<FakeSettings>(_serializer, new SettingsStoreOptions { SaveRetryDelay = TimeSpan.FromMilliseconds(-1) }));
		Assert.Throws<ArgumentException>(
			() => new SettingsStore<FakeSettings>(_serializer, new SettingsStoreOptions { DirectoryPath = " " }));
		Assert.Throws<ArgumentException>(
			() => new SettingsStore<FakeSettings>(_serializer, new SettingsStoreOptions { FileName = "" }));
	}

	[Fact]
	public void Constructor_TouchesNothingOnDisk()
	{
		CreateStore();

		Assert.False(Directory.Exists(_folder.Path));
	}

	[Fact]
	public void FileName_ComesFromSerializerUnlessOverridden()
	{
		Assert.Equal(_folder.Combine("settings.fake"), CreateStore().FilePath);
		Assert.Equal(_folder.Combine("profile.dat"), CreateStore("profile.dat").FilePath);
	}

	[Fact]
	public void Save_CreatesFolderAndFile()
	{
		SettingsStore<FakeSettings> store = CreateStore();

		bool saved = store.Save(new FakeSettings { Text = "hello" });

		Assert.True(saved);
		Assert.Equal("hello", File.ReadAllText(store.FilePath));
	}

	[Fact]
	public void Save_ReplacesExistingFileAndLeavesNoTempFile()
	{
		SettingsStore<FakeSettings> store = CreateStore();
		store.Save(new FakeSettings { Text = "a much longer first version" });

		store.Save(new FakeSettings { Text = "second" });

		Assert.Equal("second", File.ReadAllText(store.FilePath));
		string onlyFile = Assert.Single(Directory.GetFiles(_folder.Path));
		Assert.Equal(store.FilePath, onlyFile);
	}

	[Fact]
	public void Save_ReturnsFalse_WhenFolderCannotBeCreated()
	{
		// A file sits where the settings folder's parent should be, so the folder can never be created.
		Directory.CreateDirectory(_folder.Path);
		string blocker = _folder.Combine("blocker");
		File.WriteAllText(blocker, "not a folder");
		SettingsStore<FakeSettings> store = new(
			_serializer,
			new SettingsStoreOptions
			{
				DirectoryPath = Path.Combine(blocker, "settings"),
				SaveAttempts = 3,
				SaveRetryDelay = TimeSpan.FromMilliseconds(1),
			});

		bool saved = store.Save(new FakeSettings());

		Assert.False(saved);
		Assert.Equal(1, _serializer.SerializeCalls);
	}

	[Fact]
	public void Save_RejectsNull()
	{
		Assert.Throws<ArgumentNullException>(() => CreateStore().Save(null!));
	}

	[Fact]
	public void Load_ReturnsDefaults_WhenFileIsMissing()
	{
		FakeSettings settings = CreateStore().Load();

		Assert.Equal("default", settings.Text);
		Assert.Equal(0, _serializer.PopulateCalls);
	}

	[Fact]
	public void Load_ReturnsWhatWasSaved()
	{
		SettingsStore<FakeSettings> store = CreateStore();
		store.Save(new FakeSettings { Text = "kept" });

		Assert.Equal("kept", store.Load().Text);
	}

	[Fact]
	public void Load_ReturnsDefaults_WhenFileIsDamaged()
	{
		SettingsStore<FakeSettings> store = CreateStore();
		Directory.CreateDirectory(_folder.Path);
		File.WriteAllText(store.FilePath, FakeSerializer.InvalidContent);

		FakeSettings settings = store.Load();

		Assert.Equal("default", settings.Text);
		Assert.Equal(1, _serializer.PopulateCalls);
	}

	[Fact]
	public void Load_IntoExistingObject_OverwritesItsValues()
	{
		SettingsStore<FakeSettings> store = CreateStore();
		store.Save(new FakeSettings { Text = "saved" });
		FakeSettings settings = new() { Text = "unsaved edit" };

		store.Load(settings);

		Assert.Equal("saved", settings.Text);
	}

	[Fact]
	public void Load_RejectsNull()
	{
		Assert.Throws<ArgumentNullException>(() => CreateStore().Load(null!));
	}

	[Fact]
	public void SaveAndLoad_FromManyThreads_NeverSeeBrokenContent()
	{
		SettingsStore<FakeSettings> store = CreateStore();
		store.Save(new FakeSettings { Text = "value 0" });

		Parallel.For(1, 40, i =>
		{
			Assert.True(store.Save(new FakeSettings { Text = $"value {i}" }));
			Assert.StartsWith("value ", store.Load().Text);
		});
	}

	[Fact]
	public void TempDirectoryPath_CreatesFolderNextToSettings()
	{
		SettingsStore<FakeSettings> store = CreateStore();

		string temp = store.TempDirectoryPath;

		Assert.Equal(_folder.Combine("Temp"), temp);
		Assert.True(Directory.Exists(temp));
	}

	[Fact]
	public void ClearTempDirectory_RemovesContentButKeepsFolder()
	{
		SettingsStore<FakeSettings> store = CreateStore();
		string temp = store.TempDirectoryPath;
		File.WriteAllText(Path.Combine(temp, "file.tmp"), "x");
		Directory.CreateDirectory(Path.Combine(temp, "sub", "deeper"));
		File.WriteAllText(Path.Combine(temp, "sub", "deeper", "file.tmp"), "x");

		store.ClearTempDirectory();

		Assert.True(Directory.Exists(temp));
		Assert.Empty(Directory.GetFileSystemEntries(temp));
	}

	[Fact]
	public void ClearTempDirectory_DoesNothing_WhenFolderDoesNotExist()
	{
		CreateStore().ClearTempDirectory();

		Assert.False(Directory.Exists(_folder.Path));
	}

	[Fact]
	public void ClearTempDirectory_DoesNotTouchSettingsFile()
	{
		SettingsStore<FakeSettings> store = CreateStore();
		store.Save(new FakeSettings { Text = "kept" });
		File.WriteAllText(Path.Combine(store.TempDirectoryPath, "file.tmp"), "x");

		store.ClearTempDirectory();

		Assert.Equal("kept", store.Load().Text);
	}
}
