namespace Barion.Forwarding.Test;

[Forwarding]
internal sealed partial class ReadonlyB
{
    [Forward]
    private B _Inner = default!;
}
