using System;

public class AutoSaveSystem
{
    private readonly GameController _gameController;
    private readonly GameEventBus _eventBus;
    private Action<DayChangedEvent> _dayChangedHandler;
    private int _daysSinceLastSave;

    private const int AutoSaveIntervalDays = 7;

    public AutoSaveSystem(GameController gameController, GameEventBus eventBus)
    {
        _gameController = gameController;
        _eventBus = eventBus;
        _daysSinceLastSave = 0;
        _dayChangedHandler = OnDayChanged;
        _eventBus.Subscribe<DayChangedEvent>(_dayChangedHandler);
    }

    public void Dispose()
    {
        _eventBus.Unsubscribe<DayChangedEvent>(_dayChangedHandler);
        _dayChangedHandler = null;
    }

    private void OnDayChanged(DayChangedEvent evt)
    {
        _daysSinceLastSave++;
        if (_daysSinceLastSave < AutoSaveIntervalDays)
            return;

        GameState state = _gameController.GetGameState();
        string slotName = SaveManager.GetNextAutoSaveSlot();
        string displayName = BuildDisplayName(state, evt);
        SaveManager.SaveGame(state, RngStateTracker.GetInvocationCounts(), slotName, displayName, isAutoSave: true);
        _daysSinceLastSave = 0;
    }

    private static string BuildDisplayName(GameState state, DayChangedEvent evt)
    {
        string company = state.companyName ?? "Company";
        return $"{company} - Y{evt.Year} M{evt.Month} D{evt.Day}";
    }
}
