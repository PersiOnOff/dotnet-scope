using Microsoft.AspNetCore.SignalR;

namespace DevDashboard.Core.Hubs;

/// <summary>
/// SignalR hub the dashboard UI connects to. Events are pushed to every connected dashboard
/// through <c>ReceiveEvent</c>.
/// </summary>
public class DashboardHub : Hub
{
}
