using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace ProgramSettings;

public class BaseProgramSettings
{
	private const string SettingsFileName = "settings.xml";
	private const string TempFolderName = "Temp";
	private const string RootElementName = "Settings";

	private const string ItemTagName = "Item";
	private const string KeyTagName = "Key";
	private const string ValueTagName = "Value";

	private static string? _uniquePostfix;
	private static string? _directoryPath;
	private static string? _tempDirectoryPath;
	private static Version? _version;
	private static List<PropertyInfo>? _saveProperties;

	protected static int _savingAttempts = 10;
	protected static int _savingAttemptsDelay = 200;

	protected static void Init(Type inheritType)
	{
		_saveProperties =
			inheritType
				.GetProperties()
				.Where(p => p
					.GetCustomAttributes(typeof(SaveAttribute), true)
					.FirstOrDefault() != null)
				.ToList();

		foreach (PropertyInfo propertyInfo in _saveProperties)
		{
			if (propertyInfo.Name == ItemTagName
				|| propertyInfo.Name == KeyTagName
				|| propertyInfo.Name == ValueTagName)
			{
				throw new Exception($"Invalid property name \"{propertyInfo.Name}\"");
			}
		}

		Assembly assembly = Assembly.GetCallingAssembly();
		string appDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		string companyName = ((AssemblyCompanyAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyCompanyAttribute), false)!).Company;
		string programName = ((AssemblyProductAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute), false)!).Product;
		_uniquePostfix = GeneratePostfix(assembly.Location);
		_directoryPath = Path.Combine(appDataDirectory, companyName, programName + "_" + _uniquePostfix);
		_tempDirectoryPath = Path.Combine(_directoryPath, TempFolderName);
		_version = new Version(1, 0, 0, 0);

		if (Directory.Exists(_tempDirectoryPath))
		{
			InnerClearTempDirectory();
		}
		else
		{
			Directory.CreateDirectory(_tempDirectoryPath);
		}
	}

	protected static string? GetUniquePostfix() => _uniquePostfix;
	protected static string? GetDirectoryPath() => _directoryPath;
	protected static string? GetTempDirectoryPath() => _tempDirectoryPath;
	protected static Version? GetVersion() => _version;

	protected static void InnerReload(List<PropertyInfo> properties, object? obj, string filePath)
	{
		if (!File.Exists(filePath))
			return;

		XElement root = XElement.Load(filePath);
		LoadProperties(root, properties, obj);
	}

	protected static void InnerReload()
	{
		if (_saveProperties == null)
			return;

		InnerReload(_saveProperties, null, Path.Combine(_directoryPath!, SettingsFileName));
	}

	protected static void LoadProperties(XElement root, List<PropertyInfo> properties, object? obj)
	{
		string name = root.Name.LocalName;
		PropertyInfo? propertyInfo = properties.FirstOrDefault(s => s.Name == name);
		if (propertyInfo != null
			&& TryConvertFromXElement(propertyInfo, propertyInfo.PropertyType, root, out object? value))
		{
			propertyInfo.SetValue(obj, value);
		}

		foreach (XElement element in root.Elements())
		{
			LoadProperties(element, properties, obj);
		}
	}

	protected static bool InnerSave(string rootElementName, List<PropertyInfo> properties, object? obj, string filePath)
	{
		XElement root = new XElement(rootElementName, new XAttribute(nameof(Version), _version!.ToString()));
		foreach (PropertyInfo property in properties)
		{
			object? value = property.GetValue(obj);
			if (value == null)
				continue;

			root.Add(ConvertToXElement(property, property.Name, value));
		}

		return SaveXMLFile(root, Path.Combine(_directoryPath!, filePath));
	}

	protected static bool InnerSave()
	{
		if (_saveProperties == null)
			return false;

		return InnerSave(RootElementName, _saveProperties, null, SettingsFileName);
	}

	private static bool SaveXMLFile(XElement root, string path)
	{
		for (int i = 0; i < _savingAttempts; ++i)
		{
			try
			{
				root.Save(path);
				return true;
			}
			catch (IOException)
			{
				if (i != _savingAttempts - 1)
				{
					Thread.Sleep(_savingAttemptsDelay);
				}
			}
		}
		return false;
	}

	protected static void InnerClearTempDirectory()
	{
		foreach (string file in Directory.GetFiles(_tempDirectoryPath!))
		{
			File.Delete(file);
		}
		foreach (string dir in Directory.GetDirectories(_tempDirectoryPath!))
		{
			Directory.Delete(dir, true);
		}
	}

	private static string GeneratePostfix(string input)
	{
		string output = string.Empty;
		using (SHA256 sha256 = SHA256.Create())
		{
			byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
			string base64Hash = Convert.ToBase64String(hashBytes);
			for (int i = 0; i < base64Hash.Length && output.Length < 32; ++i)
			{
				if (char.IsLetterOrDigit(base64Hash[i]))
				{
					output += base64Hash[i];
				}
			}
		}
		return output;
	}

	protected static XElement ConvertToXElement(PropertyInfo? propertyInfo, string name, object obj)
	{
		XElement xElement = new XElement(name);

		if (obj is IList list)
		{
			foreach (object item in list)
			{
				if (item == null)
					continue;

				xElement.Add(ConvertToXElement(null, ItemTagName, item));
			}
		}
		else if (obj is IDictionary dictionary)
		{
			foreach (object item in dictionary)
			{
				if (item == null)
					continue;

				xElement.Add(ConvertToXElement(null, ItemTagName, item));
			}
		}
		else if ((obj.GetType().IsGenericType && obj.GetType().GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
			|| obj is DictionaryEntry)
		{
			xElement.Add(ConvertToXElement(null, KeyTagName, obj.GetType().GetProperty("Key")?.GetValue(obj)!));
			xElement.Add(ConvertToXElement(null, ValueTagName, obj.GetType().GetProperty("Value")?.GetValue(obj)!));
		}
		else if (obj.GetType().IsDefined(typeof(SaveAttribute), true))
		{
			IEnumerable<PropertyInfo> properties = obj.GetType().GetProperties()
				.Where(p => p
					.GetCustomAttributes(typeof(SaveAttribute), true)
					.FirstOrDefault() != null);

			foreach (PropertyInfo prop in properties)
			{
				object? value = prop.GetValue(obj);
				if (value == null)
					continue;

				xElement.Add(ConvertToXElement(prop, prop.Name, value));
			}
		}
		else
		{
			string stringValue = SpecialSymbolsReplacer.ReplaceSpecialSymbols(obj.ToString()!);
			if (propertyInfo?.GetCustomAttributes(typeof(SaveCryptAttribute), true).FirstOrDefault() != null)
			{
				try
				{
					byte[] encrypted = ProtectedData.Protect(Encoding.UTF8.GetBytes(stringValue), null, DataProtectionScope.CurrentUser);
					stringValue = Convert.ToBase64String(encrypted);
				}
				catch { }
			}
			xElement.Value = stringValue;
		}

		return xElement;
	}

	protected static bool TryConvertFromXElement(PropertyInfo? propertyInfo, Type type, XElement xElement, out object? value)
	{
		value = null;
		try
		{
			if (typeof(IList).IsAssignableFrom(type))
			{
				value = Activator.CreateInstance(type);
				IList list = (IList)value!;
				Type innerType = type.IsGenericType ? type.GenericTypeArguments[0] : typeof(object);
				foreach (XElement innerXElement in xElement.Elements(ItemTagName))
				{
					if (TryConvertFromXElement(null, innerType, innerXElement, out object? innerValue))
					{
						list.Add(innerValue);
					}
				}
			}
			else if (typeof(IDictionary).IsAssignableFrom(type))
			{
				value = Activator.CreateInstance(type);
				IDictionary dictionary = (IDictionary)value!;
				Type innerTypeKey = type.IsGenericType ? type.GenericTypeArguments[0] : typeof(object);
				Type innerTypeValue = type.IsGenericType ? type.GenericTypeArguments[1] : typeof(object);
				foreach (XElement innerXElement in xElement.Elements(ItemTagName))
				{
					if (TryConvertFromXElement(null, innerTypeKey, innerXElement.Element(KeyTagName)!, out object? innerValueKey)
						&& TryConvertFromXElement(null, innerTypeValue, innerXElement.Element(ValueTagName)!, out object? innerValueValue))
					{
						dictionary.Add(innerValueKey!, innerValueValue);
					}
				}
			}
			else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
			{
				Type innerTypeKey = type.IsGenericType ? type.GenericTypeArguments[0] : typeof(object);
				Type innerTypeValue = type.IsGenericType ? type.GenericTypeArguments[1] : typeof(object);
				if (TryConvertFromXElement(null, innerTypeKey, xElement.Element(KeyTagName)!, out object? innerValueKey)
						&& TryConvertFromXElement(null, innerTypeValue, xElement.Element(ValueTagName)!, out object? innerValueValue))
				{
					value = Activator.CreateInstance(type, innerValueKey, innerValueValue);
				}
			}
			else if (type.IsDefined(typeof(SaveAttribute), true))
			{
				value = Activator.CreateInstance(type);
				IEnumerable<PropertyInfo> properties = type.GetProperties()
					.Where(p => p
						.GetCustomAttributes(typeof(SaveAttribute), true)
						.FirstOrDefault() != null);

				foreach (PropertyInfo prop in properties)
				{
					XElement? innerXElement = xElement.Elements(prop.Name).FirstOrDefault();
					if (innerXElement != null)
					{
						if (TryConvertFromXElement(prop, prop.PropertyType, innerXElement, out object? innerValue))
						{
							prop.SetValue(value, innerValue);
						}
					}
				}
			}
			else
			{
				string stringValue = SpecialSymbolsReplacer.ReplaceBackSpecialSymbols(xElement.Value);
				if (propertyInfo?.GetCustomAttributes(typeof(SaveCryptAttribute), true).FirstOrDefault() != null)
				{
					try
					{
						byte[] decrypted = ProtectedData.Unprotect(Convert.FromBase64String(stringValue), null, DataProtectionScope.CurrentUser);
						stringValue = Encoding.UTF8.GetString(decrypted);
					}
					catch
					{
						stringValue = string.Empty;
					}
				}
				value = ConvertFromString(type, stringValue);
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static object ConvertFromString(Type type, string stringValue)
	{
		if (type.IsEnum)
		{
			try
			{
				return Enum.Parse(type, stringValue);
			}
			catch
			{
				return false;
			}
		}
		else
		{
			return Convert.ChangeType(stringValue, type);
		}
	}
}
