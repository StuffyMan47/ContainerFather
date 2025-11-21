using ContainerFather.Bot.States;

namespace ContainerFather.Bot.Services.Interfaces;

public interface IAdminDialogService
{
    void StartWeeklyMessageDialog(long adminId);
    void StartDailyMessageDialog(long adminId);
    AdminDialogState GetDialogState(long adminId);
    void SetDialogData<T>(long adminId, string key, T value);
    void SetDialogState(long adminId, AdminDialogState state);
    T GetDialogData<T>(long adminId, string key);
    void CompleteDialog(long adminId);
    bool IsInDialog(long adminId);
}