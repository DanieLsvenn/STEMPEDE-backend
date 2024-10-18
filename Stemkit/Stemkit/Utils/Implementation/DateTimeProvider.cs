using Stemkit.Utils.Interfaces;

namespace Stemkit.Utils.Implementation
{
    public class DateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
