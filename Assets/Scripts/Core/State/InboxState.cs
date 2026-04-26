using System;
using System.Collections.Generic;

public enum MailCategory { Alert, Contract, Recruitment, HR, Finance, Technology, Operations, NewsArticle }
public enum MailPriority { Info, Warning, Critical }
public enum MailActionType { Navigate, OpenModal, DismissOnly }

[Serializable]
public struct MailAction
{
    public string Label;
    public MailActionType Type;
    public int NavTargetInt; // ScreenId cast to int; -1 = no target
    public string ModalKey;
    public int TabHint; // tab index for ITabNavigable screens; -1 = no hint

    public ScreenId? NavTarget
    {
        get => NavTargetInt >= 0 ? (ScreenId?)((ScreenId)NavTargetInt) : null;
        set => NavTargetInt = value.HasValue ? (int)value.Value : -1;
    }
}

[Serializable]
public struct MailItem
{
    public int Id;
    public int Tick;
    public MailCategory Category;
    public MailPriority Priority;
    public string Title;
    public string Body;
    public bool IsRead;
    public bool IsDismissed;
    public MailAction[] Actions;
    public HypeEventType? EventType;
    public ProductId? RelatedProductId;
    public MonthlyNewsReport AttachedReport;
}

[Serializable]
public class InboxState
{
    public const int MaxItems = 50;

    public List<MailItem> Items;
    public int LastSalaryMailTick;

    public static InboxState CreateNew()
    {
        return new InboxState
        {
            Items = new List<MailItem>(),
            LastSalaryMailTick = 0
        };
    }

    // Prepend item newest-first and enforce 50-item cap.
    public void AddItem(MailItem item)
    {
        Items.Insert(0, item);
        EnforceCap();
    }

    private void EnforceCap()
    {
        if (Items.Count <= MaxItems) return;

        // Evict oldest read item first (highest index = oldest).
        int readIndex = -1;
        for (int i = Items.Count - 1; i >= 0; i--)
        {
            if (Items[i].IsRead)
            {
                readIndex = i;
                break;
            }
        }

        if (readIndex >= 0)
        {
            Items.RemoveAt(readIndex);
        }
        else
        {
            // No read items — evict oldest item regardless.
            Items.RemoveAt(Items.Count - 1);
        }
    }
}
