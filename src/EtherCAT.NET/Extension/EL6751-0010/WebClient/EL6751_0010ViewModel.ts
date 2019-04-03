let ViewModelConstructor = (model: any, identification: ExtensionIdentificationViewModel) => new EL6731_0010ViewModel(model, identification)

class EL6731_0010ViewModel extends SlaveExtensionViewModelBase
{
    public StationNumber: KnockoutObservable<number>;
    public SelectedModule: KnockoutObservable<EL6731_0010ModuleEnum>;
    public SelectedModuleSet: KnockoutObservableArray<EL6731_0010ModuleEnum>;

    constructor(model, identification: ExtensionIdentificationViewModel)
    {
        super(model, identification)

        // slave out / master in / byte
        for (var i = 1; i <= 16; i++)
        {
            if (i < 10)
            {
                EnumerationHelper.Description["EL6731_0010ModuleEnum_SO_MI_Byte_0" + i] = "slave out / master in (" + i + "x byte)"
            }
            else
            {
                EnumerationHelper.Description["EL6731_0010ModuleEnum_SO_MI_Byte_" + i] = "slave out / master in (" + i + "x byte)"
            }
        }

        // slave in / master out / byte
        for (var i = 1; i <= 16; i++)
        {
            if (i < 10)
            {
                EnumerationHelper.Description["EL6731_0010ModuleEnum_SI_MO_Byte_0" + i] = "slave in / master out (" + i + "x byte)"
            }
            else
            {
                EnumerationHelper.Description["EL6731_0010ModuleEnum_SI_MO_Byte_" + i] = "slave in / master out (" + i + "x byte)"
            }
        }

        // slave out / master in / word
        for (var i = 1; i <= 64; i++)
        {
            if (i < 10)
            {
                EnumerationHelper.Description["EL6731_0010ModuleEnum_SO_MI_Word_0" + i] = "slave out / master in (" + i + "x word)"
            }
            else
            {
                EnumerationHelper.Description["EL6731_0010ModuleEnum_SO_MI_Word_" + i] = "slave out / master in (" + i + "x word)"
            }
        }

        // slave in / master out / word
        for (var i = 1; i <= 64; i++)
        {
            if (i < 10)
            {
                EnumerationHelper.Description["EL6731_0010ModuleEnum_SI_MO_Word_0" + i] = "slave in / master out (" + i + "x word)"
            }
            else
            {
                EnumerationHelper.Description["EL6731_0010ModuleEnum_SI_MO_Word_" + i] = "slave in / master out (" + i + "x word)"
            }
        }
    
        this.StationNumber = ko.observable<number>(model.StationNumber)
        this.SelectedModule = ko.observable<EL6731_0010ModuleEnum>()
        this.SelectedModuleSet = ko.observableArray<EL6731_0010ModuleEnum>(model.SelectedModuleSet)

        this.SelectedModule.subscribe(newValue =>
        {
            if (newValue)
            {
                this.SelectedModuleSet.push(newValue)
                this.SelectedModule(null)
            }
        })
    }

    // methods
    public async InitializeAsync()
    {
        //
    }

    public ExtendModel(model: any)
    {
        super.ExtendModel(model)

        model.StationNumber = this.StationNumber()
        model.SelectedModuleSet = this.SelectedModuleSet()
    }

    // commands
    public DeleteModule = (value: EL6731_0010ModuleEnum) =>
    {
        this.SelectedModuleSet.remove(value)
    }
}