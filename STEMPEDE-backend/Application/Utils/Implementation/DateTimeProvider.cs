using Application.Utils.Interfaces;

namespace Application.Utils.Implementation
{
    public class DateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
