using System.Diagnostics;

namespace Edgamat.Messaging.Samples.Host;

public static class DiagnosticsConfig
{
    public const string ServiceName = "Edgamat.Messaging.Samples.Host";

    public static ActivitySource Source { get; } = new(ServiceName);
}

