using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using LiteSettings.Xml.Internal;

namespace LiteSettings.Xml;

/// <summary>
/// Reads and writes settings as XML, in the format of the original ProgramSettings library.
/// </summary>
/// <remarks>
/// <para>
/// Every saved property becomes a child element of the root, named after the property. Lists and arrays hold
/// <c>Item</c> elements, dictionaries hold <c>Item</c> elements with <c>Key</c> and <c>Value</c>, types marked with
/// <see cref="SaveAttribute"/> hold one element per saved property, everything else is stored as text.
/// Properties whose value is <see langword="null"/> are left out.
/// </para>
/// <para>
/// Text values are produced with <see cref="object.ToString"/> and read back using the current culture.
/// A file with numbers or dates is therefore tied to the regional settings it was written under.
/// </para>
/// </remarks>
public sealed class XmlSettingsSerializer : ISettingsSerializer
{
	/// <summary>
	/// Name of the root element used by the parameterless constructor.
	/// </summary>
	public const string DefaultRootElementName = "Settings";

	private const string SettingsFileName = "settings.xml";

	// The format has not changed since ProgramSettings, so the files keep carrying its version.
	private const string VersionAttributeName = "Version";
	private const string FormatVersion = "1.0.0.0";

	private const string ItemTagName = "Item";
	private const string KeyTagName = "Key";
	private const string ValueTagName = "Value";

	private static readonly MethodInfo _parseMethod =
		typeof(XmlSettingsSerializer).GetMethod(nameof(Parse), BindingFlags.NonPublic | BindingFlags.Static)!;

	private readonly XName _rootElementName;

	/// <summary>
	/// Creates a serializer that writes the <c>Settings</c> root element.
	/// </summary>
	public XmlSettingsSerializer()
		: this(DefaultRootElementName)
	{
	}

	/// <summary>
	/// Creates a serializer that writes a root element with the given name.
	/// The name only matters for writing: any root element is accepted when reading.
	/// </summary>
	/// <param name="rootElementName">Valid XML element name.</param>
	public XmlSettingsSerializer(string rootElementName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(rootElementName);

		try
		{
			_rootElementName = XName.Get(rootElementName);
		}
		catch (XmlException e)
		{
			throw new ArgumentException($"\"{rootElementName}\" is not a valid XML element name.", nameof(rootElementName), e);
		}
	}

	/// <summary>
	/// Name of the root element this serializer writes.
	/// </summary>
	public string RootElementName => _rootElementName.ToString();

	/// <inheritdoc />
	public string DefaultFileName => SettingsFileName;

	/// <inheritdoc />
	public void Serialize(Stream stream, object settings)
	{
		ArgumentNullException.ThrowIfNull(stream);
		ArgumentNullException.ThrowIfNull(settings);

		XElement root = new(_rootElementName, new XAttribute(VersionAttributeName, FormatVersion));
		foreach (PropertyInfo property in SaveMetadata.GetProperties(settings.GetType()))
		{
			object? value = property.GetValue(settings);
			if (value == null)
				continue;

			XElement? element = ConvertToXElement(property.Name, value);
			if (element != null)
			{
				root.Add(element);
			}
		}

		root.Save(stream);
	}

	/// <inheritdoc />
	public void Populate(Stream stream, object target)
	{
		ArgumentNullException.ThrowIfNull(stream);
		ArgumentNullException.ThrowIfNull(target);

		XElement root;
		try
		{
			root = XElement.Load(stream);
		}
		catch (XmlException e)
		{
			throw new InvalidDataException("The settings file is not a well-formed XML document.", e);
		}

		// Only direct children of the root are settings. Deeper elements belong to nested values
		// and must not be mistaken for a top-level property with the same name.
		IReadOnlyList<PropertyInfo> properties = SaveMetadata.GetProperties(target.GetType());
		foreach (XElement element in root.Elements())
		{
			string name = element.Name.LocalName;
			PropertyInfo? property = properties.FirstOrDefault(p => p.Name == name);
			if (property != null
				&& TryConvertFromXElement(property.PropertyType, element, out object? value))
			{
				property.SetValue(target, value);
			}
		}
	}

	/// <returns>
	/// The element, or <see langword="null"/> when <paramref name="obj"/> is a key/value pair
	/// with a missing part and therefore cannot be written.
	/// </returns>
	private static XElement? ConvertToXElement(string name, object obj)
	{
		XElement xElement = new(name);
		Type type = obj.GetType();

		if (obj is IList list)
		{
			foreach (object? item in list)
			{
				if (item == null)
					continue;

				AddIfWritten(xElement, ConvertToXElement(ItemTagName, item));
			}
		}
		else if (obj is IDictionary dictionary)
		{
			// Enumerating IDictionary yields DictionaryEntry values, handled by the next branch.
			foreach (object? item in dictionary)
			{
				if (item == null)
					continue;

				AddIfWritten(xElement, ConvertToXElement(ItemTagName, item));
			}
		}
		else if (IsKeyValuePair(type) || obj is DictionaryEntry)
		{
			object? key = type.GetProperty("Key")?.GetValue(obj);
			object? value = type.GetProperty("Value")?.GetValue(obj);
			if (key == null || value == null)
				return null;

			XElement? keyElement = ConvertToXElement(KeyTagName, key);
			XElement? valueElement = ConvertToXElement(ValueTagName, value);
			if (keyElement == null || valueElement == null)
				return null;

			xElement.Add(keyElement);
			xElement.Add(valueElement);
		}
		else if (SaveMetadata.IsSaveType(type))
		{
			foreach (PropertyInfo prop in SaveMetadata.GetProperties(type))
			{
				object? value = prop.GetValue(obj);
				if (value == null)
					continue;

				AddIfWritten(xElement, ConvertToXElement(prop.Name, value));
			}
		}
		else
		{
			string stringValue = SpecialSymbolsReplacer.ReplaceSpecialSymbols(obj.ToString() ?? string.Empty);
			xElement.Value = RemoveCharactersXmlCannotHold(stringValue);
		}

		return xElement;
	}

	/// <summary>
	/// XML 1.0 has no way to store most control characters (for example the <c>'\0'</c> of a default
	/// <see cref="char"/>) or unpaired surrogates, and the writer throws on them. They are dropped instead,
	/// so that one odd value does not make the whole file impossible to save. Any other text is returned as is.
	/// </summary>
	private static string RemoveCharactersXmlCannotHold(string text)
	{
		StringBuilder? cleaned = null;

		for (int i = 0; i < text.Length; ++i)
		{
			char current = text[i];
			bool isPair = char.IsHighSurrogate(current) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]);

			if (isPair || XmlConvert.IsXmlChar(current))
			{
				if (cleaned != null)
				{
					cleaned.Append(current);
					if (isPair)
					{
						cleaned.Append(text[i + 1]);
					}
				}
				if (isPair)
				{
					++i;
				}
			}
			else
			{
				cleaned ??= new StringBuilder(text, 0, i, text.Length);
			}
		}

		return cleaned?.ToString() ?? text;
	}

	private static void AddIfWritten(XElement parent, XElement? child)
	{
		if (child != null)
		{
			parent.Add(child);
		}
	}

	private static bool TryConvertFromXElement(Type type, XElement? xElement, out object? value)
	{
		value = null;
		if (xElement == null)
			return false;

		// A nullable property holds either nothing (then it is not in the file) or a value of the underlying type.
		type = Nullable.GetUnderlyingType(type) ?? type;

		try
		{
			if (type.IsArray)
			{
				if (type.GetArrayRank() != 1)
					return false;

				Type innerType = type.GetElementType()!;
				List<object?> items = [];
				foreach (XElement innerXElement in xElement.Elements(ItemTagName))
				{
					if (TryConvertFromXElement(innerType, innerXElement, out object? innerValue))
					{
						items.Add(innerValue);
					}
				}

				Array array = Array.CreateInstance(innerType, items.Count);
				for (int i = 0; i < items.Count; ++i)
				{
					array.SetValue(items[i], i);
				}
				value = array;
			}
			else if (typeof(IList).IsAssignableFrom(type))
			{
				value = Activator.CreateInstance(type);
				IList list = (IList)value!;
				Type innerType = GetItemType(type);
				foreach (XElement innerXElement in xElement.Elements(ItemTagName))
				{
					if (TryConvertFromXElement(innerType, innerXElement, out object? innerValue))
					{
						list.Add(innerValue);
					}
				}
			}
			else if (typeof(IDictionary).IsAssignableFrom(type))
			{
				value = Activator.CreateInstance(type);
				IDictionary dictionary = (IDictionary)value!;
				(Type innerTypeKey, Type innerTypeValue) = GetKeyValueTypes(type, typeof(IDictionary<,>));
				foreach (XElement innerXElement in xElement.Elements(ItemTagName))
				{
					if (TryConvertFromXElement(innerTypeKey, innerXElement.Element(KeyTagName), out object? innerValueKey)
						&& TryConvertFromXElement(innerTypeValue, innerXElement.Element(ValueTagName), out object? innerValueValue))
					{
						dictionary.Add(innerValueKey!, innerValueValue);
					}
				}
			}
			else if (IsKeyValuePair(type))
			{
				Type innerTypeKey = type.GenericTypeArguments[0];
				Type innerTypeValue = type.GenericTypeArguments[1];
				if (TryConvertFromXElement(innerTypeKey, xElement.Element(KeyTagName), out object? innerValueKey)
					&& TryConvertFromXElement(innerTypeValue, xElement.Element(ValueTagName), out object? innerValueValue))
				{
					value = Activator.CreateInstance(type, innerValueKey, innerValueValue);
				}
			}
			else if (SaveMetadata.IsSaveType(type))
			{
				value = Activator.CreateInstance(type);
				foreach (PropertyInfo prop in SaveMetadata.GetProperties(type))
				{
					XElement? innerXElement = xElement.Elements(prop.Name).FirstOrDefault();
					if (innerXElement != null)
					{
						if (TryConvertFromXElement(prop.PropertyType, innerXElement, out object? innerValue))
						{
							prop.SetValue(value, innerValue);
						}
					}
				}
			}
			else
			{
				string stringValue = SpecialSymbolsReplacer.ReplaceBackSpecialSymbols(xElement.Value);
				value = ConvertFromString(type, stringValue);
			}
			return true;
		}
		catch
		{
			// One value that cannot be read must not prevent the rest of the file from loading.
			value = null;
			return false;
		}
	}

	private static object ConvertFromString(Type type, string stringValue)
	{
		if (type.IsEnum)
		{
			return Enum.Parse(type, stringValue);
		}

		if (type == typeof(string) || type == typeof(object))
		{
			return stringValue;
		}

		// Primitive types, decimal and DateTime: the same conversion (and the same current culture) as in ProgramSettings.
		if (Type.GetTypeCode(type) != TypeCode.Object)
		{
			return Convert.ChangeType(stringValue, type);
		}

		// Types Convert does not know (Guid, TimeSpan, DateTimeOffset, ...) are read through their own parser.
		// The current culture mirrors the ToString() call that produced the text.
		if (IsParsable(type))
		{
			return _parseMethod.MakeGenericMethod(type).Invoke(null, [stringValue, CultureInfo.CurrentCulture])!;
		}

		throw new NotSupportedException($"Values of type {type} cannot be read from text.");
	}

	private static object Parse<TValue>(string stringValue, IFormatProvider provider) where TValue : IParsable<TValue>
	{
		return TValue.Parse(stringValue, provider);
	}

	private static bool IsParsable(Type type)
	{
		return type
			.GetInterfaces()
			.Any(i => i.IsGenericType
				&& i.GetGenericTypeDefinition() == typeof(IParsable<>)
				&& i.GenericTypeArguments[0] == type);
	}

	private static bool IsKeyValuePair(Type type)
	{
		return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);
	}

	private static Type GetItemType(Type listType)
	{
		Type? genericList = FindInterface(listType, typeof(IList<>));
		if (genericList != null)
		{
			return genericList.GenericTypeArguments[0];
		}
		return listType.IsGenericType ? listType.GenericTypeArguments[0] : typeof(object);
	}

	private static (Type Key, Type Value) GetKeyValueTypes(Type dictionaryType, Type genericInterface)
	{
		Type? genericDictionary = FindInterface(dictionaryType, genericInterface);
		if (genericDictionary != null)
		{
			return (genericDictionary.GenericTypeArguments[0], genericDictionary.GenericTypeArguments[1]);
		}
		return dictionaryType.IsGenericType && dictionaryType.GenericTypeArguments.Length >= 2
			? (dictionaryType.GenericTypeArguments[0], dictionaryType.GenericTypeArguments[1])
			: (typeof(object), typeof(object));
	}

	private static Type? FindInterface(Type type, Type genericInterface)
	{
		return type
			.GetInterfaces()
			.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericInterface);
	}
}
