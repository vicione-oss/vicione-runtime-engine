using System;

namespace ViciOne.Core.Dataflow.DataModel.Editors;

internal static class ExceptionFactory
{
    internal static FormatException CreateNoValidNamespaceOrClassNameException(string name)
        => new($"'{name}' is no valid namespace or class name.");
}
