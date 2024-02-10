﻿using System.Linq;
using Microsoft.CodeAnalysis;

namespace Mond.SourceGenerator;

public partial class MondSourceGenerator
{
    private static void ClassBindings(GeneratorExecutionContext context, INamedTypeSymbol klass, IndentTextWriter writer)
    {
        var className = klass.GetAttributes().TryGetAttribute("MondClassAttribute", out var classAttr)
            ? classAttr.GetArgument<string>() ?? klass.Name
            : klass.Name;

        var qualifier = $"global::{klass.GetFullyQualifiedName()}";
        var constructors = GetConstructors(context, klass);
        var properties = GetProperties(context, klass, false);
        var methods = GetMethods(context, klass, false);
        var methodTables = MethodTable.Build(methods.Concat(constructors.Select(c => (c, "#ctor", "__ctor"))));

        writer.WriteLine("public sealed partial class Library : IMondLibrary");
        writer.OpenBracket();

        writer.WriteLine("public IEnumerable<KeyValuePair<string, MondValue>> GetDefinitions(MondState state)");
        writer.OpenBracket();

        writer.WriteLine("var prototype = MondValue.Object(state);");

        foreach (var (property, name) in properties)
        {
            if (property.GetMethod is { DeclaredAccessibility: Accessibility.Public })
            {
                writer.WriteLine($"prototype[\"get{name}\"] = MondValue.Function({name}__Getter);");
            }

            if (property.SetMethod is { DeclaredAccessibility: Accessibility.Public })
            {
                writer.WriteLine($"prototype[\"set{name}\"] = MondValue.Function({name}__Setter);");
            }
        }

        foreach (var table in methodTables)
        {
            writer.WriteLine($"prototype[\"{table.Identifier}\"] = MondValue.Function({table.Identifier}__Dispatch);");
        }

        writer.WriteLine("ModifyObject(prototype);");
        writer.WriteLine("prototype.Lock();");
        writer.WriteLine($"state.TryAddPrototype(\"{klass.GetFullyQualifiedName()}\", prototype);");
        writer.WriteLine();

        if (constructors.Count > 0)
        {
            writer.WriteLine($"yield return new KeyValuePair<string, MondValue>(\"{className}\", MondValue.Function(__ctor__Dispatch));");
        }

        writer.WriteLine("yield break;");
        writer.CloseBracket();

        writer.WriteLine();
        writer.WriteLine("partial void ModifyObject(MondValue obj);");

        writer.WriteLine();

        foreach (var (property, name) in properties)
        {
            if (property.GetMethod is { DeclaredAccessibility: Accessibility.Public })
            {
                writer.WriteLine($"public static MondValue {name}__Getter(MondState state, MondValue instance, params MondValue[] args)");
                writer.OpenBracket();

                Prologue($"get{name}");

                writer.WriteLine("if (args.Length != 0)");
                writer.OpenBracket();
                writer.WriteLine($"throw new MondRuntimeException(\"{className}.get{name}: expected 0 arguments\");");
                writer.CloseBracket();

                writer.WriteLine($"var value = obj.{property.Name};");
                writer.WriteLine($"return {ConvertToMondValue("value", property.Type)};");
                writer.CloseBracket();
                writer.WriteLine();
            }

            if (property.SetMethod is { DeclaredAccessibility: Accessibility.Public })
            {
                var parameter = new Parameter(property.SetMethod.Parameters[0]);

                writer.WriteLine($"public static MondValue {name}__Setter(MondState state, MondValue instance, params MondValue[] args)");
                writer.OpenBracket();

                Prologue($"set{name}");

                writer.WriteLine($"if (args.Length != 1 || !{CompareArgument(0, parameter)})");
                writer.OpenBracket();
                writer.WriteLine($"throw new MondRuntimeException(\"{className}.set{name}: expected 1 argument of type {parameter.TypeName}\");");
                writer.CloseBracket();

                writer.WriteLine($"obj.{property.Name} = {ConvertFromMondValue(0, property.Type)};");

                writer.WriteLine("return MondValue.Undefined;");
                writer.CloseBracket();
                writer.WriteLine();
            }
        }

        foreach (var table in methodTables)
        {
            var isNormalMethod = table.Name != "#ctor";

            writer.WriteLine(isNormalMethod
                ? $"public static MondValue {table.Identifier}__Dispatch(MondState state, MondValue instance, params MondValue[] args)"
                : $"public static MondValue {table.Identifier}__Dispatch(MondState state, params MondValue[] args)");
            writer.OpenBracket();

            if (isNormalMethod)
            {
                Prologue(table.Name);
            }

            writer.WriteLine("switch (args.Length)");
            writer.OpenBracket();

            for (var i = 0; i < table.Methods.Count; i++)
            {
                var tableMethods = table.Methods[i];
                if (tableMethods.Count == 0)
                {
                    continue;
                }

                writer.WriteLine($"case {i}:");
                writer.OpenBracket();
                foreach (var method in tableMethods)
                {
                    writer.WriteLine($"if ({CompareArguments(method, i)})");
                    writer.OpenBracket();
                    CallMethod(writer, "obj", method, i);
                    writer.CloseBracket();
                }
                writer.WriteLine("break;");
                writer.CloseBracket();
            }

            writer.CloseBracket();

            foreach (var method in table.ParamsMethods)
            {
                writer.WriteLine($"if (args.Length >= {method.RequiredMondParameterCount} && {CompareArguments(method)})");
                writer.OpenBracket();
                CallMethod(writer, "obj", method);
                writer.CloseBracket();
            }

            writer.WriteLine();
            var errorPrefix = $"{className}.{table.Name}: ";
            var errorMessage = GetMethodNotMatchedErrorMessage(errorPrefix, table);
            writer.WriteLine($"throw new MondRuntimeException(\"{EscapeForStringLiteral(errorMessage)}\");");

            writer.CloseBracket();
            writer.WriteLine();
        }

        writer.CloseBracket();

        return;

        void Prologue(string methodName)
        {
            writer.WriteLine($"if (instance.Type != MondValueType.Object || instance.UserData is not {qualifier} obj)");
            writer.OpenBracket();
            writer.WriteLine($"throw new MondRuntimeException(\"{className}.{methodName}: can only be called on an instance of {className}\");");
            writer.CloseBracket();
            writer.WriteLine();
        }
    }
}
