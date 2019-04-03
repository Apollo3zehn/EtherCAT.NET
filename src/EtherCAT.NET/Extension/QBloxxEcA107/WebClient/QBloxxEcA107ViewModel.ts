let ViewModelConstructor = (model: any, identification: ExtensionIdentificationViewModel) => new QBloxxEcA107ViewModel(model, identification)

class QBloxxEcA107ViewModel extends SlaveExtensionViewModelBase
{
    public OneDasModuleSelector: KnockoutObservable<OneDasModuleSelectorViewModel>

    constructor(model, identification: ExtensionIdentificationViewModel)
    {
        let moduleSet: OneDasModuleViewModel[]

        super(model, identification)

        moduleSet = model.ModuleSet.map(oneDasModuleModel => new OneDasModuleViewModel(oneDasModuleModel))
        moduleSet.forEach(oneDasModule => oneDasModule.MaxSize(1))

        this.OneDasModuleSelector = ko.observable<OneDasModuleSelectorViewModel>(new QBloxxEcA107ModuleSelectorViewModel(moduleSet))
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