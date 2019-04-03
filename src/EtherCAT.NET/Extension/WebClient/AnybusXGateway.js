class AnybusXGatewayModuleSelectorViewModel extends OneDasModuleSelectorViewModel {
    constructor(moduleSet) {
        super(OneDasModuleSelectorModeEnum.Duplex, moduleSet);
        this.SetMaxBytes(128);
    }
    Update() {
        let totalMaxBytes;
        let usedBytes;
        let remainingBytes;
        totalMaxBytes = this.MaxBytes() * 4;
        usedBytes = this.ModuleSet().map(oneDasModule => oneDasModule.GetByteCount()).reduce((previousValue, currentValue) => previousValue + currentValue, 0);
        remainingBytes = (totalMaxBytes - usedBytes) % this.MaxBytes();
        if (remainingBytes === 0) {
            remainingBytes = this.MaxBytes();
        }
        if (usedBytes >= 512) {
            remainingBytes = 0;
        }
        this.RemainingBytes(remainingBytes);
        this.RemainingCount(Math.floor(this.RemainingBytes() / ((this.NewModule().DataType() & 0x0FF) / 8)));
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
let ViewModelConstructor = (model, identification) => new AnybusXGatewayViewModel(model, identification);
class AnybusXGatewayViewModel extends SlaveExtensionViewModelBase {
    constructor(model, identification) {
        super(model, identification);
        this.OneDasModuleSelector = ko.observable(new AnybusXGatewayModuleSelectorViewModel(model.ModuleSet.map(oneDasModuleModel => new OneDasModuleViewModel(oneDasModuleModel))));
    }
    InitializeAsync() {
        return __awaiter(this, void 0, void 0, function* () {
            //
        });
    }
    // methods
    ExtendModel(model) {
        super.ExtendModel(model);
        model.ModuleSet = this.OneDasModuleSelector().ModuleSet().map(moduleModel => moduleModel.ToModel());
    }
}
