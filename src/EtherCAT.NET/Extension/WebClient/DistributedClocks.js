class DistributedClocksOpModeViewModel {
    constructor(model) {
        this.Name = model.Name;
        this.Description = model.Description;
        // Cyclic mode
        this.CycleTimeSyncUnit_IsEnabled = ko.observable(model.CycleTimeSyncUnit_IsEnabled);
        this.CycleTimeSyncUnit = ko.observable(model.CycleTimeSyncUnit);
        // SYNC 0
        this.CycleTimeSync0_IsEnabled = ko.observable(model.CycleTimeSync0_IsEnabled);
        this.CycleTimeSync0 = ko.observable(model.CycleTimeSync0);
        this.CycleTimeSync0_Factor = ko.observable(model.CycleTimeSync0_Factor);
        this.ShiftTimeSync0 = ko.observable(model.ShiftTimeSync0);
        this.ShiftTimeSync0_Factor = ko.observable(model.ShiftTimeSync0_Factor);
        this.ShiftTimeSync0_Input = ko.observable(model.ShiftTimeSync0_Input);
        // SYNC 1
        this.CycleTimeSync1_IsEnabled = ko.observable(model.CycleTimeSync1_IsEnabled);
        this.CycleTimeSync1 = ko.observable(model.CycleTimeSync1);
        this.CycleTimeSync1_Factor = ko.observable(model.CycleTimeSync1_Factor);
        this.ShiftTimeSync1 = ko.observable(model.ShiftTimeSync1);
        this.ShiftTimeSync1_Factor = ko.observable(model.ShiftTimeSync1_Factor);
        this.ShiftTimeSync1_Input = ko.observable(model.ShiftTimeSync1_Input);
        this.ShiftTimeSync1_Factor_UseSync0 = ko.pureComputed(() => this.ShiftTimeSync1_Factor() >= 0);
    }
}
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : new P(function (resolve) { resolve(result.value); }).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
let ViewModelConstructor = (model, identification) => new DistributedClocksViewModel(model, identification);
class DistributedClocksViewModel extends SlaveExtensionViewModelBase {
    constructor(model, identification) {
        super(model, identification);
        this.SelectedOpModeId = ko.observable(model.SelectedOpModeId);
        this.SelectedOpMode = ko.observable();
        this.OpModeSet = ko.observableArray([]);
    }
    // methods
    InitializeAsync() {
        return __awaiter(this, void 0, void 0, function* () {
            let actionResponse;
            let slaveInfoDynamicDataModel;
            let opModeModelSet;
            actionResponse = yield this.SendActionRequest(0, "GetOpModes", this.SlaveInfo.ToFlatModel());
            opModeModelSet = actionResponse.Data.$values;
            this.OpModeSet(opModeModelSet.map(opModeModel => new DistributedClocksOpModeViewModel(opModeModel)));
            this.SelectedOpMode(this.OpModeSet().find(opMode => opMode.Name === this.SelectedOpModeId()));
            this.SelectedOpMode.subscribe(newValue => {
                this.SelectedOpModeId(this.SelectedOpMode().Name);
            });
        });
    }
    ExtendModel(model) {
        super.ExtendModel(model);
        model.SelectedOpModeId = this.SelectedOpModeId();
    }
}
