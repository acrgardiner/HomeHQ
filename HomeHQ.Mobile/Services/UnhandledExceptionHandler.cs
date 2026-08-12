using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace HomeHQ.Mobile.Services;

public static class UnhandledExceptionHandler
{
    public static void Register()
    {
        AppDomain.CurrentDomain.UnhandledException += (s, e) => Handle((Exception?)e.ExceptionObject, "AppDomain.UnhandledException");
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            Handle(e.Exception, "TaskScheduler.UnobservedTaskException");
            e.SetObserved();
        };
    }

    private static void Handle(Exception? ex, string source)
    {
        try
        {
            var correlationId = System.Diagnostics.Activity.Current?.Id ?? Guid.NewGuid().ToString();
            var message = $"[{DateTime.UtcNow:O}] {source} CorrelationId={correlationId} Exception={ex}\n";

            Debug.WriteLine(message);

            try
            {
                var dir = FileSystem.AppDataDirectory;
                var path = Path.Combine(dir, "crash.log");
                File.AppendAllText(path, message);
            }
            catch
            {
                // best-effort - swallow
            }
        }
        catch
        {
            // swallow
        }
    }
}
