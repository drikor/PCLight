using System.Threading;
using System.Threading.Tasks;

namespace YeelightBulbControl
{
    public class RateLimiter
    {
        private readonly SemaphoreSlim semaphore;

        public RateLimiter(int maxRequests)
        {
            semaphore = new SemaphoreSlim(maxRequests, maxRequests);
        }

        public async Task<bool> TryConsumeAsync()
        {
            if (await semaphore.WaitAsync(0))
            {
                // Успешно получили доступ, ждем 1 секунду
                await Task.Delay(1000);
                return true;
            }
            else
            {
                // Не удалось получить доступ
                return false;
            }
        }

        public void Release()
        {
            semaphore.Release();
        }
    }
}
