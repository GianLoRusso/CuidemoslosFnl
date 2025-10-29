namespace Cuidemoslos.Web.ViewModels;

public record PatientListItemVM(int Id, string FullName, string Email, DateTime CreatedAt, int AlertsCount);
public record PatientDetailVM(int Id, string FullName, string Email, DateTime CreatedAt, IEnumerable<MoodItemVM> Mood, IEnumerable<NotificationItemVM> Notifications);
public record MoodItemVM(DateTime CreatedAt, int Score, string? Notes);
public record NotificationItemVM(DateTime CreatedAt, string Subject);

