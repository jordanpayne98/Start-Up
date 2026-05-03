using System;

public struct ToastData
{
    public int      Id;
    public ToastType Type;
    public string   Title;
    public string   Message;
    public string   IconClass;
    public float    Duration;
    public string   ActionLabel;
    public Action   OnAction;
}

public struct ModalOptions
{
    public bool   DismissOnBackdropClick;
    public bool   ShowCloseButton;
    public string WidthClass;

    public static readonly ModalOptions Default = new ModalOptions
    {
        DismissOnBackdropClick = true,
        ShowCloseButton        = true,
        WidthClass             = ""
    };
}
