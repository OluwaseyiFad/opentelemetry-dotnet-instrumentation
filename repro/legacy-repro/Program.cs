using System.Diagnostics;

// Legacy activity: created WITHOUT an ActivitySource (the old new Activity(...) style).
// The OpenTelemetry SDK only collects this when its operation name is registered via
// AddLegacySource, which is driven by OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_LEGACY_SOURCES.
using var activity = new Activity("ManualSpan");
activity.Start();
activity.Stop();

Console.WriteLine($"[app] emitted legacy activity '{activity.OperationName}', recorded={activity.Recorded}, id={activity.Id}");
