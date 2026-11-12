using DTZero;

namespace DTZeroTests.TrimmingTests;

/// <summary>
/// Tests that the feature switch constant folding works directly.
/// If the trimmer constant-folds DTZeroRuntime.IsServerRuntime to false,
/// then the ServerOnlyDirect class should be trimmed away.
/// </summary>
public static class DirectFeatureSwitchTest
{
    public static bool Run()
    {
        if (DTZeroRuntime.IsServerRuntime)
        {
            // This code path should be dead when IsServerRuntime is constant-folded to false.
            var helper = new ServerOnlyDirect();
            Console.WriteLine(helper.Marker);
            Console.WriteLine("Feature switch FAILED: IsServerRuntime is true in the client-configured harness.");
            return false;
        }

        Console.WriteLine("Client mode - server-only code trimmed.");
        return true;
    }
}

/// <summary>
/// A type that is only referenced inside the DTZeroRuntime.IsServerRuntime guard.
/// If constant folding works, this type should be entirely removed by the trimmer.
/// The "ServerOnlyDirect_MARKER" string should be absent from the trimmed output.
/// </summary>
public class ServerOnlyDirect
{
    public string Marker => "ServerOnlyDirect_MARKER";
}
