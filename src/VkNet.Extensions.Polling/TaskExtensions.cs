using System;
using System.Threading.Tasks;

namespace VkNet.Extensions.Polling
{
    public static class TaskExtensions
    {
        public static async Task<T> IgnoreExceptions<T>(this Task<T> task, bool ignoreOperationCanceledException = true)
        {
            try
            {
                return await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                if (!ignoreOperationCanceledException)
                    throw;
            }
            catch
            {
                // Ignore
            }

            return default;
        }
    }
}