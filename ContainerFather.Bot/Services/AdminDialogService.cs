using System.Collections.Concurrent;
using ContainerFather.Bot.Services.Interfaces;
using ContainerFather.Bot.States;

namespace ContainerFather.Bot.Services;

public class AdminDialogService : IAdminDialogService
{
    private readonly ConcurrentDictionary<long, AdminDialogState> _adminStates = new();
    private readonly ConcurrentDictionary<long, ConcurrentDictionary<string, object>> _dialogData = new();

    public void StartWeeklyMessageDialog(long adminId)
    {
        SetDialogState(adminId, AdminDialogState.ManagingWeeklyMessage);
    }

    public void StartDailyMessageDialog(long adminId)
    {
        SetDialogState(adminId, AdminDialogState.ManagingDailyMessage);
    }

    public AdminDialogState GetDialogState(long adminId)
    {
        return _adminStates.GetValueOrDefault(adminId, AdminDialogState.None);
    }

    public void SetDialogData<T>(long adminId, string key, T value)
    {
        if (!_dialogData.ContainsKey(adminId))
        {
            _dialogData[adminId] = new ConcurrentDictionary<string, object>();
        }
        _dialogData[adminId][key] = value;
    }

    public T GetDialogData<T>(long adminId, string key)
    {
        if (_dialogData.TryGetValue(adminId, out var data) && 
            data.TryGetValue(key, out var value) && 
            value is T result)
        {
            return result;
        }
        return default;
    }

    public void SetDialogState(long adminId, AdminDialogState state)
    {
        _adminStates[adminId] = state;
        if (!_dialogData.ContainsKey(adminId))
        {
            _dialogData[adminId] = new ConcurrentDictionary<string, object>();
        }
    }

    public void CompleteDialog(long adminId)
    {
        _adminStates.TryRemove(adminId, out _);
        _dialogData.TryRemove(adminId, out _);
    }
    
    public bool IsInDialog(long adminId) => _adminStates.ContainsKey(adminId);
}