using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace LiteSettings.Internal;

/// <summary>
/// Computes the settings folder exactly as ProgramSettings did:
/// <c>{LocalApplicationData}\{Company}\{Product}_{postfix}</c>.
/// Any change here moves the folder and makes existing settings invisible to the application.
/// </summary>
internal static class SettingsPathResolver
{
	private const int PostfixLength = 32;

	/// <summary>
	/// First 32 letters and digits of Base64(SHA-256(UTF-8(<paramref name="input"/>))).
	/// </summary>
	public static string GeneratePostfix(string input)
	{
		byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
		string base64Hash = Convert.ToBase64String(hashBytes);

		StringBuilder output = new(PostfixLength);
		for (int i = 0; i < base64Hash.Length && output.Length < PostfixLength; ++i)
		{
			if (char.IsLetterOrDigit(base64Hash[i]))
			{
				output.Append(base64Hash[i]);
			}
		}
		return output.ToString();
	}

	/// <summary>
	/// The postfix is derived from the assembly location on purpose, including the case of a single-file
	/// application where the location is an empty string: installed applications already use that value.
	/// </summary>
	public static string GeneratePostfix(Assembly assembly)
	{
		return GeneratePostfix(assembly.Location);
	}

	public static string GetDirectoryPath(Assembly assembly, string postfix)
	{
		// DoNotVerify returns the same path as the default option, but does not turn into
		// an empty string on systems where the folder has not been created yet.
		string appDataDirectory = Environment.GetFolderPath(
			Environment.SpecialFolder.LocalApplicationData,
			Environment.SpecialFolderOption.DoNotVerify);

		string companyName = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company
			?? throw new InvalidOperationException(
				$"Assembly \"{assembly.GetName().Name}\" has no AssemblyCompanyAttribute, the settings folder cannot be determined.");
		string programName = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product
			?? throw new InvalidOperationException(
				$"Assembly \"{assembly.GetName().Name}\" has no AssemblyProductAttribute, the settings folder cannot be determined.");

		return Path.Combine(appDataDirectory, companyName, programName + "_" + postfix);
	}
}
