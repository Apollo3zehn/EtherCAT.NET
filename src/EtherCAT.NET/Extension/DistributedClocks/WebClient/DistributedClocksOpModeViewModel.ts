class DistributedClocksOpModeViewModel
{
    public Name: string
    public Description: string

    public CycleTimeSyncUnit_IsEnabled: KnockoutObservable<boolean>
    public CycleTimeSyncUnit: KnockoutObservable<number>

    public CycleTimeSync0_IsEnabled: KnockoutObservable<boolean>
    public CycleTimeSync0: KnockoutObservable<number>
    public CycleTimeSync0_Factor: KnockoutObservable<number>
    public ShiftTimeSync0: KnockoutObservable<number>
    public ShiftTimeSync0_Factor: KnockoutObservable<number>
    public ShiftTimeSync0_Input: KnockoutObservable<boolean>

    public CycleTimeSync1_IsEnabled: KnockoutObservable<boolean>
    public CycleTimeSync1: KnockoutObservable<number>
    public CycleTimeSync1_Factor: KnockoutObservable<number>
    public ShiftTimeSync1: KnockoutObservable<number>
    public ShiftTimeSync1_Factor: KnockoutObservable<number>
    public ShiftTimeSync1_Input: KnockoutObservable<boolean>

    public ShiftTimeSync1_Factor_UseSync0: KnockoutObservable<boolean>

    constructor(model)
    {
        this.Name = model.Name
        this.Description = model.Description

        // Cyclic mode
        this.CycleTimeSyncUnit_IsEnabled = ko.observable<boolean>(model.CycleTimeSyncUnit_IsEnabled);
        this.CycleTimeSyncUnit = ko.observable<number>(model.CycleTimeSyncUnit);

        // SYNC 0
        this.CycleTimeSync0_IsEnabled = ko.observable<boolean>(model.CycleTimeSync0_IsEnabled);
        this.CycleTimeSync0 = ko.observable<number>(model.CycleTimeSync0);
        this.CycleTimeSync0_Factor = ko.observable<number>(model.CycleTimeSync0_Factor);
        this.ShiftTimeSync0 = ko.observable<number>(model.ShiftTimeSync0);
        this.ShiftTimeSync0_Factor = ko.observable<number>(model.ShiftTimeSync0_Factor);
        this.ShiftTimeSync0_Input = ko.observable<boolean>(model.ShiftTimeSync0_Input);

        // SYNC 1
        this.CycleTimeSync1_IsEnabled = ko.observable<boolean>(model.CycleTimeSync1_IsEnabled);
        this.CycleTimeSync1 = ko.observable<number>(model.CycleTimeSync1);
        this.CycleTimeSync1_Factor = ko.observable<number>(model.CycleTimeSync1_Factor);
        this.ShiftTimeSync1 = ko.observable<number>(model.ShiftTimeSync1);
        this.ShiftTimeSync1_Factor = ko.observable<number>(model.ShiftTimeSync1_Factor);
        this.ShiftTimeSync1_Input = ko.observable<boolean>(model.ShiftTimeSync1_Input);

        this.ShiftTimeSync1_Factor_UseSync0 = ko.pureComputed(() => this.ShiftTimeSync1_Factor() >= 0)
    }
}