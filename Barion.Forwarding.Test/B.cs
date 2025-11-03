namespace Barion.Forwarding.Test;

internal sealed class B
{
    public required string Bar { get; set; }

    public string Foo()
    {
        return Bar.ToUpper();
    }

    public void Foo<T>(T val)
    {

    }

    public override string ToString()
    {
        return Bar;
    }
}
