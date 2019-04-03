class AnybusXGatewayModuleSelectorViewModel extends OneDasModuleSelectorViewModel
{
    constructor(moduleSet: OneDasModuleViewModel[])
    {
        super(OneDasModuleSelectorModeEnum.Duplex, moduleSet)

        this.SetMaxBytes(128)
    }

    public Update()
    {
        let totalMaxBytes: number
        let usedBytes: number
        let remainingBytes: number

        totalMaxBytes = this.MaxBytes() * 4
        usedBytes = this.ModuleSet().map(oneDasModule => oneDasModule.GetByteCount()).reduce((previousValue, currentValue) => previousValue + currentValue, 0)

        remainingBytes = (totalMaxBytes - usedBytes) % this.MaxBytes()

        if (remainingBytes === 0)
        {
            remainingBytes = this.MaxBytes()
        }

        if (usedBytes >= 512)
        {
            remainingBytes = 0
        }

        this.RemainingBytes(remainingBytes)
        this.RemainingCount(Math.floor(this.RemainingBytes() / ((this.NewModule().DataType() & 0x0FF) / 8)))
    }
}