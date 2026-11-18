# LiteSettings

A small settings library for .NET 10 applications. Mark the properties you want to keep with `[Save]`,
and LiteSettings stores them in a file under the user's local application data folder.

LiteSettings is the successor of ProgramSettings. It reads and writes the same `settings.xml` in the same
folder, so an application can switch to it without losing the settings of its users.

| Package | Contents |
| --- | --- |
| `DOtrishko.LiteSettings.Xml` | XML format. **Install this one**; it brings the core package with it. |
| `DOtrishko.LiteSettings` | Core: `[Save]`, `SettingsStore<T>`, `ISettingsSerializer`. Has no file format of its own. |

## Quick start

```
dotnet add package DOtrishko.LiteSettings.Xml
```

```csharp
using LiteSettings;
using LiteSettings.Xml;

public class AppSettings
{
    [Save]
    public string Language { get; set; } = "en";

    [Save]
    public List<string> RecentFiles { get; set; } = [];

    public string NotSaved { get; set; } = "";   // no attribute, never written
}

SettingsStore<AppSettings> store = XmlSettingsStore.Create<AppSettings>();

AppSettings settings = store.Load();   // defaults when there is no file yet
settings.Language = "uk";
store.Save(settings);                  // false when the file could not be written
```

Create one store when the application starts and keep it, together with the loaded settings object,
for as long as the application runs (a singleton in a DI container works well). A store can be used
from several threads.

## What is saved

- Public instance properties marked with `[Save]` that have a getter and a setter. The setter may be private.
- Static properties, indexers and properties without the attribute are ignored.
- Properties holding `null` are not written. When the file is loaded, a property that is not in the file
  keeps the value given by the constructor of the settings class.

Supported property types:

| Kind | Types |
| --- | --- |
| Text values | `string`, `bool`, `char`, all integer and floating point types, `decimal`, `DateTime`, enums, and every type implementing `IParsable<T>` such as `Guid`, `TimeSpan`, `DateTimeOffset`, `DateOnly` |
| Nullable | `Nullable<T>` of any supported value type |
| Collections | one-dimensional arrays, `List<T>` and other classes implementing `IList`, `Dictionary<TKey, TValue>` and other classes implementing `IDictionary`, `KeyValuePair<TKey, TValue>` |
| Nested objects | classes and structs marked with `[Save]`; their own `[Save]` properties are stored. A parameterless constructor is required |

```csharp
[Save]
public class WindowPlacement
{
    [Save]
    public int Left { get; set; }

    [Save]
    public int Top { get; set; }
}

public class AppSettings
{
    [Save]
    public WindowPlacement? MainWindow { get; set; }

    [Save]
    public Dictionary<string, WindowPlacement> ToolWindows { get; set; } = [];
}
```

Null items of a list and dictionary entries with a null value are skipped. A collection property declared
as an interface (`IList<T>`, `IReadOnlyList<T>`, ...) is written but cannot be loaded; declare the concrete type.

## Where the file is stored

```
{LocalApplicationData}\{Company}\{Product}_{postfix}\settings.xml
```

- `Company` and `Product` come from the assembly attributes of the assembly that declares the settings class
  (`<Company>` and `<Product>` in the project file; both default to the assembly name).
- `postfix` is derived from the location of that assembly, so two copies of an application installed in
  different folders keep separate settings. In a single-file application the location is empty, and all
  copies share one folder.

`store.DirectoryPath` and `store.FilePath` return the resolved paths.

## Options

```csharp
SettingsStore<AppSettings> store = XmlSettingsStore.Create<AppSettings>(new SettingsStoreOptions
{
    Assembly = typeof(Program).Assembly,          // which assembly identifies the application
    DirectoryPath = @"D:\Portable\MyApp",         // use this folder instead of the computed one
    FileName = "settings.xml",                    // default comes from the serializer
    SaveAttempts = 10,                            // default
    SaveRetryDelay = TimeSpan.FromMilliseconds(200),   // default
});
```

To keep a second object in its own file next to `settings.xml`, create another store:

```csharp
SettingsStore<Profiles> profiles = new(
    new XmlSettingsSerializer("Profiles"),                     // name of the root element
    new SettingsStoreOptions { FileName = "profiles.xml" });
```

## Saving and loading

- `Save` writes a temporary file and then moves it over `settings.xml`, so a crash while saving cannot leave
  a half-written file.
- When the file or folder is busy or not accessible, `Save` retries (`SaveAttempts` times, waiting
  `SaveRetryDelay` in between, on the calling thread) and returns `false` if every attempt failed.
- `Load` never fails because of the file content. A missing or damaged file gives the default values;
  a single value that cannot be read (for example an enum member that no longer exists) is skipped and the
  rest of the file is loaded.
- `Load(existing)` fills an object you already have, for example to discard unsaved changes.

## Temp folder

`store.TempDirectoryPath` is a `Temp` folder next to the settings file for the application's own temporary
files. It is created when the property is read. Nothing is removed automatically; call
`store.ClearTempDirectory()` at startup if leftovers of the previous run must go away.

## Adding another file format

A format is a class implementing `ISettingsSerializer`:

```csharp
public sealed class JsonSettingsSerializer : ISettingsSerializer
{
    public string DefaultFileName => "settings.json";

    public void Serialize(Stream stream, object settings) { /* write the document */ }

    public void Populate(Stream stream, object target) { /* read values into target */ }
}

SettingsStore<AppSettings> store = new(new JsonSettingsSerializer());
```

`SaveMetadata.GetProperties(type)` returns the properties selected by `[Save]`, and
`SaveMetadata.IsSaveType(type)` tells whether a type is stored as a nested object, so every format can
follow the same rules. `Populate` must throw `InvalidDataException` when the stream is not a readable document.

## Migrating from ProgramSettings

Before:

```csharp
using ProgramSettings;

public class Settings : BaseProgramSettings
{
    [Save]
    public static string Language { get; set; } = "en";

    static Settings()
    {
        Init(typeof(Settings));
        InnerReload();
    }

    public static bool Save() => InnerSave();
}
```

After:

```csharp
using LiteSettings;
using LiteSettings.Xml;

public class Settings
{
    [Save]
    public string Language { get; set; } = "en";
}

SettingsStore<Settings> store = XmlSettingsStore.Create<Settings>();
Settings settings = store.Load();
```

Checklist:

1. **Keep property names and types.** Element names in the file are the property names. Remove `static`,
   the base class and the static constructor.
2. **Keep the settings class in the same assembly** that used to call `Init`. The folder is derived from that
   assembly. If the class has to move, pass the original assembly in `SettingsStoreOptions.Assembly`.
3. **`[SaveCrypt]` no longer exists.** Rename or remove such properties. If a property keeps its name and gets
   `[Save]`, the old encrypted text (Base64) is loaded into it as is. A renamed property starts with its
   default value, and the old element disappears from the file on the next save.
4. **The Temp folder is no longer cleared on start.** Call `store.ClearTempDirectory()` yourself if the
   application relied on that.
5. **`_savingAttempts` and `_savingAttemptsDelay`** became `SaveAttempts` and `SaveRetryDelay` in the options.
6. **Additional files** written with the `InnerSave(rootElementName, properties, obj, filePath)` overload become
   separate stores, see [Options](#options).

What behaves differently from ProgramSettings:

- An element nested inside another value no longer overwrites a top-level property with the same name,
  so properties may now be called `Item`, `Key` or `Value`.
- An unknown enum value, a damaged file, a `null` inside a dictionary, a default `char` or a control character
  in a string no longer make loading or saving throw.
- `Nullable<T>`, `Guid`, `TimeSpan`, `DateTimeOffset` and arrays are now loaded (they were written before,
  but could not be read back).
- `Save` returns `false` instead of throwing when access to the file is denied.

The file format itself is unchanged: for the same data LiteSettings produces byte for byte the same file,
and each library reads the files of the other. The test suite checks this against the original ProgramSettings
sources.

## Limitations

- **Numbers and dates depend on regional settings.** Values are written with `ToString()` and read with the
  current culture, exactly as ProgramSettings did (`1,5` on a Ukrainian system, `1.5` on an English one).
  A file is tied to the culture it was written under; after the regional settings of the system change,
  numbers with a fraction and dates may be read incorrectly or fall back to their defaults.
- **Trimming and Native AOT are not supported.** The library is based on reflection.
- Line breaks inside strings are stored in the platform's form and come back as `\n`.

## Building

```
dotnet test
dotnet pack -c Release -o artifacts
```
