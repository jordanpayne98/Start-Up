using UnityEngine;
using UnityEngine.UIElements;

public class CalendarView : IGameView
{
    private readonly IModalPresenter _modal;
    private VisualElement _root;
    private Label _headerLabel;
    private Button _prevButton;
    private Button _nextButton;
    private readonly Button[] _monthPills = new Button[12];
    private readonly VisualElement[] _dayCells = new VisualElement[7];
    private readonly Label[] _dayLabels = new Label[7];
    private readonly VisualElement[] _dayEventContainers = new VisualElement[7];
    private VisualElement _legendContainer;
    private CalendarViewModel _viewModel;

    private static readonly string[] DayNames = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };

    // Pre-allocated event label pools per day (max events per day shown in UI)
    private const int MaxEventsPerDay = 8;
    private readonly Label[][] _eventLabelPools = new Label[7][];

    private static readonly Color _colBlue   = new Color(0.27f, 0.60f, 0.87f, 1f);
    private static readonly Color _colRed    = new Color(0.87f, 0.27f, 0.27f, 1f);
    private static readonly Color _colYellow = new Color(0.95f, 0.78f, 0.20f, 1f);
    private static readonly Color _colPurple = new Color(0.65f, 0.27f, 0.87f, 1f);

    public CalendarView(IModalPresenter modal) {
        _modal = modal;
    }

    public void Initialize(VisualElement root) {
        _root = root;

        var header = new VisualElement();
        header.AddToClassList("flex-row");
        header.AddToClassList("justify-between");
        header.AddToClassList("align-center");
        header.style.marginBottom = 8;

        _prevButton = new Button { text = "<" };
        _prevButton.AddToClassList("btn-secondary");
        _prevButton.AddToClassList("btn-sm");
        _prevButton.clicked += OnPrevClicked;
        header.Add(_prevButton);

        _headerLabel = new Label("Calendar");
        _headerLabel.AddToClassList("section-header");
        header.Add(_headerLabel);

        _nextButton = new Button { text = ">" };
        _nextButton.AddToClassList("btn-secondary");
        _nextButton.AddToClassList("btn-sm");
        _nextButton.clicked += OnNextClicked;
        header.Add(_nextButton);

        _root.Add(header);

        var monthRow = new VisualElement();
        monthRow.AddToClassList("flex-row");
        monthRow.style.flexWrap = Wrap.Wrap;
        monthRow.style.marginBottom = 12;

        string[] monthNames = { "Jan", "Feb", "Mar", "Apr", "May", "Jun",
                                 "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        for (int i = 0; i < 12; i++) {
            var pill = new Button { text = monthNames[i] };
            pill.AddToClassList("sub-tab");
            pill.userData = i + 1;
            pill.RegisterCallback<ClickEvent>(OnMonthPillClick);
            _monthPills[i] = pill;
            monthRow.Add(pill);
        }
        _root.Add(monthRow);

        var dayHeaders = new VisualElement();
        dayHeaders.AddToClassList("flex-row");
        for (int i = 0; i < 7; i++) {
            var dayHeader = new Label(DayNames[i]);
            dayHeader.AddToClassList("text-muted");
            dayHeader.AddToClassList("text-sm");
            dayHeader.AddToClassList("text-bold");
            dayHeader.style.flexGrow = 1;
            dayHeader.style.flexBasis = 0;
            dayHeader.style.unityTextAlign = UnityEngine.TextAnchor.MiddleCenter;
            dayHeaders.Add(dayHeader);
        }
        _root.Add(dayHeaders);

        var weekRow = new VisualElement();
        weekRow.AddToClassList("flex-row");
        weekRow.style.flexGrow = 1;

        for (int i = 0; i < 7; i++) {
            var cell = new VisualElement();
            cell.AddToClassList("card");
            cell.style.flexGrow = 1;
            cell.style.flexBasis = 0;
            cell.style.marginRight = i < 6 ? 4 : 0;
            cell.style.minHeight = 120;

            var dayLabel = new Label();
            dayLabel.AddToClassList("text-lg");
            dayLabel.AddToClassList("text-bold");
            cell.Add(dayLabel);

            var eventContainer = new VisualElement();
            eventContainer.style.marginTop = 8;
            cell.Add(eventContainer);

            _dayCells[i] = cell;
            _dayLabels[i] = dayLabel;
            _dayEventContainers[i] = eventContainer;

            // Pre-create event labels for this day
            _eventLabelPools[i] = new Label[MaxEventsPerDay];
            for (int e = 0; e < MaxEventsPerDay; e++) {
                var evtLabel = new Label();
                evtLabel.AddToClassList("text-sm");
                evtLabel.AddToClassList("badge");
                evtLabel.style.marginBottom = 2;
                evtLabel.style.display = DisplayStyle.None;
                evtLabel.RegisterCallback<ClickEvent>(OnEventLabelClicked);
                eventContainer.Add(evtLabel);
                _eventLabelPools[i][e] = evtLabel;
            }

            weekRow.Add(cell);
        }

        _root.Add(weekRow);

        _legendContainer = new VisualElement();
        _legendContainer.AddToClassList("flex-row");
        _legendContainer.style.marginTop = 8;
        AddLegendItem(_legendContainer, "Player", _colBlue);
        AddLegendItem(_legendContainer, "Competitor", _colRed);
        AddLegendItem(_legendContainer, "Disruption", _colYellow);
        AddLegendItem(_legendContainer, "Showdown", _colPurple);
        _root.Add(_legendContainer);
    }

    public void Bind(IViewModel viewModel) {
        _viewModel = viewModel as CalendarViewModel;
        if (_viewModel == null) return;

        _headerLabel.text = UIFormatting.FormatMonthName(_viewModel.CurrentMonth) + " " + _viewModel.CurrentYear;

        for (int i = 0; i < 12; i++) {
            _monthPills[i].EnableInClassList("sub-tab--active", (i + 1) == _viewModel.CurrentMonth);
        }

        for (int i = 0; i < 7; i++) {
            var day = _viewModel.Days[i];
            _dayLabels[i].text = day.DayNumber.ToString();

            if (day.IsCurrentDay) {
                SetBorderColor(_dayCells[i], new Color(0.3f, 0.79f, 0.69f, 1f), 2);
            } else {
                SetBorderColor(_dayCells[i], new Color(1f, 1f, 1f, 0.05f), 1);
            }

            int eventCount = day.Events.Count;
            var pool = _eventLabelPools[i];
            int poolLen = pool.Length;
            for (int e = 0; e < poolLen; e++) {
                if (e < eventCount) {
                    var evt = day.Events[e];
                    pool[e].text = evt.Title;
                    pool[e].style.backgroundColor = new StyleColor(EventColorToUnityColor(evt.Color));
                    pool[e].style.display = DisplayStyle.Flex;
                    pool[e].userData = evt;
                } else {
                    pool[e].style.display = DisplayStyle.None;
                    pool[e].userData = null;
                }
            }        }
    }

    public void Dispose() {
        if (_prevButton != null) _prevButton.clicked -= OnPrevClicked;
        if (_nextButton != null) _nextButton.clicked -= OnNextClicked;
        for (int i = 0; i < 12; i++) {
            if (_monthPills[i] != null) _monthPills[i].UnregisterCallback<ClickEvent>(OnMonthPillClick);
        }
        for (int i = 0; i < 7; i++) {
            if (_eventLabelPools[i] == null) continue;
            for (int e = 0; e < MaxEventsPerDay; e++) {
                if (_eventLabelPools[i][e] != null)
                    _eventLabelPools[i][e].UnregisterCallback<ClickEvent>(OnEventLabelClicked);
            }
        }
        _viewModel = null;
    }

    private void OnEventLabelClicked(ClickEvent evt) {
        var label = evt.currentTarget as Label;
        if (label?.userData is CalendarEventDisplay display) {
            if (display.ProductId.HasValue)
                _modal?.OpenProductDetail(display.ProductId.Value);
            else if (display.CompetitorId.HasValue)
                _modal?.OpenCompetitorProfile(display.CompetitorId.Value);
        }
    }

    private void OnPrevClicked() {
        if (_viewModel == null) return;
        _viewModel.PrevWeek();
        _viewModel.RecalculateDays();
        Bind(_viewModel);
    }

    private void OnNextClicked() {
        if (_viewModel == null) return;
        _viewModel.NextWeek();
        _viewModel.RecalculateDays();
        Bind(_viewModel);
    }

    private void OnMonthPillClick(ClickEvent evt) {
        if (_viewModel == null) return;
        var btn = evt.currentTarget as Button;
        if (btn?.userData is int month) {
            _viewModel.JumpToMonth(month, _viewModel.CurrentYear);
            _viewModel.RecalculateDays();
            Bind(_viewModel);
        }
    }

    private static void SetBorderColor(VisualElement el, Color col, float width) {
        el.style.borderTopWidth = width;
        el.style.borderBottomWidth = width;
        el.style.borderLeftWidth = width;
        el.style.borderRightWidth = width;
        el.style.borderTopColor = new StyleColor(col);
        el.style.borderBottomColor = new StyleColor(col);
        el.style.borderLeftColor = new StyleColor(col);
        el.style.borderRightColor = new StyleColor(col);
    }

    private static Color EventColorToUnityColor(CalendarEventColor color) {
        switch (color) {
            case CalendarEventColor.Red:    return _colRed;
            case CalendarEventColor.Yellow: return _colYellow;
            case CalendarEventColor.Purple: return _colPurple;
            default:                        return _colBlue;
        }
    }

    private static void AddLegendItem(VisualElement container, string label, Color color) {
        var row = new VisualElement();
        row.AddToClassList("flex-row");
        row.AddToClassList("align-center");
        row.style.marginRight = 12;

        var dot = new VisualElement();
        dot.style.width = 8;
        dot.style.height = 8;
        dot.style.borderTopLeftRadius = 4;
        dot.style.borderTopRightRadius = 4;
        dot.style.borderBottomLeftRadius = 4;
        dot.style.borderBottomRightRadius = 4;
        dot.style.backgroundColor = new StyleColor(color);
        dot.style.marginRight = 4;
        row.Add(dot);

        var lbl = new Label(label);
        lbl.AddToClassList("text-sm");
        lbl.AddToClassList("text-muted");
        row.Add(lbl);

        container.Add(row);
    }
}
