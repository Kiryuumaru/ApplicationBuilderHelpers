using System.Runtime.CompilerServices;

// Test-only visibility: the CLI unit-test assembly reads internals to observe the reflection cache and run gates.
// No production behavior flows through this grant.
[assembly: InternalsVisibleTo("ApplicationBuilderHelpers.Test.Cli.UnitTest")]
