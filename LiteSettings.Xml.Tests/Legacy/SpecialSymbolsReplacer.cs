namespace ProgramSettings;

internal static class SpecialSymbolsReplacer
{
	private static readonly List<KeyValuePair<string, string>> specialSymbolsDictionary;

	static SpecialSymbolsReplacer()
	{
		specialSymbolsDictionary =
			[
				new KeyValuePair<string, string>("&", "&amp;"),
				new KeyValuePair<string, string>("'", "&apos;"),
				new KeyValuePair<string, string>("\"", "&quot;"),
				new KeyValuePair<string, string>("<", "&lt;"),
				new KeyValuePair<string, string>(">", "&gt;"),
				new KeyValuePair<string, string>("\\", "&sl;"),
			];
	}

	public static string ReplaceSpecialSymbols(string sourceStr)
	{
		foreach (KeyValuePair<string, string> pair in specialSymbolsDictionary)
		{
			sourceStr = sourceStr.Replace(pair.Key, pair.Value);
		}
		return sourceStr;
	}

	public static string ReplaceBackSpecialSymbols(string sourceStr)
	{
		for (int i = specialSymbolsDictionary.Count - 1; i >= 0; --i)
		{
			KeyValuePair<string, string> pair = specialSymbolsDictionary[i];
			sourceStr = sourceStr.Replace(pair.Value, pair.Key);
		}
		return sourceStr;
	}
}
