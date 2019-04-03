let ViewModelConstructor = (model: any, identification: ExtensionIdentificationViewModel) => new EL3202ViewModel(model, identification)

class EL3202ViewModel extends SlaveExtensionViewModelBase
{
    public WiringMode: KnockoutObservable<WiringModeEnum>

    constructor(model, identification: ExtensionIdentificationViewModel)
    {
        super(model, identification)

        EnumerationHelper.Description["WiringModeEnum_Wire2"] = "2-wire"
        EnumerationHelper.Description["WiringModeEnum_Wire3"] = "3-wire"
        EnumerationHelper.Description["WiringModeEnum_Wire4"] = "4-wire"

        this.WiringMode = ko.observable<WiringModeEnum>(model.WiringMode);
    }

    // methods
    public async InitializeAsync()
    {
        //
    }

    public ExtendModel(model: any)
    {
        super.ExtendModel(model)

        model.WiringMode = <WiringModeEnum>this.WiringMode()
    }
}