namespace LiteSettings;

/// <summary>
/// Marks what is written to the settings file.
/// </summary>
/// <remarks>
/// On a property: the property is saved and loaded. Properties without the attribute are ignored.
/// On a class or struct: values of this type are saved as nested objects made of their own marked properties.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class SaveAttribute : Attribute
{
}
