// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "CA1069:Enums values should not be duplicated", Justification = "Foreign code", Scope = "namespaceanddescendants", Target = "~N:JetBrains.Annotations")]
[assembly: SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "Foreign code", Scope = "namespace", Target = "~N:JetBrains.Annotations")]
[assembly: SuppressMessage("Style", "IDE0057:Use range operator", Justification = "Multiple target frameworks without support for range operator", Scope = "namespaceanddescendants", Target = "~N:Nemesis.TextParsers")]
[assembly: SuppressMessage("Style", "IDE0290:Use primary constructor", Justification = "not improving readability", Scope = "namespaceanddescendants", Target = "~N:Nemesis.TextParsers")]