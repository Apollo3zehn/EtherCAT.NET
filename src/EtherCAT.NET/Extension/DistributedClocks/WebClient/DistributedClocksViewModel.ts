let ViewModelConstructor = (model: any, identification: ExtensionIdentificationViewModel) => new DistributedClocksViewModel(model, identification)

class DistributedClocksViewModel extends SlaveExtensionViewModelBase
{
    public SelectedOpModeId: KnockoutObservable<string>
    public SelectedOpMode: KnockoutObservable<DistributedClocksOpModeViewModel>
    public OpModeSet: KnockoutObservableArray<DistributedClocksOpModeViewModel>

    constructor(model, identification: ExtensionIdentificationViewModel)
    {
        super(model, identification)

        this.SelectedOpModeId = ko.observable<string>(model.SelectedOpModeId)
        this.SelectedOpMode = ko.observable<DistributedClocksOpModeViewModel>()
        this.OpModeSet = ko.observableArray<DistributedClocksOpModeViewModel>([])
    }

    // methods
    public async InitializeAsync()
    {
        let actionResponse: ActionResponse
        let slaveInfoDynamicDataModel: any
        let opModeModelSet: any[]

        actionResponse = await this.SendActionRequest(0, "GetOpModes", this.SlaveInfo.ToFlatModel())
        opModeModelSet = actionResponse.Data.$values

        this.OpModeSet(opModeModelSet.map(opModeModel => new DistributedClocksOpModeViewModel(opModeModel)) )
        this.SelectedOpMode(this.OpModeSet().find(opMode => opMode.Name === this.SelectedOpModeId()))

        this.SelectedOpMode.subscribe(newValue =>
        {
            this.SelectedOpModeId(this.SelectedOpMode().Name)
        })
    }

    public ExtendModel(model: any)
    {
        super.ExtendModel(model)

        model.SelectedOpModeId = this.SelectedOpModeId();
    }
}