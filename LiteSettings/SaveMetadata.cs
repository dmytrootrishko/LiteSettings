using System.Collections.Concurrent;
using System.Reflection;

namespace LiteSettings;

/// <summary>
/// Tells serializers what <see cref="SaveAttribute"/> selects, so that every format saves the same set of properties.
/// </summary>
public static class SaveMetadata
{
	private static readonly ConcurrentDictionary<Type, IReadOnlyList<PropertyInfo>> _properties = new();

	/// <summary>
	/// Returns the properties of <paramref name="type"/> that take part in saving and loading.
	/// </summary>
	/// <remarks>
	/// These are public instance properties marked with <see cref="SaveAttribute"/> that have both
	/// a getter and a setter (the setter may be non-public). Static properties and indexers are never included.
	/// </remarks>
	/// <param name="type">Settings type or a nested type marked with <see cref="SaveAttribute"/>.</param>
	/// <returns>Properties in reflection order. The list is cached per type.</returns>
	public static IReadOnlyList<PropertyInfo> GetProperties(Type type)
	{
		ArgumentNullException.ThrowIfNull(type);
		return _properties.GetOrAdd(type, FindProperties);
	}

	/// <summary>
	/// Tells whether values of <paramref name="type"/> are saved as nested objects,
	/// that is whether the type itself (or its base type) is marked with <see cref="SaveAttribute"/>.
	/// </summary>
	/// <param name="type">Type of a value found inside the settings object.</param>
	public static bool IsSaveType(Type type)
	{
		ArgumentNullException.ThrowIfNull(type);
		return type.IsDefined(typeof(SaveAttribute), true);
	}

	private static IReadOnlyList<PropertyInfo> FindProperties(Type type)
	{
		return type
			.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(p => p.IsDefined(typeof(SaveAttribute), true)
				&& p.CanRead
				&& p.CanWrite
				&& p.GetIndexParameters().Length == 0)
			.ToArray();
	}
}
