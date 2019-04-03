let ViewModelConstructor = (model: any, identification: ExtensionIdentificationViewModel) => new EL6601ViewModel(model, identification)

class EL6601ViewModel extends SlaveExtensionViewModelBase
{
    constructor(model, identification: ExtensionIdentificationViewModel)
    {
        super(model, identification)
    }

    // methods
    public async InitializeAsync()
    {
        //
    }

    public ExtendModel(model: any)
    {
        super.ExtendModel(model)
    }
}