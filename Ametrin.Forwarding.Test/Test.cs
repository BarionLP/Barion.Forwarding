namespace Ametrin.Forwarding.Test;

[Forwarding]
public partial class Test {
    [Forward]
    private InternalTest Composited = new() { Name = "GPT" };
}

internal sealed class InternalTest {
    internal required string Name { get; set; }
    protected int Age { get; set; } = 13;

    public void Hi() {
        Console.WriteLine($"Hi from {Name}");
    }
    public void Hi(string other) {
        Console.WriteLine($"Hi {other}, it's me {Name}");
    }

    public void Hi(string other, uint ha = 8) {
        Console.WriteLine($"Hi {other}, I'm {ha}ft. taller than you");
    }
}