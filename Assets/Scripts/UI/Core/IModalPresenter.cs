using System.Collections.Generic;

public interface IModalPresenter
{
    void ShowModal(IGameView view, IViewModel viewModel);
    void DismissModal();

    void OpenHRSearchConfigurator(TeamId teamId);
    void OpenHRCandidateReview(IReadOnlyList<int> candidateIds, string teamName, string criteriaLabel);
    void OpenCompetitorProfile(CompetitorId competitorId);
    void OpenProductDetail(ProductId productId);
}
