using System.Diagnostics;

namespace Edgamat.Messaging;

public static class DiagnosticsConfig
{
    public const string ServiceName = "Edgamat.Messaging";

    public static ActivitySource Source { get; } = new(ServiceName);
}

