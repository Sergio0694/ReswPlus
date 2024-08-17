using ReswPlus.Core.ClassGenerator.Models;
using ReswPlus.Core.ResourceParser;
using System.Collections.Generic;
using System.Linq;

namespace ReswPlus.Core.CodeGenerators
{
    /// <summary>
    /// Generates C# code for strongly-typed resources based on localization files.
    /// </summary>
    public class CSharpCodeGenerator : ICodeGenerator
    {
        /// <summary>
        /// Generates the C# files based on the provided strongly-typed class information and resource file information.
        /// </summary>
        /// <param name="baseFilename">The base filename to be used for the generated files.</param>
        /// <param name="info">The strongly-typed class information used for generating code.</param>
        /// <param name="resourceFileInfo">The resource file information used to generate the C# files.</param>
        /// <returns>A collection of generated files.</returns>
        public IEnumerable<GeneratedFile> GetGeneratedFiles(string baseFilename, StronglyTypedClass info, ResourceInfo.ResourceFileInfo resourceFileInfo)
        {
            var builder = new CodeStringBuilder(resourceFileInfo.Project.GetIndentString());
            GenerateHeaders(builder, info.IsAdvanced);
            AddNewLine(builder);
            OpenNamespace(builder, info.Namespaces);
            OpenStronglyTypedClass(builder, info.ResoureFile, info.ClassName);

            foreach (var item in info.Localizations)
            {
                AddNewLine(builder);

                OpenRegion(builder, item.Key);

                CreateFormatMethod(
                    builder,
                    item.Key,
                    item.IsProperty,
                    item.Parameters,
                    item.Summary,
                    item.ExtraParameters,
                    (item as PluralLocalization)?.ParameterToUseForPluralization,
                    (item as PluralLocalization)?.SupportNoneState ?? false,
                    (item as IVariantLocalization)?.ParameterToUseForVariant);

                CloseRegion(builder, item.Key);
            }

            CloseStronglyTypedClass(builder);
            AddNewLine(builder);
            CreateMarkupExtension(builder, info.ResoureFile, info.ClassName + "Extension", info.Localizations.Where(i => i is Localization).Select(s => s.Key));
            CloseNamespace(builder, info.Namespaces);
            return GetGeneratedFiles(builder, baseFilename);
        }

        /// <summary>
        /// Converts a parameter type to its corresponding C# type string.
        /// </summary>
        /// <param name="type">The parameter type.</param>
        /// <returns>The corresponding C# type string.</returns>
        private string GetParameterTypeString(ParameterType type) => type switch
        {
            ParameterType.Byte => "byte",
            ParameterType.Int => "int",
            ParameterType.Uint => "uint",
            ParameterType.Long => "long",
            ParameterType.String => "string",
            ParameterType.Double => "double",
            ParameterType.Char => "char",
            ParameterType.Ulong => "ulong",
            ParameterType.Decimal => "decimal",
            _ => "object",
        };

        /// <summary>
        /// Creates the generated file object for the C# code.
        /// </summary>
        /// <param name="builder">The code string builder containing the generated C# code.</param>
        /// <param name="baseFilename">The base filename for the generated file.</param>
        /// <returns>An enumerable containing the generated file.</returns>
        private IEnumerable<GeneratedFile> GetGeneratedFiles(CodeStringBuilder builder, string baseFilename)
        {
            yield return new GeneratedFile() { Filename = baseFilename + ".cs", Content = builder.GetString() };
        }

        /// <summary>
        /// Generates the headers for the C# file, including necessary using statements and comments.
        /// </summary>
        /// <param name="builder">The code string builder to append the headers to.</param>
        /// <param name="supportPluralization">Indicates whether pluralization support is needed.</param>
        private void GenerateHeaders(CodeStringBuilder builder, bool supportPluralization)
        {
            builder.AppendLine("// File generated automatically by ReswPlus. https://github.com/DotNetPlus/ReswPlus");
            if (supportPluralization)
            {
                builder.AppendLine("// The NuGet package ReswPlusLib is necessary to support Pluralization.");
            }
            builder.AppendLine("using System;")
                .AppendLine("using Windows.ApplicationModel.Resources;")
                .AppendLine("using Windows.UI.Xaml.Markup;")
                .AppendLine("using Windows.UI.Xaml.Data;");
        }

        /// <summary>
        /// Opens the namespace block in the generated C# code.
        /// </summary>
        /// <param name="builder">The code string builder to append the namespace block to.</param>
        /// <param name="namespaces">The collection of namespaces for the generated class.</param>
        private void OpenNamespace(CodeStringBuilder builder, IEnumerable<string> namespaces)
        {
            if (namespaces != null && namespaces.Any())
            {
                builder.AppendLine($"namespace {namespaces.Aggregate((a, b) => a + "." + b)}{{");
                builder.AddLevel();
            }
        }

        /// <summary>
        /// Closes the namespace block in the generated C# code.
        /// </summary>
        /// <param name="builder">The code string builder to append the closing namespace block to.</param>
        /// <param name="namespaces">The collection of namespaces for the generated class.</param>
        private void CloseNamespace(CodeStringBuilder builder, IEnumerable<string> namespaces)
        {
            if (namespaces != null && namespaces.Any())
            {
                builder.RemoveLevel();
                builder.AppendLine($"}} //{namespaces.Aggregate((a, b) => a + "." + b)}");
            }
        }

        /// <summary>
        /// Opens the strongly-typed class block in the generated C# code.
        /// </summary>
        /// <param name="builder">The code string builder to append the class block to.</param>
        /// <param name="resourceFilename">The resource file name associated with the strongly-typed class.</param>
        /// <param name="className">The name of the strongly-typed class.</param>
        private void OpenStronglyTypedClass(CodeStringBuilder builder, string resourceFilename, string className)
        {
            builder.AppendLine($"[global::System.CodeDom.Compiler.GeneratedCodeAttribute(\"{Constants.ReswPlusName}\", \"{Constants.ReswPlusExtensionVersion}\")]")
                .AppendLine("[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]")
                .AppendLine("[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]")
                .AppendLine($"public static class {className} {{")
                .AddLevel()
                .AppendLine("private static ResourceLoader _resourceLoader;")
                .AppendLine($"static {className}()")
                .AppendLine("{")
                .AddLevel()
                .AppendLine($"_resourceLoader = ResourceLoader.GetForViewIndependentUse(\"{resourceFilename}\");")
                .RemoveLevel()
                .AppendLine("}");
        }

        /// <summary>
        /// Closes the strongly-typed class block in the generated C# code.
        /// </summary>
        /// <param name="builder">The code string builder to append the closing class block to.</param>
        private void CloseStronglyTypedClass(CodeStringBuilder builder)
        {
            builder.RemoveLevel()
                .AppendLine("}");
        }

        /// <summary>
        /// Opens a region block in the generated C# code.
        /// </summary>
        /// <param name="builder">The code string builder to append the region block to.</param>
        /// <param name="name">The name of the region.</param>
        private void OpenRegion(CodeStringBuilder builder, string name) => builder.AppendLine($"#region {name}");

        /// <summary>
        /// Closes a region block in the generated C# code.
        /// </summary>
        /// <param name="builder">The code string builder to append the closing region block to.</param>
        /// <param name="name">The name of the region.</param>
        private void CloseRegion(CodeStringBuilder builder, string name) => builder.AppendLine($"#endregion");

        /// <summary>
        /// Creates a format method for localization keys, including handling pluralization and variants if applicable.
        /// </summary>
        /// <param name="builder">The code string builder to append the format method to.</param>
        /// <param name="key">The localization key.</param>
        /// <param name="isProperty">Indicates whether the key should be treated as a property.</param>
        /// <param name="parameters">The parameters for the format method.</param>
        /// <param name="summary">The summary documentation for the method.</param>
        /// <param name="extraParameters">Extra parameters for the format method.</param>
        /// <param name="parameterForPluralization">The parameter to be used for pluralization.</param>
        /// <param name="supportNoneState">Indicates whether the "none" state is supported in pluralization.</param>
        /// <param name="parameterForVariant">The parameter to be used for variants.</param>
        private void CreateFormatMethod(CodeStringBuilder builder, string key, bool isProperty, IEnumerable<IFormatTagParameter> parameters, string summary = null, IEnumerable<FunctionFormatTagParameter> extraParameters = null, FunctionFormatTagParameter parameterForPluralization = null, bool supportNoneState = false, FunctionFormatTagParameter parameterForVariant = null)
        {
            builder.AppendLine("/// <summary>")
                .AppendLine($"///   {summary}")
                .AppendLine("/// </summary>");

            if (isProperty)
            {
                builder.AppendLine($"public static string {key}")
                    .AppendLine("{")
                    .AddLevel()
                    .AppendLine("get");
            }
            else
            {
                var functionParameters = parameters != null
                    ? parameters.OfType<FunctionFormatTagParameter>().ToList()
                    : new List<FunctionFormatTagParameter>();
                if (extraParameters != null && extraParameters.Any())
                {
                    functionParameters.InsertRange(0, extraParameters);
                }

                if (parameters.Any(p => p is FunctionFormatTagParameter functionParam && functionParam.IsVariantId) || extraParameters.Any(p => p.IsVariantId))
                {
                    // one of the parameters is a variantId, we must create a second method with object as the variantId type.
                    var genericParametersStr = functionParameters.Select(p => (p.IsVariantId ? "object" : GetParameterTypeString(p.Type)) + " " + p.Name).Aggregate((a, b) => a + ", " + b);
                    builder.AppendLine($"public static string {key}({genericParametersStr})")
                        .AppendLine("{")
                        .AddLevel()
                        .AppendLine("try")
                        .AppendLine("{")
                        .AddLevel()
                        .AppendLine($"return {key}({functionParameters.Select(p => p.IsVariantId ? $"Convert.ToInt64({p.Name})" : p.Name).Aggregate((a, b) => a + ", " + b)});")
                        .RemoveLevel()
                        .AppendLine("}")
                        .AppendLine("catch")
                        .AppendLine("{")
                        .AddLevel()
                        .AppendLine("return \"\";")
                        .RemoveLevel()
                        .AppendLine("}")
                        .RemoveLevel()
                        .AppendLine("}")
                        .AppendEmptyLine()
                        .AppendLine("/// <summary>")
                        .AppendLine($"///   {summary}")
                        .AppendLine("/// </summary>");
                }

                var parametersStr = functionParameters.Select(p => GetParameterTypeString(p.Type) + " " + p.Name).Aggregate((a, b) => a + ", " + b);
                builder.AppendLine($"public static string {key}({parametersStr})");
            }
            builder.AppendLine("{");
            builder.AddLevel();

            string keyToUseStr = $"\"{key}\"";
            if (parameterForVariant != null)
            {
                keyToUseStr = $"\"{key}_Variant\" + {parameterForVariant.Name}";
            }

            string localizationStr;
            if (parameterForPluralization != null)
            {
                var pluralNumber = parameterForPluralization.TypeToCast.HasValue ? $"({GetParameterTypeString(parameterForPluralization.TypeToCast.Value)}){parameterForPluralization.Name}" : parameterForPluralization.Name;

                var supportNoneStateStr = supportNoneState ? "true" : "false";
                localizationStr = $"ReswPlusLib.ResourceLoaderExtension.GetPlural(_resourceLoader, {keyToUseStr}, {pluralNumber}, {supportNoneStateStr})";

            }
            else
            {
                localizationStr = $"_resourceLoader.GetString({keyToUseStr})";
            }

            if (parameters != null && parameters.Any())
            {
                var formatParameters = parameters.Select(p => p switch
                {
                    FunctionFormatTagParameter functionParam => functionParam.Name,
                    MacroFormatTagParameter macroParam => $"ReswPlusLib.Macros.{macroParam.Id}",
                    LiteralStringFormatTagParameter constStringParameter => $"\"{constStringParameter.Value}\"",
                    StringRefFormatTagParameter localizationStringParameter => localizationStringParameter.Id,
                    _ => "",//should not happen
                }).Aggregate((a, b) => a + ", " + b);
                builder.AppendLine($"return string.Format({localizationStr}, {formatParameters});");
            }
            else
            {
                builder.AppendLine($"return {localizationStr};");
            }

            if (isProperty)
            {
                builder.RemoveLevel()
                    .AppendLine("}");
            }
            builder.RemoveLevel()
                .AppendLine("}");

        }

        /// <summary>
        /// Creates a markup extension class for the strongly-typed resource class.
        /// </summary>
        /// <param name="builder">The code string builder to append the markup extension class to.</param>
        /// <param name="resourceFileName">The name of the resource file associated with the class.</param>
        /// <param name="className">The name of the markup extension class.</param>
        /// <param name="keys">The collection of keys for the strongly-typed resource class.</param>
        private void CreateMarkupExtension(CodeStringBuilder builder, string resourceFileName, string className, IEnumerable<string> keys)
        {
            builder.AppendLine($"[global::System.CodeDom.Compiler.GeneratedCodeAttribute(\"{Constants.ReswPlusName}\", \"{Constants.ReswPlusExtensionVersion}\")]")
                .AppendLine("[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]")
                .AppendLine("[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]")
                .AppendLine("[MarkupExtensionReturnType(ReturnType = typeof(string))]")
                .AppendLine($"public class {className} : MarkupExtension")
                .AppendLine("{")
                .AddLevel()
                .AppendLine("public enum KeyEnum")
                .AppendLine("{")
                .AddLevel()
                .AppendLine("__Undefined = 0,");

            foreach (var key in keys)
            {
                builder.AppendLine($"{key},");
            }
            builder.RemoveLevel()
                .AppendLine("}")
                .AppendEmptyLine()
                .AppendLine("private static ResourceLoader _resourceLoader;")
                .AppendLine($"static {className}()")
                .AppendLine("{")
                .AddLevel()
                .AppendLine($"_resourceLoader = ResourceLoader.GetForViewIndependentUse(\"{resourceFileName}\");")
                .RemoveLevel()
                .AppendLine("}")
                .AppendLine("public KeyEnum Key { get; set;}")
                .AppendLine("public IValueConverter Converter { get; set;}")
                .AppendLine("public object ConverterParameter { get; set;}")
                .AppendLine("private  object ProvideValue()")
                .AppendLine("{")
                .AddLevel()
                .AppendLine("string res;")
                .AppendLine("if(Key == KeyEnum.__Undefined)")
                .AppendLine("{")
                .AddLevel()
                .AppendLine("res = \"\";")
                .RemoveLevel()
                .AppendLine("}")
                .AppendLine("else")
                .AppendLine("{")
                .AddLevel()
                .AppendLine("res = _resourceLoader.GetString(Key.ToString());")
                .RemoveLevel()
                .AppendLine("}")
                .AppendLine("return Converter == null ? res : Converter.Convert(res, typeof(String), ConverterParameter, null);")
                .RemoveLevel()
                .AppendLine("}")
                .RemoveLevel()
                .AppendLine("}");
        }

        /// <summary>
        /// Adds an empty line to the generated C# code.
        /// </summary>
        /// <param name="builder">The code string builder to append the empty line to.</param>
        private void AddNewLine(CodeStringBuilder builder) => builder.AppendEmptyLine();
    }
}
