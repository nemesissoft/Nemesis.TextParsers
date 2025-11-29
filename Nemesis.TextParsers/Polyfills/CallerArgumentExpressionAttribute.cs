#if NETSTANDARD2_0
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Runtime.CompilerServices;
#pragma warning restore IDE0130 // Namespace does not match folder structure

//
// Summary:
//     Indicates that a parameter captures the expression passed for another parameter
//     as a string.
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class CallerArgumentExpressionAttribute : Attribute
{
    //
    // Summary:
    //     Initializes a new instance of the System.Runtime.CompilerServices.CallerArgumentExpressionAttribute
    //     class.
    //
    // Parameters:
    //   parameterName:
    //     The name of the parameter whose expression should be captured as a string.
    public CallerArgumentExpressionAttribute(string parameterName) => ParameterName = parameterName;

    //
    // Summary:
    //     Gets the name of the parameter whose expression should be captured as a string.
    //
    //
    // Returns:
    //     The name of the parameter whose expression should be captured.
    public string ParameterName { get; }
}




#endif