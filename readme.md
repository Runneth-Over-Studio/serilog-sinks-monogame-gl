<p align="left">
  <img src="icon.png" width="100" alt="Package Icon">
</p>

# serilog-sinks-monogame-gl

A Serilog sink that writes events to a MonoGame DrawableGameComponent as well as System.Diagnotics.Debug listeners. Provides an overlay of the log events that can be toggled on and off using the tilde key.

### Usage
Configure Serilog like you normally would, just choosing the MonoGameSink to write to:

```
  Serilog.Log.Logger = new Serilog.LoggerConfiguration()
      .MinimumLevel.Debug()
      .WriteTo.MonoGameSink(this)
      .CreateLogger();
  
  return new SerilogLoggerFactory(Serilog.Log.Logger)
      .CreateLogger(nameof(Game1));
```

### Release
For a quick & simple implementation, the latest release is available in the [NuGet Gallery](https://www.nuget.org/packages/RunnethOverStudio.SerilogSinksMonoGameGL/). However, the source code is pretty straightforward and could be copied into your own solution and easily modified to meet your specific requirements.
