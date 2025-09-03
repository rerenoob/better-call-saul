using better_call_saul.Models;

namespace better_call_saul.Services;

public interface IDashboardService
{
    Task<DashboardData> GetDashboardDataAsync(string userId);
}