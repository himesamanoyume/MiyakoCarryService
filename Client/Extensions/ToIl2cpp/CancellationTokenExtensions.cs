using System.Threading;

namespace MiyakoCarryService.Client.Extensions.ToIl2Cpp
{
    public static class CancellationTokenExtensions
    {
        extension(CancellationToken cancellationToken)
        {
            public Il2CppSystem.Threading.CancellationToken ToIl2Cpp()
            {
                return new Il2CppSystem.Threading.CancellationToken(cancellationToken.IsCancellationRequested);
            }
        }
    }
}