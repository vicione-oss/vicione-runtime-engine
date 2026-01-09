using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using ViciOne.Core.Contracts;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime.Execution;
using Executor = ViciOne.Core.Runtime.Execution.FunctionBlockExecutor;

namespace FunctionBlockExecutor;

internal class Program
{
    private static readonly Lock s_consoleLocker = new();
    private static Executor? s_executor;
    private static ConsoleState s_state = ConsoleState.Show;

    public static void Main(string[] args)
    {
        string fbAssemblyPath = args[0];
        RelevantAssemblies assemblies = AssemblyLoader.CreateRelevantAssemblies(fbAssemblyPath);
        DataflowInfo dataflowInfo = DataflowGenerator.GenerateDataflow(assemblies);
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole(options => options.SingleLine = true));
        using (s_executor = new Executor(dataflowInfo, assemblies.AllAssemblies, loggerFactory))
        {
            s_executor.InputValuesChanged += (_, __) => Show();
            s_executor.OutputValuesChanged += (_, __) => Show();
            s_executor.PersistentVariableValuesChanged += (_, __) => Show();
            s_executor.SettingValuesChanged += (_, __) => Show();
            s_executor.CycleFinished += (_, __) => Show();

            s_executor.StartEngine();

            Show();

            while (s_executor.EngineState != EngineState.Stopped)
                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.Enter:
                        s_executor.ProcessCycle();
                        break;

                    case ConsoleKey.I when s_state == ConsoleState.Show:
                        s_state = ConsoleState.ChangeInputValue;
                        Show();
                        break;

                    case ConsoleKey.S when s_state == ConsoleState.Show:
                        s_state = ConsoleState.ChangeSettingValue;
                        Show();
                        break;

                    case ConsoleKey.P when s_state == ConsoleState.Show:
                        s_state = ConsoleState.ChangePersistentVariableValue;
                        Show();
                        break;

                    case ConsoleKey.Q:
                        s_executor.StopEngine();
                        break;
                }
        }
    }

    private static void Show()
    {
        lock (s_consoleLocker)
        {
            Console.Clear();
            switch (s_state)
            {
                case ConsoleState.Show:
                    Console.WriteLine("Press (Enter) to process next engine cycle.");
                    Console.WriteLine("Press (I) to change an input connector's value.");
                    Console.WriteLine("Press (S) to change a setting's value.");
                    Console.WriteLine("Press (P) to change a persistent variable's value.");
                    Console.WriteLine("Press (Q) to quit.");
                    Console.WriteLine();
                    Console.WriteLine($"Cycle no: {s_executor!.CycleNo}");
                    Console.WriteLine();
                    Console.WriteLine("Settings:");
                    foreach (KeyValuePair<ISetting, ChangedEntry> pair in s_executor.GetSettingValues())
                        Console.WriteLine($"- {pair.Key.Design.Name}: {pair.Value.Value}");
                    Console.WriteLine("Inputs:");
                    foreach (KeyValuePair<IConnector, ChangedEntry> pair in s_executor.GetInputValues())
                        Console.WriteLine($"- {pair.Key.Design.Name}: {pair.Value.Value}");
                    Console.WriteLine("Outputs:");
                    foreach (KeyValuePair<IConnector, ChangedEntry> pair in s_executor.GetOutputValues())
                        Console.WriteLine($"- {pair.Key.Design.Name}: {pair.Value.Value}");
                    break;

                case ConsoleState.ChangeInputValue:
                    AskForNewValueAndCallChangeValueForInput();
                    s_state = ConsoleState.Show;
                    Show();
                    break;

                case ConsoleState.ChangePersistentVariableValue:
                    AskForNewValueAndCallChangeValueForPersistentVariable();
                    s_state = ConsoleState.Show;
                    Show();
                    break;

                case ConsoleState.ChangeSettingValue:
                    AskForNewValueAndCallChangeValueForSetting();
                    s_state = ConsoleState.Show;
                    Show();
                    break;
            }
        }
    }

    private static void AskForNewValueAndCallChangeValue<TElement>(string elementType, Func<IEnumerable<TElement>> selectElements, Func<TElement, string> selectElementName, Action<TElement, object> changeElementValue)
    {
        Console.Write($"Enter number of {elementType}: ");
        (int cursorLeft, int cursorTop) = (Console.CursorLeft, Console.CursorTop);
        Console.WriteLine();
        TElement[] elements = [.. selectElements()];
        for (int i = 0; i < elements.Length; i++)
            Console.WriteLine($"({i}) {selectElementName(elements[i])}");
        Console.SetCursorPosition(cursorLeft, cursorTop);
        int elementNumber = int.Parse(Console.ReadLine() ?? "0", CultureInfo.InvariantCulture);
        TElement? element = elements[elementNumber];
        Console.Clear();
        Console.Write($"Enter new value of {elementType} {selectElementName(element)}: ");
        string? newValueText = Console.ReadLine();
        if (string.IsNullOrEmpty(newValueText) == false)
        {
            Type targetType = DataTypeCompatibilityValidator.DetermineValueType(element!);
            object newValue = ConvertToType(newValueText, targetType);
            changeElementValue(element, newValue);
        }
    }

    private static void AskForNewValueAndCallChangeValueForInput()
        => AskForNewValueAndCallChangeValue("input connector", s_executor!.GetInputs, x => x.Design.Name, (input, newValue) => s_executor!.ChangeInputValue(input, newValue));

    private static void AskForNewValueAndCallChangeValueForPersistentVariable()
        => AskForNewValueAndCallChangeValue("persistent variable", s_executor!.GetPersistentVariables, x => x.Design.Name, (variable, newValue) => s_executor!.ChangePersistentVariableValue(variable, newValue));

    private static void AskForNewValueAndCallChangeValueForSetting()
        => AskForNewValueAndCallChangeValue("setting", s_executor!.GetSettings, x => x.Design.Name, (setting, newValue) => s_executor!.ChangeSettingValue(setting, newValue));

    private static object ConvertToType(string value, Type targetType)
        => targetType.IsEnum
            ? Enum.Parse(targetType, value)
            : Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
}
