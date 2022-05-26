using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PrincipleStudios.Salesforce;

public class FormattableStringBuilder
{
    private readonly string format;
    private readonly ImmutableList<object?> arguments;

    public static readonly FormattableStringBuilder Empty = new FormattableStringBuilder();
    private static readonly ConcatFormatter concatFormatter = new ConcatFormatter();

    private FormattableStringBuilder() : this(string.Empty, ImmutableList<object?>.Empty) { }
    public FormattableStringBuilder(FormattableString initial) : this((initial ?? throw new ArgumentNullException(nameof(initial))).Format, initial.GetArguments().ToImmutableList()) { }

    private FormattableStringBuilder(string format, ImmutableList<object?> arguments)
    {
        this.format = format;
        this.arguments = arguments;
    }

    public FormattableStringBuilder Add(FormattableString next)
    {
        if (next is null)
        {
            throw new ArgumentNullException(nameof(next));
        }

        var newArgs = next.GetArguments();
        return new FormattableStringBuilder(
            format + string.Format(concatFormatter, next.Format, args: Enumerable.Range(arguments.Count, newArgs.Length).Select(i => (object?)(i)).ToArray()),
            arguments.AddRange(newArgs)
        );
    }

    public FormattableString Build() => FormattableStringFactory.Create(format, arguments.ToArray());

    public static FormattableStringBuilder From(FormattableString f) => new(f);

    private class ConcatFormatter : IFormatProvider, ICustomFormatter
    {
        public string Format(string? format, object? arg, IFormatProvider? formatProvider)
        {
            if (format == null)
                return $"{{{arg}}}";

            return $"{{{arg}:{format}}}";
        }

        public object? GetFormat(Type? formatType)
        {
            return this;
        }
    }
}
