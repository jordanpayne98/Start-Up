using UnityEngine.UIElements;

public class SellProductView : IGameView
{
    private readonly IModalPresenter _modal;
    private readonly ICommandDispatcher _dispatcher;
    private VisualElement _root;
    private SellProductViewModel _vm;

    private Label _productNameLabel;
    private Label _nicheLabel;
    private Label _fairValueLabel;
    private VisualElement _offerContainer;
    private ElementPool _offerPool;
    private Button _cancelButton;

    public SellProductView(IModalPresenter modal, ICommandDispatcher dispatcher) {
        _modal = modal;
        _dispatcher = dispatcher;
    }

    public void Initialize(VisualElement root) {
        _root = root;
        _root.AddToClassList("sell-product-modal");

        // Header
        var header = new VisualElement();
        header.AddToClassList("modal-header");
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.alignItems = Align.Center;

        var title = new Label("Sell Product");
        title.AddToClassList("text-xl");
        title.AddToClassList("text-bold");
        header.Add(title);

        _cancelButton = new Button { text = "X" };
        _cancelButton.AddToClassList("btn-sm");
        _cancelButton.style.minWidth = 30;
        header.Add(_cancelButton);
        _root.Add(header);

        // Product info
        var infoCard = new VisualElement();
        infoCard.AddToClassList("card");
        infoCard.style.marginBottom = 12;

        _productNameLabel = new Label();
        _productNameLabel.AddToClassList("text-lg");
        _productNameLabel.AddToClassList("text-bold");
        _productNameLabel.style.marginBottom = 4;
        infoCard.Add(_productNameLabel);

        var infoRow = new VisualElement();
        infoRow.style.flexDirection = FlexDirection.Row;
        infoRow.style.justifyContent = Justify.SpaceBetween;

        _nicheLabel = new Label();
        _nicheLabel.AddToClassList("text-sm");
        _nicheLabel.AddToClassList("text-muted");
        infoRow.Add(_nicheLabel);

        var fmvRow = new VisualElement();
        fmvRow.style.flexDirection = FlexDirection.Row;
        var fmvLabel = new Label("Fair Market Value: ");
        fmvLabel.AddToClassList("text-sm");
        fmvLabel.AddToClassList("text-muted");
        fmvRow.Add(fmvLabel);

        _fairValueLabel = new Label("--");
        _fairValueLabel.AddToClassList("text-sm");
        _fairValueLabel.AddToClassList("text-bold");
        fmvRow.Add(_fairValueLabel);
        infoRow.Add(fmvRow);

        infoCard.Add(infoRow);

        var bodyScroll = new ScrollView();
        bodyScroll.AddToClassList("modal-body");
        bodyScroll.style.flexGrow = 1;
        bodyScroll.style.flexShrink = 1;
        var body = bodyScroll.contentContainer;

        body.Add(infoCard);

        var offersTitle = new Label("Buyer Offers");
        offersTitle.AddToClassList("text-bold");
        offersTitle.style.marginBottom = 8;
        body.Add(offersTitle);

        _offerContainer = new VisualElement();
        _offerPool = new ElementPool(CreateOfferRow, _offerContainer);
        body.Add(_offerContainer);

        _root.Add(bodyScroll);

        var footer = new VisualElement();
        footer.AddToClassList("modal-footer");
        footer.style.justifyContent = Justify.FlexEnd;
        _root.Add(footer);

        _cancelButton.clicked += OnCancelClicked;
    }

    public void Bind(IViewModel viewModel) {
        _vm = viewModel as SellProductViewModel;
        if (_vm == null) return;

        _productNameLabel.text = _vm.ProductName;
        _nicheLabel.text = _vm.Niche;
        _fairValueLabel.text = _vm.FairMarketValue;
        _offerPool.UpdateList(_vm.Offers, BindOfferRow);
    }

    public void Dispose() {
        if (_cancelButton != null) _cancelButton.clicked -= OnCancelClicked;
        _offerPool = null;
        _vm = null;
    }

    private void OnCancelClicked() {
        _modal?.DismissModal();
    }

    private void OnAcceptOfferClicked(ClickEvent evt) {
        var el = evt.currentTarget as VisualElement;
        if (el == null || _vm == null) return;
        if (el.userData is CompetitorId buyerId) {
            _dispatcher?.Dispatch(new SellProductToCompetitorCommand(
                _dispatcher.CurrentTick, _vm.ProductId, buyerId));
            _modal?.DismissModal();
        }
    }

    private VisualElement CreateOfferRow() {
        var card = new VisualElement();
        card.AddToClassList("card");
        card.style.marginBottom = 8;
        card.style.flexDirection = FlexDirection.Row;
        card.style.justifyContent = Justify.SpaceBetween;
        card.style.alignItems = Align.Center;

        var left = new VisualElement();
        left.style.flexGrow = 1;

        var nameLabel = new Label();
        nameLabel.name = "buyer-name";
        nameLabel.AddToClassList("text-bold");
        nameLabel.AddToClassList("text-sm");
        left.Add(nameLabel);

        var detailRow = new VisualElement();
        detailRow.style.flexDirection = FlexDirection.Row;

        var nicheLabel = new Label();
        nicheLabel.name = "buyer-niche";
        nicheLabel.AddToClassList("text-sm");
        nicheLabel.AddToClassList("text-muted");
        nicheLabel.style.marginRight = 8;
        detailRow.Add(nicheLabel);

        var cashLabel = new Label();
        cashLabel.name = "buyer-cash";
        cashLabel.AddToClassList("text-sm");
        cashLabel.AddToClassList("text-muted");
        detailRow.Add(cashLabel);
        left.Add(detailRow);

        card.Add(left);

        var right = new VisualElement();
        right.style.flexDirection = FlexDirection.Row;
        right.style.alignItems = Align.Center;

        var offerLabel = new Label();
        offerLabel.name = "buyer-offer";
        offerLabel.AddToClassList("text-bold");
        offerLabel.style.marginRight = 12;
        right.Add(offerLabel);

        var pctLabel = new Label();
        pctLabel.name = "buyer-pct";
        pctLabel.AddToClassList("text-sm");
        pctLabel.AddToClassList("text-muted");
        pctLabel.style.marginRight = 12;
        right.Add(pctLabel);

        var acceptBtn = new Button { text = "Accept" };
        acceptBtn.name = "accept-btn";
        acceptBtn.AddToClassList("btn-primary");
        acceptBtn.AddToClassList("btn-sm");
        acceptBtn.RegisterCallback<ClickEvent>(OnAcceptOfferClicked);
        right.Add(acceptBtn);

        card.Add(right);
        return card;
    }

    private void BindOfferRow(VisualElement el, BuyerOfferVM data) {
        el.Q<Label>("buyer-name").text = data.CompanyName;
        el.Q<Label>("buyer-niche").text = data.NichePresence;
        el.Q<Label>("buyer-cash").text = "Cash: " + data.Cash;
        el.Q<Label>("buyer-offer").text = data.OfferPrice;
        el.Q<Label>("buyer-pct").text = data.OfferPercent;
        var btn = el.Q<Button>("accept-btn");
        if (btn != null) btn.userData = data.BuyerId;
    }
}
