namespace Nulah.UpApi.Domain;

/// <summary>
/// Indicates that this object should reflect 1:1 with a response from the UpApi.
/// <para>
///	This attribute also implies that any naming conventions or other corrections should be ignored unless otherwise stated
/// as they match to an expected output from Api responses
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum)]
internal class ApiModelAttribute : Attribute;