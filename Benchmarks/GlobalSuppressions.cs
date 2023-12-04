// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Benchmarks needs to be instance methods", Scope = "namespaceanddescendants", Target = "~N:Benchmarks")]
[assembly: SuppressMessage("Style", "IDE0290:Use primary constructor", Justification = "not improving readability", Scope = "namespaceanddescendants", Target = "~N:Benchmarks")]

[assembly: SuppressMessage("Style", "IDE0300:Simplify collection initialization", Justification = "Benchmark project. Changes needs to be intentional", Scope = "namespaceanddescendants", Target = "~N:Benchmarks")]
[assembly: SuppressMessage("Style", "IDE0251:Make member 'readonly'", Justification = "Benchmark project. Changes needs to be intentional", Scope = "namespaceanddescendants", Target = "~N:Benchmarks")]
[assembly: SuppressMessage("Performance", "CA1829:Use Length/Count property instead of Count() when available", Justification = "Benchmark project. Changes needs to be intentional", Scope = "namespaceanddescendants", Target = "~N:Benchmarks")]
[assembly: SuppressMessage("GeneratedRegex", "SYSLIB1045:Convert to 'GeneratedRegexAttribute'.", Justification = "Benchmark project. Changes needs to be intentional", Scope = "namespaceanddescendants", Target = "~N:Benchmarks")]
[assembly: SuppressMessage("Performance", "CA1827:Do not use Count() or LongCount() when Any() can be used", Justification = "Benchmark project. Changes needs to be intentional", Scope = "namespaceanddescendants", Target = "~N:Benchmarks")]
