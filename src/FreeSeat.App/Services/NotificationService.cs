using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace FreeSeat.App.Services;

/// <summary>
/// Represents a notification displayed to the user.
/// </summary>
public sealed class Notification
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public NotificationType Type { get; init; } = NotificationType.Info;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public bool IsDismissed { get; set; }
    public TimeSpan AutoDismissDelay { get; init; } = TimeSpan.FromSeconds(5);
}

/// <summary>
/// Types of notifications.
/// </summary>
public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}

/// <summary>
/// Service for managing in-app notifications.
/// </summary>
public interface INotificationService
{
    ObservableCollection<Notification> Notifications { get; }
    void ShowInfo(string title, string message, TimeSpan? autoDismiss = null);
    void ShowSuccess(string title, string message, TimeSpan? autoDismiss = null);
    void ShowWarning(string title, string message, TimeSpan? autoDismiss = null);
    void ShowError(string title, string message, TimeSpan? autoDismiss = null);
    void Dismiss(string notificationId);
    void DismissAll();
}

/// <summary>
/// Implementation of notification service.
/// </summary>
public sealed partial class NotificationService : ObservableObject, INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly DispatcherTimer _cleanupTimer;

    public ObservableCollection<Notification> Notifications { get; } = new();

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _cleanupTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _cleanupTimer.Tick += CleanupTimer_Tick;
        _cleanupTimer.Start();
    }

    public void ShowInfo(string title, string message, TimeSpan? autoDismiss = null)
    {
        Show(NotificationType.Info, title, message, autoDismiss);
    }

    public void ShowSuccess(string title, string message, TimeSpan? autoDismiss = null)
    {
        Show(NotificationType.Success, title, message, autoDismiss);
    }

    public void ShowWarning(string title, string message, TimeSpan? autoDismiss = null)
    {
        Show(NotificationType.Warning, title, message, autoDismiss);
    }

    public void ShowError(string title, string message, TimeSpan? autoDismiss = null)
    {
        Show(NotificationType.Error, title, message, autoDismiss);
    }

    private void Show(NotificationType type, string title, string message, TimeSpan? autoDismiss)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            var notification = new Notification
            {
                Type = type,
                Title = title,
                Message = message,
                AutoDismissDelay = autoDismiss ?? TimeSpan.FromSeconds(5)
            };
            Notifications.Add(notification);
            _logger.LogDebug("Notification shown: {Type} - {Title}: {Message}", type, title, message);
        });
    }

    public void Dismiss(string notificationId)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            var notification = Notifications.FirstOrDefault(n => n.Id == notificationId);
            if (notification != null)
            {
                notification.IsDismissed = true;
                Notifications.Remove(notification);
            }
        });
    }

    public void DismissAll()
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            foreach (var notification in Notifications.ToList())
            {
                notification.IsDismissed = true;
            }
            Notifications.Clear();
        });
    }

    private void CleanupTimer_Tick(object? sender, EventArgs e)
    {
        var expired = Notifications
            .Where(n => n.IsDismissed || DateTime.Now - n.Timestamp > n.AutoDismissDelay)
            .ToList();

        foreach (var notification in expired)
        {
            Notifications.Remove(notification);
        }
    }
}