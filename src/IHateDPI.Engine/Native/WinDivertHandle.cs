using Microsoft.Win32.SafeHandles;

namespace IHateDPI.Engine.Native;

public sealed class WinDivertHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public WinDivertHandle() : base(true) { }

    protected override bool ReleaseHandle()
    {
        return NativeMethods.WinDivertClose(handle);
    }
}