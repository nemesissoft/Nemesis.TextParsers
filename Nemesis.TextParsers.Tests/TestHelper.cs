using System;
using System.Reflection;
using NUnit.Framework;

namespace Nemesis.TextParsers.Tests
{
    class TestHelper
    {
        public static void AssertException(Exception actual, Type expectedException, string expectedErrorMessagePart)
        {
            if (actual is TargetInvocationException tie && tie.InnerException is Exception inner)
                actual = inner;

            Assert.That(actual, Is.TypeOf(expectedException));
            Assert.That(actual?.Message, Does.Contain(expectedErrorMessagePart));

            if (actual is OverflowException oe)
                Console.WriteLine("Expected overflow: " + oe.Message);
            else if (actual is FormatException fe)
                Console.WriteLine("Expected bad format: " + fe.Message);
            else if (actual is ArgumentException ae)
                Console.WriteLine("Expected argument exception: " + ae.Message);
            else if (actual is InvalidOperationException ioe)
                Console.WriteLine("Expected invalid operation: " + ioe.Message);
            else
                Console.WriteLine("Expected other: " + actual?.Message);

        }
    }
}
