using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RunnethOverStudio.SerilogSinksMonoGameGL.Assets;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Channels;

namespace RunnethOverStudio.SerilogSinksMonoGameGL;

/// <summary>
/// A Serilog sink that writes events to a MonoGame DrawableGameComponent as well as <see cref="Debug"/> listeners. 
/// Provides an overlay of the log events that can be toggled on and off using the tilde key.
/// </summary>
public class MonoGameSink : DrawableGameComponent, ILogEventSink
{
    private const string DEFAULT_TEMPLATE = "{Timestamp:HH:mm:ss.fff} [{Level}] {Message}{NewLine}{Exception}";

    private readonly ITextFormatter _formatter;
    private readonly int _maxBatchSize;
    private readonly Channel<LogEvent> _channel;
    private bool _drawLogs = false;
    private bool _previousToggleDrawKeyState = true;
    private Queue<LogSpriteBatch> _logDrawBuffer;
    private SpriteFont? _font;
    private SpriteFont? _fontBold;
    private SpriteBatch? _spriteBatch;
    private Texture2D? _backgroundTexture;
    private Texture2D? _gridLineTexture;
    private Color _backgroundColor;

    // Instantiating to guarantee not-null; real values are set during Initialize().
    private Vector2 _logsLabelPosition = Vector2.Zero;
    private Vector2 _headerStartingPosition = Vector2.Zero;
    private Vector2 _logRecordsStartingPosition = Vector2.Zero;
    private Vector2 _levelColumnPos = Vector2.Zero;
    private Vector2 _messageColumnPos = Vector2.Zero;
    private Vector2 _exceptionColumnPos = Vector2.Zero;
    private Rectangle _backgroundRec = new();
    private Rectangle _horizontalGridLineRec = new();
    private Rectangle _firstVerticalGridLineRec = new();
    private Rectangle _secondVerticalGridLineRec = new();
    private Rectangle _thirdVerticalGridLineRec = new();

    public MonoGameSink(Game game, ITextFormatter? textFormatter = null, int maxBatchSize = 4) : base(game)
    {
        DrawOrder = int.MaxValue;

        _formatter = textFormatter ?? new MessageTemplateTextFormatter(DEFAULT_TEMPLATE, null);
        _maxBatchSize = maxBatchSize;
        _channel = Channel.CreateUnbounded<LogEvent>();
        _logDrawBuffer = new();
        _backgroundColor = Color.Black * 0.5F;

        game.Window.ClientSizeChanged += GameWindow_ClientSizeChanged;
        game.Components.Add(this);
    }

    public void Emit(LogEvent logEvent)
    {
        StringWriter buffer = new(new StringBuilder());
        _formatter.Format(logEvent, buffer);
        string formattedLogEventText = buffer.ToString();

        Debug.WriteLine(formattedLogEventText);

        if (!_channel.Writer.TryWrite(logEvent))
        {
            SelfLog.WriteLine($"{nameof(MonoGameSink)} failed to write to channel: {formattedLogEventText}");
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        _spriteBatch = new(GraphicsDevice);
        _font = new FreeMono().GetFont(GraphicsDevice);
        _fontBold = new FreeMonoBold().GetFont(GraphicsDevice);

        _backgroundTexture = new Texture2D(Game.GraphicsDevice, 1, 1);
        _backgroundTexture.SetData(new[] { Color.Black });

        _gridLineTexture = new Texture2D(Game.GraphicsDevice, 1, 1);
        _gridLineTexture.SetData(new[] { Color.White });

        SetViewportDependentPositions();
    }

    public override void Update(GameTime gameTime)
    {
        // Check for tilde key press and toggle _drawLogs only on key down transition.
        KeyboardState state = Keyboard.GetState();
        bool isTildeKeyDown = state.IsKeyDown(Keys.OemTilde);
        if (isTildeKeyDown && !_previousToggleDrawKeyState)
        {
            _drawLogs = !_drawLogs;
        }
        _previousToggleDrawKeyState = isTildeKeyDown;
        if (!_drawLogs)
        {
            return;
        }

        for (int i = 0; i < _maxBatchSize; i++)
        {
            if (_channel.Reader.TryRead(out LogEvent? logEvent))
            {
                _logDrawBuffer.Enqueue(new LogSpriteBatch(logEvent));
            }
            else
            {
                break;
            }
        }

        // Ensure the draw buffer does not exceed the maximum batch size, by removing the oldest entries.
        while (_logDrawBuffer.Count > _maxBatchSize)
        {
            _logDrawBuffer.Dequeue();
        }
    }

    public override void Draw(GameTime gameTime)
    {
        if (!_drawLogs)
        {
            return;
        }

        if (_spriteBatch == null)
        {
            throw new InvalidOperationException("SpriteBatch is null. Perhaps an Initialize() override was made without calling base.");
        }

        if (_font == null || _fontBold == null)
        {
            throw new InvalidOperationException("SpriteFont is null. Perhaps an Initialize() override was made without calling base.");
        }

        _spriteBatch.Begin();

        // Draw background.
        _spriteBatch.Draw(_backgroundTexture, _backgroundRec, _backgroundColor);

        // Draw vertical "Logs" label.
        Vector2 logsLabelPosition = new(_logsLabelPosition.X, _logsLabelPosition.Y);
        string logsLabel = "LOGS";
        for (int i = 0; i < logsLabel.Length; i++)
        {
            _spriteBatch.DrawString(_fontBold, logsLabel[i].ToString(), logsLabelPosition + Vector2.One, Color.Black);
            _spriteBatch.DrawString(_fontBold, logsLabel[i].ToString(), logsLabelPosition, Color.White);
            logsLabelPosition.Y += _fontBold.LineSpacing;
        }

        // Draw header.
        _spriteBatch.DrawString(_fontBold, "Timestamp", _headerStartingPosition + Vector2.One, Color.Black);
        _spriteBatch.DrawString(_fontBold, "Timestamp", _headerStartingPosition, Color.White);

        _spriteBatch.DrawString(_fontBold, "Level", _levelColumnPos + Vector2.One, Color.Black);
        _spriteBatch.DrawString(_fontBold, "Level", _levelColumnPos, Color.White);

        _spriteBatch.DrawString(_fontBold, "Message", _messageColumnPos + Vector2.One, Color.Black);
        _spriteBatch.DrawString(_fontBold, "Message", _messageColumnPos, Color.White);

        _spriteBatch.DrawString(_fontBold, "Exception", _exceptionColumnPos + Vector2.One, Color.Black);
        _spriteBatch.DrawString(_fontBold, "Exception", _exceptionColumnPos, Color.White);

        // Draw grid lines.
        _spriteBatch.Draw(_gridLineTexture, _horizontalGridLineRec, Color.White);
        _spriteBatch.Draw(_gridLineTexture, _firstVerticalGridLineRec, Color.White);
        _spriteBatch.Draw(_gridLineTexture, _secondVerticalGridLineRec, Color.White);
        _spriteBatch.Draw(_gridLineTexture, _thirdVerticalGridLineRec, Color.White);

        // Draw logs.
        Vector2 logRecordsPosition = new(_logRecordsStartingPosition.X, _logRecordsStartingPosition.Y);
        foreach (LogSpriteBatch logSpriteBatch in _logDrawBuffer.OrderByDescending(log => log.TimeStamp))
        {
            _spriteBatch.DrawString(_font, logSpriteBatch.TimeStampFormatted, logRecordsPosition + Vector2.One, Color.Black);
            _spriteBatch.DrawString(_font, logSpriteBatch.TimeStampFormatted, logRecordsPosition, Color.White);

            _spriteBatch.DrawString(_font, logSpriteBatch.Level, new Vector2(_levelColumnPos.X, logRecordsPosition.Y) + Vector2.One, Color.Black);
            _spriteBatch.DrawString(_font, logSpriteBatch.Level, new Vector2(_levelColumnPos.X, logRecordsPosition.Y), logSpriteBatch.EventLevelColor);

            _spriteBatch.DrawString(_font, logSpriteBatch.Message, new Vector2(_messageColumnPos.X, logRecordsPosition.Y) + Vector2.One, Color.Black);
            _spriteBatch.DrawString(_font, logSpriteBatch.Message, new Vector2(_messageColumnPos.X, logRecordsPosition.Y), Color.White);

            _spriteBatch.DrawString(_font, logSpriteBatch.Exception, new Vector2(_exceptionColumnPos.X, logRecordsPosition.Y) + Vector2.One, Color.Black);
            _spriteBatch.DrawString(_font, logSpriteBatch.Exception, new Vector2(_exceptionColumnPos.X, logRecordsPosition.Y), Color.White);

            logRecordsPosition.Y += _font.LineSpacing;
        }

        _spriteBatch.End();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Game.Window.ClientSizeChanged -= GameWindow_ClientSizeChanged;
            _spriteBatch?.Dispose();
            _backgroundTexture?.Dispose();
            _gridLineTexture?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void GameWindow_ClientSizeChanged(object? sender, EventArgs e)
    {
        SetViewportDependentPositions();
    }

    private void SetViewportDependentPositions()
    {
        const float boxWidthAsPercentageOfViewport = 1.00F;

        // Determine & set the position of the background.
        int backgroundHeight = _font!.LineSpacing * (_maxBatchSize + 2); // +2 for the header and its underline.
        int viewportHeight = Game.GraphicsDevice.PresentationParameters.BackBufferHeight;
        int viewportWidth = Game.GraphicsDevice.PresentationParameters.BackBufferWidth;
        int backgroundWidth = (int)(viewportWidth * boxWidthAsPercentageOfViewport);
        int rectangleBottom = viewportHeight - 10;
        int backgroundTop = rectangleBottom - backgroundHeight;
        _backgroundRec = new Rectangle(Game.GraphicsDevice.PresentationParameters.BackBufferWidth - backgroundWidth, backgroundTop, backgroundWidth, backgroundHeight);

        // Table label.
        float monoCharWidth = _font.MeasureString("W").X;
        float tableLabelWidth = monoCharWidth + 5;
        _logsLabelPosition = new(_backgroundRec.X + 10, _backgroundRec.Y + _font.LineSpacing + 10);

        // Set where header & logs will start to be written inside of the rectangle.
        _headerStartingPosition = new Vector2(viewportWidth - backgroundWidth + tableLabelWidth + 10, backgroundTop + 10);
        _logRecordsStartingPosition = new Vector2(_headerStartingPosition.X, _headerStartingPosition.Y + (_font.LineSpacing + 5));
        _levelColumnPos = new Vector2(_headerStartingPosition.X + (monoCharWidth * 13.0F), _headerStartingPosition.Y);
        _messageColumnPos = new Vector2(_headerStartingPosition.X + (monoCharWidth * 25.0F), _headerStartingPosition.Y);
        _exceptionColumnPos = new Vector2(_headerStartingPosition.X + (monoCharWidth * 87.0F), _headerStartingPosition.Y); // At 1920 resolution, gets about half the combined message area. On smaller resolutions, it's out of the way of main message.

        // Set grid lines.
        _horizontalGridLineRec = new Rectangle((int)_headerStartingPosition.X, (int)(_headerStartingPosition.Y + _font.LineSpacing), backgroundWidth - 20, 1);
        _firstVerticalGridLineRec = new Rectangle((int)_levelColumnPos.X - 5, backgroundTop + 5, 1, backgroundHeight - 10);
        _secondVerticalGridLineRec = new Rectangle((int)_messageColumnPos.X - 5, backgroundTop + 5, 1, backgroundHeight - 10);
        _thirdVerticalGridLineRec = new Rectangle((int)_exceptionColumnPos.X - 5, backgroundTop + 5, 1, backgroundHeight - 10);
    }
}
