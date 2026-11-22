using System.Threading.Tasks;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Extensions.ToIl2Cpp
{
    public static class TaskExtensions
    {
        extension(Task task)
        {
            public void McsHandleExceptions()
            {
                if (task == null)
                {
                    return;
                }

                task.ContinueWith(finishedTask => McsLogger.LogError(finishedTask.Exception), TaskContinuationOptions.OnlyOnFaulted);
            }
        }
    }
}