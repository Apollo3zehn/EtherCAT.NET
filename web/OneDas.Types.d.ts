/// <reference types="knockout" />
declare enum DataDirectionEnum {
    Input = 1,
    Output = 2,
}
declare enum EndiannessEnum {
    LittleEndian = 1,
    BigEndian = 2,
}
declare enum FileGranularityEnum {
    Minute_1 = 60,
    Minute_10 = 600,
    Hour = 3600,
    Day = 86400,
}
declare enum OneDasDataTypeEnum {
    BOOLEAN = 8,
    UINT8 = 264,
    INT8 = 520,
    UINT16 = 272,
    INT16 = 528,
    UINT32 = 288,
    INT32 = 544,
    FLOAT32 = 800,
    FLOAT64 = 832,
}
declare enum OneDasStateEnum {
    Error = 1,
    Initialization = 2,
    Idle = 3,
    ApplyConfiguration = 4,
    Ready = 5,
    Run = 6,
}
declare enum SampleRateEnum {
    SampleRate_100 = 1,
    SampleRate_25 = 4,
    SampleRate_5 = 20,
    SampleRate_1 = 100,
}
declare class ActionRequest {
    readonly ExtensionId: string;
    readonly InstanceId: number;
    readonly MethodName: string;
    readonly Data: any;
    constructor(extensionId: string, instanceId: number, methodName: string, data: any);
}
declare class ActionResponse {
    Data: any;
    constructor(data: any);
}
declare class EventDispatcher<TSender, TArgs> implements IEvent<TSender, TArgs> {
    private _subscriptions;
    subscribe(fn: (sender: TSender, args: TArgs) => void): void;
    unsubscribe(fn: (sender: TSender, args: TArgs) => void): void;
    dispatch(sender: TSender, args: TArgs): void;
}
interface IEvent<TSender, TArgs> {
    subscribe(fn: (sender: TSender, args: TArgs) => void): void;
    unsubscribe(fn: (sender: TSender, args: TArgs) => void): void;
}
declare enum OneDasModuleSelectorModeEnum {
    Duplex = 1,
    InputOnly = 2,
    OutputOnly = 3,
}
declare class BufferRequestModel {
    SampleRate: SampleRateEnum;
    GroupFilter: string;
    constructor(sampleRate: SampleRateEnum, groupFilter: string);
}
declare class ChannelHubModel {
    Name: string;
    Group: string;
    DataType: OneDasDataTypeEnum;
    Guid: string;
    CreationDateTime: string;
    Unit: string;
    TransferFunctionSet: any[];
    AssociatedDataInputId: string;
    AssociatedDataOutputIdSet: string[];
    constructor(name: string, group: string, dataType: OneDasDataTypeEnum);
}
declare class OneDasModuleModel {
    DataType: OneDasDataTypeEnum;
    DataDirection: DataDirectionEnum;
    Endianness: EndiannessEnum;
    Size: number;
    constructor(dataType: OneDasDataTypeEnum, dataDirection: DataDirectionEnum, endianness: EndiannessEnum, size: number);
}
declare class TransferFunctionModel {
    DateTime: string;
    Type: string;
    Option: string;
    Argument: string;
    constructor(dateTime: string, type: string, option: string, argument: string);
}
declare var signalR: any;
declare class ConnectionManager {
    static WebClientHub: any;
    static Initialize(enableLogging: boolean): void;
    static InvokeWebClientHub: (methodName: string, ...args: any[]) => Promise<any>;
}
declare class EnumerationHelper {
    static Description: {
        [index: string]: string;
    };
    static GetEnumLocalization: (typeName: string, value: any) => any;
    static GetEnumValues: (typeName: string) => number[];
}
declare let ErrorMessage: {
    [index: string]: string;
};
declare class ObservableGroup<T> {
    Key: string;
    Members: KnockoutObservableArray<T>;
    constructor(key: string, members?: T[]);
}
declare function ObservableGroupBy<T>(list: T[], nameGetter: (x: T) => string, groupNameGetter: (x: T) => string, filter: string): ObservableGroup<T>[];
declare function AddToGroupedArray<T>(item: T, groupName: string, observableGroupSet: ObservableGroup<T>[]): void;
declare function MapMany<TArrayElement, TSelect>(array: TArrayElement[], mapFunc: (item: TArrayElement) => TSelect[]): TSelect[];
declare class Guid {
    static NewGuid(): string;
}
declare function delay(ms: number): Promise<{}>;
declare let CheckNamingConvention: (value: string) => {
    HasError: boolean;
    ErrorDescription: string;
};
declare class ExtensionFactory {
    static CreateExtensionViewModelAsync: (extensionType: string, extensionModel: any) => Promise<ExtensionViewModelBase>;
}
declare class ExtensionHive {
    static ExtensionIdentificationSet: Map<string, ExtensionIdentificationViewModel[]>;
    static Initialize: () => void;
    static FindExtensionIdentification: (extensionTypeName: string, extensionId: string) => ExtensionIdentificationViewModel;
}
declare class ChannelHubViewModel {
    Name: KnockoutObservable<string>;
    Group: KnockoutObservable<string>;
    readonly DataType: KnockoutObservable<OneDasDataTypeEnum>;
    readonly Guid: string;
    readonly CreationDateTime: string;
    readonly Unit: KnockoutObservable<string>;
    readonly TransferFunctionSet: KnockoutObservableArray<TransferFunctionViewModel>;
    SelectedTransferFunction: KnockoutObservable<TransferFunctionViewModel>;
    EvaluatedTransferFunctionSet: ((value: number) => number)[];
    IsSelected: KnockoutObservable<boolean>;
    readonly DataTypeSet: KnockoutObservableArray<OneDasDataTypeEnum>;
    readonly AssociatedDataInput: KnockoutObservable<DataPortViewModel>;
    readonly AssociatedDataOutputSet: KnockoutObservableArray<DataPortViewModel>;
    private AssociatedDataInputId;
    private AssociatedDataOutputIdSet;
    constructor(channelHubModel: ChannelHubModel);
    GetTransformedValue: (value: any) => number;
    private CreateDefaultTransferFunction;
    IsAssociationAllowed(dataPort: DataPortViewModel): boolean;
    UpdateAssociation: (dataPort: DataPortViewModel) => void;
    SetAssociation(dataPort: DataPortViewModel): void;
    ResetAssociation(maintainWeakReference: boolean, ...dataPortSet: DataPortViewModel[]): void;
    ResetAllAssociations(maintainWeakReference: boolean): void;
    GetAssociatedDataInputId: () => string;
    GetAssociatedDataOutputIdSet: () => string[];
    ToModel(): {
        Name: string;
        Group: string;
        DataType: OneDasDataTypeEnum;
        Guid: string;
        CreationDateTime: string;
        Unit: string;
        TransferFunctionSet: TransferFunctionModel[];
        AssociatedDataInputId: string;
        AssociatedDataOutputIdSet: string[];
    };
    AddTransferFunction: () => void;
    DeleteTransferFunction: () => void;
    NewTransferFunction: () => void;
    SelectTransferFunction: (transferFunction: TransferFunctionViewModel) => void;
}
declare class OneDasModuleViewModel {
    DataType: KnockoutObservable<OneDasDataTypeEnum>;
    DataDirection: KnockoutObservable<DataDirectionEnum>;
    Endianness: KnockoutObservable<EndiannessEnum>;
    Size: KnockoutObservable<number>;
    MaxSize: KnockoutObservable<number>;
    ErrorMessage: KnockoutObservable<string>;
    HasError: KnockoutComputed<boolean>;
    DataTypeSet: KnockoutObservableArray<OneDasDataTypeEnum>;
    private _onPropertyChanged;
    protected _model: any;
    constructor(oneDasModuleModel: OneDasModuleModel);
    readonly PropertyChanged: IEvent<OneDasModuleViewModel, any>;
    OnPropertyChanged: () => void;
    GetByteCount: (booleanBitSize?: number) => number;
    Validate(): void;
    ToString(): string;
    ExtendModel(model: any): void;
    ToModel(): any;
}
declare class OneDasModuleSelectorViewModel {
    SettingsTemplateName: KnockoutObservable<string>;
    NewModule: KnockoutObservable<OneDasModuleViewModel>;
    MaxBytes: KnockoutObservable<number>;
    RemainingBytes: KnockoutObservable<number>;
    RemainingCount: KnockoutObservable<number>;
    ModuleSet: KnockoutObservableArray<OneDasModuleViewModel>;
    ErrorMessage: KnockoutObservable<string>;
    HasError: KnockoutComputed<boolean>;
    OneDasModuleSelectorMode: KnockoutObservable<OneDasModuleSelectorModeEnum>;
    private _onModuleSetChanged;
    constructor(oneDasModuleSelectorMode: OneDasModuleSelectorModeEnum, moduleSet?: OneDasModuleViewModel[]);
    readonly OnModuleSetChanged: IEvent<OneDasModuleSelectorViewModel, OneDasModuleViewModel[]>;
    SetMaxBytes: (value: number) => void;
    GetInputModuleSet: () => OneDasModuleViewModel[];
    GetOutputModuleSet: () => OneDasModuleViewModel[];
    private InternalUpdate();
    protected Update(): void;
    protected Validate(): void;
    protected CreateNewModule(): OneDasModuleViewModel;
    private InternalCreateNewModule();
    private OnModulePropertyChanged;
    AddModule: () => void;
    DeleteModule: () => void;
}
declare class TransferFunctionViewModel {
    DateTime: KnockoutObservable<string>;
    Type: KnockoutObservable<string>;
    Option: KnockoutObservable<string>;
    Argument: KnockoutObservable<string>;
    constructor(transferFunctionModel: TransferFunctionModel);
    ToModel(): TransferFunctionModel;
}
declare class BufferRequestSelectorViewModel {
    NewBufferRequest: KnockoutObservable<BufferRequestViewModel>;
    BufferRequestSet: KnockoutObservableArray<BufferRequestViewModel>;
    ErrorMessage: KnockoutObservable<string>;
    HasError: KnockoutComputed<boolean>;
    private _onBufferRequestSetChanged;
    constructor(bufferRequestSet?: BufferRequestViewModel[]);
    readonly OnBufferRequestSetChanged: IEvent<BufferRequestSelectorViewModel, BufferRequestViewModel[]>;
    private InternalUpdate();
    protected Update(): void;
    protected Validate(): void;
    protected CreateNewBufferRequest(): BufferRequestViewModel;
    private InternalCreateNewBufferRequest();
    private OnBufferRequestPropertyChanged;
    AddBufferRequest: () => void;
    DeleteBufferRequest: () => void;
}
declare class BufferRequestViewModel {
    SampleRate: KnockoutObservable<SampleRateEnum>;
    GroupFilter: KnockoutObservable<string>;
    ErrorMessage: KnockoutObservable<string>;
    HasError: KnockoutComputed<boolean>;
    SampleRateSet: KnockoutObservableArray<SampleRateEnum>;
    private _onPropertyChanged;
    constructor(model: BufferRequestModel);
    readonly PropertyChanged: IEvent<BufferRequestViewModel, any>;
    OnPropertyChanged: () => void;
    Validate(): void;
    ToString(): string;
    ToModel(): any;
    private CheckGroupFilter(value);
}
declare class DataPortViewModel {
    Name: KnockoutObservable<string>;
    readonly DataType: OneDasDataTypeEnum;
    readonly DataDirection: DataDirectionEnum;
    readonly Endianness: EndiannessEnum;
    IsSelected: KnockoutObservable<boolean>;
    AssociatedChannelHubSet: KnockoutObservableArray<ChannelHubViewModel>;
    readonly AssociatedDataGateway: DataGatewayViewModelBase;
    readonly LiveDescription: KnockoutComputed<string>;
    constructor(dataPortModel: any, associatedDataGateway: DataGatewayViewModelBase);
    GetId(): string;
    ToFullQualifiedIdentifier(): string;
    ExtendModel(model: any): void;
    ToModel(): any;
    ResetAssociations(maintainWeakReference: boolean): void;
}
declare abstract class ExtensionViewModelBase {
    Description: ExtensionDescriptionViewModel;
    ExtensionIdentification: ExtensionIdentificationViewModel;
    IsInSettingsMode: KnockoutObservable<boolean>;
    private _model;
    constructor(extensionSettingsModel: any, extensionIdentification: ExtensionIdentificationViewModel);
    abstract InitializeAsync(): Promise<any>;
    SendActionRequest: (instanceId: number, methodName: string, data: any) => Promise<ActionResponse>;
    ExtendModel(model: any): void;
    ToModel(): any;
    EnableSettingsMode: () => void;
    DisableSettingsMode: () => void;
    ToggleSettingsMode: () => void;
}
declare abstract class DataGatewayViewModelBase extends ExtensionViewModelBase {
    readonly MaximumDatasetAge: KnockoutObservable<number>;
    readonly DataPortSet: KnockoutObservableArray<DataPortViewModel>;
    constructor(model: any, identification: ExtensionIdentificationViewModel);
    ExtendModel(model: any): void;
}
declare abstract class ExtendedDataGatewayViewModelBase extends DataGatewayViewModelBase {
    ModuleToDataPortMap: KnockoutObservableArray<ObservableGroup<DataPortViewModel>>;
    OneDasModuleSelector: KnockoutObservable<OneDasModuleSelectorViewModel>;
    constructor(model: any, identification: ExtensionIdentificationViewModel, oneDasModuleSelector: OneDasModuleSelectorViewModel);
    InitializeAsync(): Promise<void>;
    UpdateDataPortSet(): void;
    CreateDataPortSet(oneDasModule: OneDasModuleViewModel, index: number): DataPortViewModel[];
}
declare abstract class DataWriterViewModelBase extends ExtensionViewModelBase {
    readonly FileGranularity: KnockoutObservable<FileGranularityEnum>;
    readonly BufferRequestSet: KnockoutObservableArray<BufferRequestViewModel>;
    readonly BufferRequestSelector: KnockoutObservable<BufferRequestSelectorViewModel>;
    constructor(model: any, identification: ExtensionIdentificationViewModel);
    ExtendModel(model: any): void;
}
declare class ExtensionDescriptionViewModel {
    ProductVersion: number;
    Id: string;
    InstanceId: number;
    InstanceName: KnockoutObservable<string>;
    IsEnabled: KnockoutObservable<boolean>;
    constructor(extensionDescriptionModel: any);
    ToModel(): any;
}
declare class ExtensionIdentificationViewModel {
    ProductVersion: string;
    Id: string;
    Name: string;
    Description: string;
    ViewResourceName: string;
    ViewModelResourceName: string;
    constructor(extensionIdentificationModel: any);
}
