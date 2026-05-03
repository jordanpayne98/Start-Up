public interface IModalPresenter
{
    void ShowModal(IGameView view, IViewModel viewModel, ModalOptions options = default);
    void DismissModal();
    void DismissAll();
    bool IsModalOpen { get; }

    // Convenience modal openers (preserved from previous contract)
    void OpenCompetitorProfile(CompetitorId competitorId);
    void OpenProductDetail(ProductId productId);
    void OpenRenewalModal(EmployeeId? autoExpandId = null);
    void ShowCandidateDetailModal(int candidateId, bool showCounterOffer = false);
}
