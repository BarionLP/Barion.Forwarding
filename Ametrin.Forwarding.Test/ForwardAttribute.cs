namespace Ametrin.Forwarding.Test;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public sealed class Forward : Attribute {
    public Forward(params string[] memberNames) { }
}