using DTZero;

// Increase hint name length limit to accommodate long namespace.class names in tests.
// The naming convention {Operation}Target_{ReturnType}_{ParameterVariation} creates
// long type names within the DTZeroTests.UnitTests.TestTargets namespace.
[assembly: FactoryHintNameLength(120)]
