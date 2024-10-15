using Microsoft.Xna.Framework;
using Serilog;
using Serilog.Configuration;
using Serilog.Formatting;

namespace RunnethOverStudio.SerilogSinksMonoGameGL;

public static class MonoGameSinkExtensions
{
    /// <summary>
    /// Adds a MonoGame sink to the Serilog logger configuration.
    /// </summary>
    /// <param name="loggerConfiguration">The logger sink configuration.</param>
    /// <param name="game">The MonoGame <see cref="Game"/> instance.</param>
    /// <param name="textFormatter">The text formatter to use for log messages. If null, a default formatter will be used.</param>
    /// <param name="maxBatchSize">The maximum number of log messages to be drawn to the view.</param>
    /// <returns>The logger configuration, allowing further configuration to be chained.</returns>
    public static LoggerConfiguration MonoGameSink(this LoggerSinkConfiguration loggerConfiguration, Game game, ITextFormatter? textFormatter = null, int maxBatchSize = 4)
    {
        return loggerConfiguration.Sink(new MonoGameSink(game, textFormatter, maxBatchSize));
    }
}
