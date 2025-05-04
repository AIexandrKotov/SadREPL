using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using SadConsole;
using SadConsole.Configuration;
using SadConsole.Input;
using SadConsole.Readers;
using SadConsole.UI;
using SadConsole.UI.Controls;
using SadRogue.Primitives;
using SLThree;
using SLThree.Extensions;
using SLThree.Language;
using SLThree.sys;
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
                try
                {
                    LastExecutable = Parser.ParseScript(text);
                }
                catch
                {
                    LastExecutable = Parser.ParseExpression(text);
                }
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
        public VariablesConsole Variables;
        public TreeConsole Tree;
        public SLThreeEnvironment Environment = new();

        public MainScreen()
        {
            Input = new InputConsole(this);
            Output = new OutputConsole(this);
            Variables = new VariablesConsole(this);
            Tree = new TreeConsole(this);

            Children.Add(Input);
            Children.Add(Output);
            Children.Add(Variables);
            Children.Add(Tree);
        }

        public void TextInputed()
        {
            Environment.Input(Input.MainTextBox.Text);

            Input.MainTextBox.Text = "";
            Input.MainTextBox.CaretPosition = 0;

            UpdateOutput();
            UpdateVariables();
            UpdateTree();
        }

        public void UpdateOutput()
        {

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

        public void UpdateVariables()
        {
            Variables.Clear();
            Variables.Cursor.Move(0, 0);

            var variables = Environment.REPLContext.LocalVariables.GetAsDictionary().Take(16).ToArray();

            var i = 0;

            foreach (var variable in variables)
            {
                Variables.Cursor.Move(0, i);
                Variables.Cursor.SetPrintAppearance(Color.AnsiCyan).Print(variable.Value?.GetType().GetTypeString() ?? "?");
                Variables.Cursor.SetPrintAppearance(Color.Yellow).Print(" " + variable.Key);
                Variables.Cursor.SetPrintAppearance(Color.White).Print(" =");
                Variables.Cursor.SetPrintAppearance(Color.Cyan).Print(" " + (variable.Value?.ToString() ?? "null"));
                i += 1;
            }
        }

        public void UpdateTree()
        {
            if (Environment.LastExecutable == null)
                return;

            Tree.Clear();
            Tree.Cursor.Move(0, 0);

            var xml = slt.repr(Environment.LastExecutable);

            var i = 0;

            foreach (var line in xml.Split(System.Environment.NewLine))
            {
                var ind = Array.FindIndex(line.ToCharArray(), x => !char.IsWhiteSpace(x));
                Tree.Cursor.Move(ind, i);
                Tree.Cursor.Print(line);
                i += 1;
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
            Surface.UsePrintProcessor = true;
            IsVisible = true;
            Cursor.PrintAppearanceMatchesHost = false;
        }
    }

    public class VariablesConsole : Console
    {
        public readonly MainScreen Main;

        public VariablesConsole(MainScreen main) : base(44, 14)
        {
            Main = main;
            Position = (1, 17);
            Surface.DefaultBackground = Color.AnsiCyan;
        }
    }

    public class TreeConsole : Console
    {
        public readonly MainScreen Main;

        public TreeConsole(MainScreen main) : base(43, 14)
        {
            Main = main;
            Position = (46, 17);
            Surface.DefaultBackground = Color.AnsiCyan;
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
#if DEBUG
                .EnableImGuiDebugger(Keys.F12)
#endif
                .OnStart(Startup)
                ;

            Game.Create(configuration);
            Game.Instance.Run();
            Game.Instance.Dispose();
        }
    }
}