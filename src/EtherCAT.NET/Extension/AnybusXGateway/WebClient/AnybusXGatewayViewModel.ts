let ViewModelConstructor = (model: any, identification: ExtensionIdentificationViewModel) => new AnybusXGatewayViewModel(model, identification)

class AnybusXGatewayViewModel extends SlaveExtensionViewModelBase
{
    public OneDasModuleSelector: KnockoutObservable<OneDasModuleSelectorViewModel>

    constructor(model, identification: ExtensionIdentificationViewModel)
    {
        super(model, identification)

        this.OneDasModuleSelector = ko.observable<OneDasModuleSelectorViewModel>(new AnybusXGatewayModuleSelectorViewModel(model.ModuleSet.map(oneDasModuleModel => new OneDasModuleViewModel(oneDasModuleModel))))
    }

    public async InitializeAsync()
    {
        //
    }

    // methods
    public ExtendModel(model: any)
    {
        super.ExtendModel(model)

        model.ModuleSet = <OneDasModuleModel[]>this.OneDasModuleSelector().ModuleSet().map(moduleModel => moduleModel.ToModel())
    }
}