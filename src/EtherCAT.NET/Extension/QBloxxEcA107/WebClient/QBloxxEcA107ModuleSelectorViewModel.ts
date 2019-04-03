class QBloxxEcA107ModuleSelectorViewModel extends OneDasModuleSelectorViewModel
{
    constructor(moduleSet: OneDasModuleViewModel[])
    {
        super(OneDasModuleSelectorModeEnum.Duplex, moduleSet)
    }

    protected CreateNewModule()
    {
        let oneDasModule: OneDasModuleViewModel

        oneDasModule = super.CreateNewModule()
        oneDasModule.MaxSize(1)

        return oneDasModule
    }

    public Update()
    {
        this.RemainingCount(4 - this.ModuleSet().length)
    }
}