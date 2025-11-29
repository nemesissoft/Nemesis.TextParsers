#if NETSTANDARD2_0

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System;
#pragma warning restore IDE0130 // Namespace does not match folder structure

static class PolyfillExtensions
{
    extension(ArgumentNullException)
    {
        public static void ThrowIfNull(object argument, [CallerArgumentExpression(nameof(argument))] string paramName = null)
        {
            if (argument is null)
                throw new ArgumentNullException(paramName);
        }
    }
}

#endif