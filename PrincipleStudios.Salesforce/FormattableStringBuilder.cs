using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace PrincipleStudios.Salesforce;

public class FormattableStringBuilder
{
    private readonly ImmutableList<string> formatParts;
    private readonly ImmutableList<object?> arguments;

    public static readonly FormattableStringBuilder Empty = new FormattableStringBuilder();
    private static readonly ConcatFormatter concatFormatter = new ConcatFormatter();

    private FormattableStringBuilder() : this(ImmutableList<string>.Empty, ImmutableList<object?>.Empty) { }
    public FormattableStringBuilder(FormattableString initial) : this(
        ImmutableList<string>.Empty.Add(initial?.Format ?? throw new ArgumentNullException(nameof(initial))),
        initial.GetArguments().ToImmutableList()
    ) { }

    private FormattableStringBuilder(ImmutableList<string> formatParts, ImmutableList<object?> arguments)
    {
        this.formatParts = formatParts;
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
            formatParts.Add(string.Format(concatFormatter, next.Format, args: Enumerable.Range(arguments.Count, newArgs.Length).Select(i => (object?)(i)).ToArray())),
            arguments.AddRange(newArgs)
        );
    }

    public FormattableString Build() => FormattableStringFactory.Create(string.Join(null, formatParts), arguments.ToArray());

    public static FormattableStringBuilder From(FormattableString f) => new(f);

    private sealed class ConcatFormatter : IFormatProvider, ICustomFormatter
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
