public static class UIIcons
{
    // Mail categories
    public const string IconMailHR = "icon-mail-hr";
    public const string IconMailContract = "icon-mail-contract";
    public const string IconMailFinance = "icon-mail-finance";
    public const string IconMailTechnology = "icon-mail-research";

    // Navigation
    public const string IconPortal = "icon-portal";
    public const string IconStaff = "icon-staff";
    public const string IconBusiness = "icon-business";
    public const string IconTech = "icon-tech";

    // Status
    public const string IconSuccess = "icon-success";
    public const string IconWarning = "icon-warning";
    public const string IconDanger = "icon-danger";
    public const string IconInfo = "icon-info";

    // Stars
    public const string StarFilled = "star-filled";
    public const string StarEmpty = "star-empty";

    // Actions
    public const string IconHire = "icon-hire";
    public const string IconFire = "icon-fire";
    public const string IconAssign = "icon-assign";
    public const string IconResearch = "icon-research";
    public const string IconMoney = "icon-money";

    // Contract phases
    public const string IconPlanning = "icon-planning";
    public const string IconDesign = "icon-design";
    public const string IconDevelopment = "icon-development";
    public const string IconTesting = "icon-testing";

    public static string GetMailCategoryIcon(MailCategory category) {
        switch (category) {
            case MailCategory.HR: return IconMailHR;
            case MailCategory.Contract: return IconMailContract;
            case MailCategory.Finance: return IconMailFinance;
            case MailCategory.Technology: return IconMailTechnology;
            default: return IconInfo;
        }
    }

    public static string GetToastIcon(ToastType type) {
        switch (type) {
            case ToastType.Success: return IconSuccess;
            case ToastType.Warning: return IconWarning;
            case ToastType.Danger: return IconDanger;
            default: return IconInfo;
        }
    }
}
