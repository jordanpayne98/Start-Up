/// <summary>
/// Data struct passed to ScreenHeaderView.Bind(). Contains all display-ready content
/// for the reusable screen header component (breadcrumb, icon, title, subtitle).
/// </summary>
public struct ScreenHeaderData
{
    /// <summary>Breadcrumb label, e.g. "← HR Portal".</summary>
    public string BreadcrumbLabel;

    /// <summary>USS class to apply to the header icon element.</summary>
    public string IconClass;

    /// <summary>Screen title, e.g. "Candidates".</summary>
    public string Title;

    /// <summary>Screen subtitle / description, e.g. "Browse, evaluate, and manage potential hires for your team."</summary>
    public string Subtitle;

    /// <summary>Whether the breadcrumb / back link is visible.</summary>
    public bool ShowBreadcrumb;
}
