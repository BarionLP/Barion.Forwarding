namespace Barion.Forwarding.Test;

[Forwarding]
public partial class A1
{
    [Forward] private B b = default!;
}

[Forwarding]
public partial class A2
{
    [Forward(nameof(B.Foo), nameof(B.Bar))] private B b = default!;
}

[Forwarding]
public partial class A3
{
    [ForwardMethods(nameof(B.Foo), nameof(B.ToString)), ForwardProperties(true)] private B b = default!;

    [Forward(nameof(string.Trim), nameof(string.Length))] private static string c = default!;
}