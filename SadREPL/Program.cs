using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using SadConsole;
using SadConsole.Configuration;
using SadConsole.Input;
using SadConsole.Readers;
using SadConsole.UI;
using SadConsole.UI.Controls;
using SadRogue.Primitives;
using SLThree;
using SLThree.Language;
using static SLThree.ExecutionContext;
using Console = SadConsole.Console;

namespace SadREPL
{
    public class SLThreeEnvironment
    {
        public Parser Parser { get; set; } = new Parser();
        public ExecutionContext REPLContext { get; set; } = new ExecutionContext();
        public IExecutable? LastExecutable { get; set; }
        public Exception? LastError { get; set; }
        public object? Output { get; set; }

        public void Input(string text)
        {
            try
            {
                LastError = null!;
                LastExecutable = Parser.ParseScript(text);
                Output = LastExecutable.GetValue(REPLContext);
            }
            catch (Exception ex)
            {
                LastError = ex;
            }
        }
    }

    public class MainScreen : ScreenObject
    {
        public InputConsole Input;
        public OutputConsole Output;
        public SLThreeEnvironment Environment = new();

        public MainScreen()
        {
            Input = new InputConsole(this);
            Output = new OutputConsole(this);

            var variablesConsole = new Console(25, 15);
            variablesConsole.Position = (1, 16);
            variablesConsole.Surface.DefaultBackground = Color.AnsiCyan;

            var xmlTreeConsole = new Console(36, 15);
            xmlTreeConsole.Position = (27, 16);
            xmlTreeConsole.Surface.DefaultBackground = Color.AnsiCyan;

            var info2Console = new Console(25, 15);
            info2Console.Position = (64, 16);
            info2Console.Surface.DefaultBackground = Color.AnsiCyan;

            Children.Add(Input);
            Children.Add(Output);
            Children.Add(variablesConsole);
            Children.Add(xmlTreeConsole);
            Children.Add(info2Console);
        }

        public void TextInputed()
        {
            Environment.Input(Input.MainTextBox.Text);

            Input.MainTextBox.Text = "";
            Input.MainTextBox.CaretPosition = 0;

            Output.Clear();
            Output.Cursor.Move(0, 0);

            if (Environment.LastError != null)
            {
                Output.Cursor.SetPrintAppearance(Color.Red).Print(Environment.LastError.ToString());
            }
            else
            {
                Output.Cursor.SetPrintAppearance(Color.White).Print(Environment.Output?.ToString() ?? "null");
            }
        }
    }

    public class InputConsole : ControlsConsole
    {
        public readonly MainScreen Main;
        public readonly TextBox MainTextBox;

        public InputConsole(MainScreen mainScreen) : base(88, 3)
        {
            Main = mainScreen;
            Position = (1, 1);
            Surface.DefaultBackground = Color.AnsiCyan;
            IsVisible = true;

            MainTextBox = new TextBox(Width);
            MainTextBox.IsFocused = true;
            MainTextBox.IsEnabled = true;
            MainTextBox.UseKeyboard = true;
            Controls.Add(MainTextBox);
        }

        public override bool ProcessKeyboard(Keyboard keyboard)
        {
            var handled = false;

            if (keyboard.IsKeyPressed(Keys.Enter))
            {
                Main.TextInputed();
                handled = true;
            }

            return base.ProcessKeyboard(keyboard);
        }
    }

    public class OutputConsole : Console
    {
        public readonly MainScreen Main;

        public OutputConsole(MainScreen mainScreen) : base(88, 12)
        {
            Main = mainScreen;
            Position = (1, 3);
            Surface.DefaultBackground = Color.AnsiCyan;
            Surface.UsePrintProcessor = true;
            IsVisible = true;
        }
    }

    class Program
    {
        static void Startup(object? sender, GameHost host)
        {
            Game.Instance.Screen = new MainScreen();
        }

        static void Main(string[] args)
        {
            Settings.WindowTitle = "SLThree SadREPL";

            Builder configuration = new Builder()
                .SetScreenSize(90, 32)
                .UseDefaultConsole()
                .OnStart(Startup)
                ;

            Game.Create(configuration);
            Game.Instance.Run();
            Game.Instance.Dispose();
        }
    }
}