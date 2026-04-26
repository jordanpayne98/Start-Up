public interface IModalPresenter
{
    void ShowModal(IGameView view, IViewModel viewModel);
    void DismissModal();

    void OpenCompetitorProfile(CompetitorId competitorId);
    void OpenProductDetail(ProductId productId);
    void OpenRenewalModal(EmployeeId? autoExpandId = null);
    void ShowCandidateDetailModal(int candidateId, bool showCounterOffer = false);
}
