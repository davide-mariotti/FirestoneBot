namespace System.Runtime.CompilerServices;

// Required, not dead code: a referenced MelonLoader/Unity assembly exposes a public NullableAttribute
// without the constructors the compiler needs, so it binds to that one and fails with CS0656 on some
// lambdas (e.g. MapMissionsTask.ScanMissions). A definition in this assembly takes precedence.
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event |
    AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter, Inherited = false)]
internal sealed class NullableAttribute : Attribute
{
    public readonly byte[] NullableFlags;

    public NullableAttribute(byte flag)
    {
        NullableFlags = new[] { flag };
    }

    public NullableAttribute(byte[] flags)
    {
        NullableFlags = flags;
    }
}
