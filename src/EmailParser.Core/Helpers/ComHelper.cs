using System.Runtime.InteropServices;

namespace EmailParser.Core.Helpers;

/// <summary>
/// Utility methods for safely releasing COM objects.
/// </summary>
public static class ComHelper
{
    /// <summary>
    /// Releases a COM object reference if it is not <c>null</c> and is a valid COM object.
    /// </summary>
    public static void ReleaseComObject(object? comObject)
    {
        if (comObject is not null && Marshal.IsComObject(comObject))
            Marshal.ReleaseComObject(comObject);
    }
}
