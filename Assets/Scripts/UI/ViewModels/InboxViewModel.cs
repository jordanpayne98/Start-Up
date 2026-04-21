using System.Collections.Generic;

public enum InboxFilter
{
    All,
    Unread,
    Alert,
    Contract,
    Recruitment,
    HR,
    Finance,
    Research,
    Operations,
    News
}

public class InboxViewModel : IViewModel
{
    private readonly List<InboxItemDisplay> _allMessages = new List<InboxItemDisplay>();
    private readonly List<InboxItemDisplay> _filteredMessages = new List<InboxItemDisplay>();
    private readonly HashSet<int> _expandedIds = new HashSet<int>();

    public List<InboxItemDisplay> FilteredMessages => _filteredMessages;
    public InboxFilter ActiveFilter { get; private set; }
    public int UnreadCount { get; private set; }
    public int TotalCount { get; private set; }
    public int CriticalUnreadCount { get; private set; } // TODO: wire to sidebar badge when added

    public InboxViewModel()
    {
        ActiveFilter = InboxFilter.All;
    }

    public void SetFilter(InboxFilter filter)
    {
        ActiveFilter = filter;
        ApplyFilter();
    }

    public void ToggleExpanded(int mailId)
    {
        if (_expandedIds.Contains(mailId))
            _expandedIds.Remove(mailId);
        else
            _expandedIds.Add(mailId);
        ApplyFilter();
    }

    // Optimistically mark one item read in the local list without waiting for the command round-trip.
    public void OptimisticMarkRead(int mailId)
    {
        int count = _allMessages.Count;
        for (int i = 0; i < count; i++)
        {
            var msg = _allMessages[i];
            if (msg.Id == mailId && !msg.IsRead)
            {
                msg.IsRead = true;
                _allMessages[i] = msg;
                if (UnreadCount > 0) UnreadCount--;
                if (msg.Priority == MailPriority.Critical && CriticalUnreadCount > 0) CriticalUnreadCount--;
                ApplyFilter();
                break;
            }
        }
    }

    // Optimistically mark all items read in the local list.
    public void OptimisticMarkAllRead()
    {
        int count = _allMessages.Count;
        for (int i = 0; i < count; i++)
        {
            var msg = _allMessages[i];
            if (!msg.IsRead)
            {
                msg.IsRead = true;
                _allMessages[i] = msg;
            }
        }
        UnreadCount = 0;
        CriticalUnreadCount = 0;
        ApplyFilter();
    }

    public void Refresh(IReadOnlyGameState state)
    {
        if (state == null) return;

        _allMessages.Clear();
        UnreadCount = 0;
        CriticalUnreadCount = 0;

        int expiryTicks = TimeState.TicksPerDay * 60;

        var inbox = state.InboxItems;
        int count = inbox.Count;
        for (int i = 0; i < count; i++)
        {
            var mail = inbox[i];
            if (mail.IsDismissed) continue;

            // 60-day auto-clear
            if (state.CurrentTick - mail.Tick > expiryTicks) continue;

            if (!mail.IsRead)
            {
                UnreadCount++;
                if (mail.Priority == MailPriority.Critical)
                    CriticalUnreadCount++;
            }

            _allMessages.Add(new InboxItemDisplay
            {
                Id = mail.Id,
                Category = mail.Category,
                Priority = mail.Priority,
                Title = mail.Title,
                Body = mail.Body,
                Timestamp = UIFormatting.FormatMailAge(mail.Tick, state.CurrentTick),
                IsRead = mail.IsRead,
                Tick = mail.Tick,
                Actions = mail.Actions,
                IsExpanded = _expandedIds.Contains(mail.Id),
                IsNewsArticle = mail.Category == MailCategory.NewsArticle,
                AttachedReport = mail.AttachedReport
            });
        }

        TotalCount = _allMessages.Count;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        _filteredMessages.Clear();
        int count = _allMessages.Count;
        for (int i = 0; i < count; i++)
        {
            var msg = _allMessages[i];
            bool include = false;
            switch (ActiveFilter)
            {
                case InboxFilter.All:
                    include = true;
                    break;
                case InboxFilter.Unread:
                    include = !msg.IsRead;
                    break;
                case InboxFilter.Alert:
                    include = msg.Category == MailCategory.Alert;
                    break;
                case InboxFilter.Contract:
                    include = msg.Category == MailCategory.Contract;
                    break;
                case InboxFilter.Recruitment:
                    include = msg.Category == MailCategory.Recruitment;
                    break;
                case InboxFilter.HR:
                    include = msg.Category == MailCategory.HR;
                    break;
                case InboxFilter.Finance:
                    include = msg.Category == MailCategory.Finance;
                    break;
                case InboxFilter.Research:
                    include = msg.Category == MailCategory.Research;
                    break;
                case InboxFilter.Operations:
                    include = msg.Category == MailCategory.Operations;
                    break;
                case InboxFilter.News:
                    include = msg.Category == MailCategory.NewsArticle;
                    break;
            }

            if (include)
            {
                // Stamp expanded state
                var stamped = msg;
                stamped.IsExpanded = _expandedIds.Contains(msg.Id);
                _filteredMessages.Add(stamped);
            }
        }
    }
}
