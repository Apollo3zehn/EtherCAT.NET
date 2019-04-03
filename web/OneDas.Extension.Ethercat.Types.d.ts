/// <reference types="knockout" />
declare abstract class SlaveExtensionViewModelBase extends ExtensionViewModelBase {
    SlaveInfo: SlaveInfoViewModel;
}
declare class SlaveInfoDynamicDataViewModel {
    readonly Name: string;
    readonly Description: string;
    readonly Base64ImageData: ByteString;
    readonly PdoSet: SlavePdoViewModel[];
    constructor(slaveInfoDynamicDataModel: any, parent: SlaveInfoViewModel, dataGateway: DataGatewayViewModelBase);
}
declare class SlaveInfoViewModel {
    DynamicData: KnockoutObservable<SlaveInfoDynamicDataViewModel>;
    readonly Manufacturer: number;
    readonly ProductCode: number;
    readonly Revision: number;
    readonly Csa: number;
    readonly ChildSet: SlaveInfoViewModel[];
    private _slaveExtensionSet;
    private _oldCsa;
    private _slaveExtensionModelSet;
    constructor(slaveInfoModel: any);
    readonly SlaveExtensionSet: SlaveExtensionViewModelBase[];
    InitializeAsync(): Promise<void>;
    ToModel: () => any;
    GetOldCsa: () => number;
    ResetOldCsa: () => void;
    GetDescendants: (includeSelf: boolean) => SlaveInfoViewModel[];
    private InternalGetDescendants;
    GetVariables: () => SlaveVariableViewModel[];
    GetEthercatSlaveExtension: (index: number) => SlaveExtensionViewModelBase;
    ToFlatModel: () => {
        Manufacturer: number;
        ProductCode: number;
        Revision: number;
        OldCsa: number;
        Csa: number;
        ChildSet: any[];
        SlaveExtensionSet: any[];
    };
}
declare class SlavePdoViewModel {
    readonly Parent: SlaveInfoViewModel;
    readonly Name: string;
    readonly Index: number;
    readonly SyncManager: number;
    readonly VariableSet: SlaveVariableViewModel[];
    readonly CompactView: KnockoutObservable<boolean>;
    constructor(slavePdoModel: any, parent: SlaveInfoViewModel, dataGateway: DataGatewayViewModelBase);
}
declare class SlaveVariableViewModel extends DataPortViewModel {
    readonly Parent: SlavePdoViewModel;
    readonly Index: number;
    readonly SubIndex: number;
    constructor(slaveVariableModel: any, parent: SlavePdoViewModel, dataGateway: DataGatewayViewModelBase);
    GetId(): string;
}
