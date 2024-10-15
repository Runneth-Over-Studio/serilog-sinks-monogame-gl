using Microsoft.Xna.Framework;
using Serilog.Events;
using System;

namespace RunnethOverStudio.SerilogSinksMonoGameGL;

public record LogSpriteBatch
{
    public DateTime TimeStamp { get; }
    public string TimeStampFormatted { get; }
    public string Level { get; }
    public Color EventLevelColor { get; }
    public string Message { get; }
    public string Exception { get; }

    public LogSpriteBatch(LogEvent logEvent)
    {
        TimeStamp = logEvent.Timestamp.LocalDateTime;
        TimeStampFormatted = TimeStamp.ToString("HH:mm:ss.fff");

        Level = logEvent.Level.ToString();
        Message = logEvent.RenderMessage();
        Exception = logEvent.Exception?.ToString() ?? string.Empty;

        EventLevelColor = (LogEventLevel)logEvent.Level switch
        {
            LogEventLevel.Warning => Color.Yellow,
            LogEventLevel.Error or LogEventLevel.Fatal => Color.Red,
            _ => Color.White
        };
    }
}
