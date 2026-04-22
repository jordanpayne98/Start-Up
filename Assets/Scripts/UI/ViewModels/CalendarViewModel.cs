using System.Collections.Generic;

public enum CalendarEventColor
{
    Blue,
    Red,
    Yellow,
    Purple,
    Orange
}

public struct CalendarEventVM
{
    public int Tick;
    public string Label;
    public CalendarEventColor Color;
    public ProductId? ProductId;
    public CompetitorId? CompetitorId;
    public bool IsShowdown;
}

public struct CalendarEventDisplay
{
    public string Title;
    public string Type;
    public int DayIndex;
    public CalendarEventColor Color;
    public CompetitorId? CompetitorId;
    public ProductId? ProductId;
}

public struct CalendarDay
{
    public int DayNumber;
    public bool IsCurrentDay;
    public List<CalendarEventDisplay> Events;
}

public class CalendarViewModel : IViewModel
{
    public int CurrentDay { get; private set; }
    public int CurrentMonth { get; private set; }
    public int CurrentYear { get; private set; }
    public int DayOffset { get; private set; }

    private readonly CalendarDay[] _days = new CalendarDay[7];
    public CalendarDay[] Days => _days;

    private readonly List<CalendarEventVM> _events = new List<CalendarEventVM>(32);
    public List<CalendarEventVM> Events => _events;

    private IReadOnlyGameState _lastState;
    private CompetitorState _lastCompState;
    private DisruptionState _lastDisruptionState;
    private ProductState _lastProductState;

    public CalendarViewModel() {
        DayOffset = 0;
        for (int i = 0; i < 7; i++) {
            _days[i] = new CalendarDay { DayNumber = i + 1, Events = new List<CalendarEventDisplay>() };
        }
    }

    public void NextWeek() { DayOffset += 7; }
    public void PrevWeek() { DayOffset -= 7; }

    public void JumpToMonth(int month, int year) {
        int targetAbsoluteDay = TimeState.ToAbsoluteDay(1, month, year);
        int currentAbsoluteDay = _lastState != null ? _lastState.CurrentDay : 0;
        DayOffset = targetAbsoluteDay - currentAbsoluteDay;
    }

    public void RecalculateDays() {
        if (_lastState == null) return;
        Refresh(_lastState, _lastCompState, _lastDisruptionState, _lastProductState);
    }

    public void Refresh(IReadOnlyGameState state) {
        if (state == null) return;
        var snapshot = state as GameStateSnapshot;
        if (snapshot != null) {
            Refresh(state, snapshot.CompetitorState, snapshot.DisruptionStateRef, snapshot.ProductStateRef);
        } else {
            CurrentDay = state.CurrentDay;
            CurrentMonth = state.CurrentMonth;
            CurrentYear = state.CurrentYear;
        }
    }

    public void Refresh(IReadOnlyGameState state, CompetitorState compState, DisruptionState disruptionState, ProductState productState = null) {
        if (state == null) return;

        _lastState = state;
        _lastCompState = compState;
        _lastDisruptionState = disruptionState;
        _lastProductState = productState;

        CurrentDay = state.CurrentDay;
        CurrentMonth = state.CurrentMonth;
        CurrentYear = state.CurrentYear;

        int anchorAbsoluteDay = state.CurrentDay + DayOffset;

        for (int i = 0; i < 7; i++) {
            int thisAbsoluteDay = anchorAbsoluteDay + i;
            _days[i].DayNumber = TimeState.GetDayOfMonth(thisAbsoluteDay);
            _days[i].IsCurrentDay = (thisAbsoluteDay == state.CurrentDay);
            _days[i].Events.Clear();
        }

        CurrentMonth = TimeState.GetMonth(anchorAbsoluteDay);
        CurrentYear = TimeState.GetYear(anchorAbsoluteDay);

        _events.Clear();

        // Contract deadlines (player — blue)
        foreach (var contract in state.GetActiveContracts()) {
            int deadlineDay = contract.DeadlineTick / 4800;
            _events.Add(new CalendarEventVM {
                Tick = contract.DeadlineTick,
                Label = contract.Name + " deadline",
                Color = CalendarEventColor.Blue
            });
            for (int i = 0; i < 7; i++) {
                int thisAbsoluteDay = anchorAbsoluteDay + i;
                if (thisAbsoluteDay == deadlineDay) {
                    _days[i].Events.Add(new CalendarEventDisplay {
                        Title = contract.Name + " deadline",
                        Type = "deadline",
                        DayIndex = i,
                        Color = CalendarEventColor.Blue
                    });
                }
            }
        }

        // HR search completions (player — blue)
        var searches = state.ActiveHRSearches;
        int searchCount = searches.Count;
        for (int s = 0; s < searchCount; s++) {
            int completionDay = searches[s].completionTick / 4800;
            _events.Add(new CalendarEventVM {
                Tick = searches[s].completionTick,
                Label = "HR Search completes",
                Color = CalendarEventColor.Blue
            });
            for (int i = 0; i < 7; i++) {
                int thisAbsoluteDay = anchorAbsoluteDay + i;
                if (thisAbsoluteDay == completionDay) {
                    _days[i].Events.Add(new CalendarEventDisplay {
                        Title = "HR Search completes",
                        Type = "hr",
                        DayIndex = i,
                        Color = CalendarEventColor.Blue
                    });
                }
            }
        }

        // Player product releases (green)
        if (productState?.developmentProducts != null) {
            foreach (var kvp in productState.developmentProducts) {
                var prod = kvp.Value;
                if (prod.IsCompetitorProduct) continue;
                if (!prod.HasAnnouncedReleaseDate || prod.TargetReleaseTick <= 0) continue;
                int releaseDay = prod.TargetReleaseTick / TimeState.TicksPerDay;
                string releaseLabel = prod.ProductName;
                _events.Add(new CalendarEventVM {
                    Tick = prod.TargetReleaseTick,
                    Label = releaseLabel,
                    Color = CalendarEventColor.Blue,
                    ProductId = prod.Id
                });
                for (int i = 0; i < 7; i++) {
                    int thisAbsoluteDay = anchorAbsoluteDay + i;
                    if (thisAbsoluteDay == releaseDay) {
                        _days[i].Events.Add(new CalendarEventDisplay {
                            Title = releaseLabel,
                            Type = "release",
                            DayIndex = i,
                            Color = CalendarEventColor.Blue,
                            ProductId = prod.Id
                        });
                    }
                }
            }
        }

        // Competitor product releases (red)
        if (compState?.competitors != null && productState?.developmentProducts != null) {
            foreach (var compKvp in compState.competitors) {
                var comp = compKvp.Value;
                if (comp.IsBankrupt || comp.IsAbsorbed) continue;
                if (comp.InDevelopmentProductIds == null) continue;

                int idCount = comp.InDevelopmentProductIds.Count;
                for (int p = 0; p < idCount; p++) {
                    var pid = comp.InDevelopmentProductIds[p];
                    if (!productState.developmentProducts.TryGetValue(pid, out var devProduct)) continue;
                    if (devProduct.TargetReleaseTick <= 0) continue;

                    int releaseDay = devProduct.TargetReleaseTick / TimeState.TicksPerDay;
                    string label = devProduct.ProductName;
                    _events.Add(new CalendarEventVM {
                        Tick = devProduct.TargetReleaseTick,
                        Label = label,
                        Color = CalendarEventColor.Red,
                        CompetitorId = compKvp.Key,
                        ProductId = pid
                    });
                    for (int i = 0; i < 7; i++) {
                        int thisAbsoluteDay = anchorAbsoluteDay + i;
                        if (thisAbsoluteDay == releaseDay) {
                            _days[i].Events.Add(new CalendarEventDisplay {
                                Title = label,
                                Type = "competitor-release",
                                DayIndex = i,
                                Color = CalendarEventColor.Red,
                                CompetitorId = compKvp.Key,
                                ProductId = pid
                            });
                        }
                    }
                }
            }
        }

        // Competitor shipped products (purple)
        if (compState?.competitors != null && productState?.shippedProducts != null) {
            foreach (var compKvp in compState.competitors) {
                var comp = compKvp.Value;
                if (comp.IsBankrupt || comp.IsAbsorbed) continue;
                if (comp.ActiveProductIds == null) continue;

                int activeCount = comp.ActiveProductIds.Count;
                for (int p = 0; p < activeCount; p++) {
                    var pid = comp.ActiveProductIds[p];
                    if (!productState.shippedProducts.TryGetValue(pid, out var shipped)) continue;
                    if (shipped.ShipTick <= 0) continue;

                    int shipDay = shipped.ShipTick / TimeState.TicksPerDay;
                    string suffix = " (launched)";
                    string label = shipped.ProductName + suffix;

                    _events.Add(new CalendarEventVM {
                        Tick = shipped.ShipTick,
                        Label = label,
                        Color = CalendarEventColor.Purple,
                        CompetitorId = compKvp.Key,
                        ProductId = pid
                    });
                    for (int i = 0; i < 7; i++) {
                        int thisAbsoluteDay = anchorAbsoluteDay + i;
                        if (thisAbsoluteDay == shipDay) {
                            _days[i].Events.Add(new CalendarEventDisplay {
                                Title = label,
                                Type = "competitor-shipped",
                                DayIndex = i,
                                Color = CalendarEventColor.Purple,
                                CompetitorId = compKvp.Key,
                                ProductId = pid
                            });
                        }
                    }
                }
            }
        }

        // Competitor scheduled product updates (orange)
        if (compState?.competitors != null && productState?.shippedProducts != null) {
            foreach (var compKvp in compState.competitors) {
                var comp = compKvp.Value;
                if (comp.IsBankrupt || comp.IsAbsorbed) continue;
                if (comp.ScheduledUpdates == null) continue;

                int updateCount = comp.ScheduledUpdates.Count;
                for (int p = 0; p < updateCount; p++) {
                    var scheduled = comp.ScheduledUpdates[p];
                    if (scheduled.ScheduledTick <= 0) continue;
                    if (!productState.shippedProducts.TryGetValue(scheduled.ProductId, out var shippedProd)) continue;

                    int updateDay = scheduled.ScheduledTick / TimeState.TicksPerDay;
                    string label = shippedProd.ProductName + " (update)";
                    _events.Add(new CalendarEventVM {
                        Tick = scheduled.ScheduledTick,
                        Label = label,
                        Color = CalendarEventColor.Orange,
                        CompetitorId = compKvp.Key,
                        ProductId = scheduled.ProductId
                    });
                    for (int i = 0; i < 7; i++) {
                        int thisAbsoluteDay = anchorAbsoluteDay + i;
                        if (thisAbsoluteDay == updateDay) {
                            _days[i].Events.Add(new CalendarEventDisplay {
                                Title = label,
                                Type = "competitor-update",
                                DayIndex = i,
                                Color = CalendarEventColor.Orange,
                                CompetitorId = compKvp.Key,
                                ProductId = scheduled.ProductId
                            });
                        }
                    }
                }
            }
        }

        // Active disruptions (yellow)
        if (disruptionState?.activeDisruptions != null) {            int dCount = disruptionState.activeDisruptions.Count;
            for (int d = 0; d < dCount; d++) {
                var disruption = disruptionState.activeDisruptions[d];
                int startDay = disruption.StartTick / TimeState.TicksPerDay;
                int endDay = (disruption.StartTick + disruption.DurationTicks) / TimeState.TicksPerDay;
                string label = disruption.Description ?? disruption.EventType.ToString();

                _events.Add(new CalendarEventVM {
                    Tick = disruption.StartTick,
                    Label = label,
                    Color = CalendarEventColor.Yellow
                });

                for (int i = 0; i < 7; i++) {
                    int thisAbsoluteDay = anchorAbsoluteDay + i;
                    if (thisAbsoluteDay >= startDay && thisAbsoluteDay <= endDay) {
                        _days[i].Events.Add(new CalendarEventDisplay {
                            Title = label,
                            Type = "disruption",
                            DayIndex = i,
                            Color = CalendarEventColor.Yellow
                        });
                    }
                }
            }
        }
    }
}
