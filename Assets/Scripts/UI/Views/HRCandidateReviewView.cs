using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class HRCandidateReviewView : IGameView
{
    private VisualElement _root;
    private HRCandidateReviewViewModel _viewModel;

    private Label _titleLabel;
    private Label _criteriaLabel;
    private VisualElement _candidateList;
    private Button _closeBtn;

    // Element pool for candidate rows
    private readonly List<VisualElement> _rows = new List<VisualElement>();

    public void Initialize(VisualElement root)
    {
        _root = root;

        // ── Header ───────────────────────────────────────────────────────────────
        var header = new VisualElement();
        header.AddToClassList("modal-header");
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.alignItems = Align.Center;

        _titleLabel = new Label("HR Search Results");
        _titleLabel.AddToClassList("text-xl");
        _titleLabel.AddToClassList("text-bold");
        header.Add(_titleLabel);

        _closeBtn = new Button { text = "X" };
        _closeBtn.AddToClassList("btn-sm");
        _closeBtn.style.minWidth = 30;
        header.Add(_closeBtn);
        _root.Add(header);

        // ── Scrollable body ───────────────────────────────────────────────────────
        var bodyScroll = new ScrollView();
        bodyScroll.AddToClassList("modal-body");
        bodyScroll.style.flexGrow = 1;
        bodyScroll.style.flexShrink = 1;
        var body = bodyScroll.contentContainer;
        _root.Add(bodyScroll);

        // Criteria label
        _criteriaLabel = new Label();
        _criteriaLabel.AddToClassList("text-sm");
        _criteriaLabel.AddToClassList("text-muted");
        _criteriaLabel.style.marginBottom = 12;
        body.Add(_criteriaLabel);

        // Candidate list
        _candidateList = new VisualElement();
        body.Add(_candidateList);
    }

    public void Bind(IViewModel viewModel)
    {
        _viewModel = viewModel as HRCandidateReviewViewModel;
        if (_viewModel == null) return;

        if (_titleLabel != null)
            _titleLabel.text = "HR Search Results — " + _viewModel.TeamName;
        if (_criteriaLabel != null)
            _criteriaLabel.text = _viewModel.SearchCriteriaLabel;

        if (_closeBtn != null)
        {
            _closeBtn.clicked -= OnCloseClicked;
            _closeBtn.clicked += OnCloseClicked;
        }

        RebuildCandidateList();
    }

    public void Dispose()
    {
        _viewModel = null;
        _rows.Clear();
    }

    private void RebuildCandidateList()
    {
        if (_candidateList == null || _viewModel == null) return;
        _candidateList.Clear();
        _rows.Clear();

        var candidates = _viewModel.PendingCandidates;
        int count = candidates.Count;

        if (count == 0)
        {
            var emptyLabel = new Label("No candidates pending review.");
            emptyLabel.AddToClassList("text-muted");
            _candidateList.Add(emptyLabel);
            return;
        }

        for (int i = 0; i < count; i++)
        {
            var row = BuildCandidateRow(candidates[i]);
            _candidateList.Add(row);
            _rows.Add(row);
        }
    }

    private VisualElement BuildCandidateRow(HRCandidateReviewViewModel.ReviewCandidateDisplay data)
    {
        var card = new VisualElement();
        card.AddToClassList("card");
        card.style.marginBottom = 8;
        card.style.paddingTop    = 8;
        card.style.paddingBottom = 8;
        card.style.paddingLeft   = 10;
        card.style.paddingRight  = 10;
        card.style.flexDirection = FlexDirection.Row;
        card.style.justifyContent = Justify.SpaceBetween;
        card.style.alignItems = Align.Center;

        // Candidate info
        var info = new VisualElement();
        info.style.flexGrow = 1;

        var nameLabel = new Label(data.Name);
        nameLabel.AddToClassList("text-bold");
        info.Add(nameLabel);

        var roleLabel = new Label(data.Role);
        roleLabel.AddToClassList("role-pill");
        roleLabel.AddToClassList(UIFormatting.RolePillClass(data.Role));
        roleLabel.style.alignSelf = Align.FlexStart;
        roleLabel.style.marginTop = 2;
        info.Add(roleLabel);

        var statsRow = new VisualElement();
        statsRow.style.flexDirection = FlexDirection.Row;
        statsRow.style.marginTop = 4;

        var caLabel = new Label("Ability ★" + data.AbilityStars);
        caLabel.AddToClassList("text-sm");
        caLabel.style.marginRight = 10;
        statsRow.Add(caLabel);

        var paLabel = new Label("Potential ★" + data.PotentialStars);
        paLabel.AddToClassList("text-sm");
        paLabel.style.marginRight = 10;
        statsRow.Add(paLabel);

        var tierLabel = new Label(data.SkillTierLabel);
        tierLabel.AddToClassList("text-sm");
        statsRow.Add(tierLabel);

        info.Add(statsRow);

        var salaryLabel = new Label(data.SalaryDisplay + "/mo");
        salaryLabel.AddToClassList("text-sm");
        salaryLabel.AddToClassList("text-muted");
        info.Add(salaryLabel);

        card.Add(info);

        // Action buttons
        var btnContainer = new VisualElement();
        btnContainer.style.flexDirection = FlexDirection.Column;
        btnContainer.style.marginLeft = 12;

        var acceptBtn = new Button { text = "Accept" };
        acceptBtn.AddToClassList("btn-primary");
        acceptBtn.AddToClassList("btn-sm");
        acceptBtn.style.marginBottom = 4;

        var capturedId = data.CandidateId;
        acceptBtn.clicked += () => {
            _viewModel?.AcceptCandidate(capturedId);
            RefreshAfterAction();
        };
        btnContainer.Add(acceptBtn);

        var declineBtn = new Button { text = "Decline" };
        declineBtn.AddToClassList("btn-danger");
        declineBtn.AddToClassList("btn-sm");
        declineBtn.clicked += () => {
            _viewModel?.DeclineCandidate(capturedId);
            RefreshAfterAction();
        };
        btnContainer.Add(declineBtn);

        card.Add(btnContainer);
        return card;
    }

    private void RefreshAfterAction()
    {
        // Immediately remove processed candidate from visual list;
        // The ViewModel's PendingCandidates will update on next full Refresh.
        if (_viewModel == null) return;

        // If no more pending, close modal automatically
        if (_viewModel.PendingCandidates.Count == 0)
            _viewModel.Dismiss();
    }

    private void OnCloseClicked()
    {
        _viewModel?.Dismiss();
    }
}
