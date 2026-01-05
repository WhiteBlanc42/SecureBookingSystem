using System.Threading.Tasks;

namespace SecureBookingSystem.Services
{
    public interface IAuditService
    {
        Task LogAsync(string userId, string action, string details, string ipAddress);
    }
}
