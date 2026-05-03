public interface IToastService
{
    void Show(ToastData data);
    void Dismiss(int toastId);
}
