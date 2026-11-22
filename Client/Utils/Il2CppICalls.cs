using System;

namespace MiyakoCarryService.Client.Utils;

public static class Il2CppICalls
{
    public static IntPtr Resolve(params string[] names)
    {
        if (names == null)
        {
            return IntPtr.Zero;
        }

        foreach (var name in names)
        {
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            var pointer = IL2CPP.il2cpp_resolve_icall(name);
            if (pointer != IntPtr.Zero)
            {
                return pointer;
            }
        }

        return IntPtr.Zero;
    }
}
