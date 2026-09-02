using System;
using System.Threading.Tasks;

namespace Arman.AssetProviding
{
    public static class TaskUtilities
    {

        public static Task WaitUntil(Func<bool> condition)
        {
            return Task.Run(
                async () =>
                {
                    while (!condition())
                    {
                        await Task.Delay(100);
                    }
                });
        }
    }
}