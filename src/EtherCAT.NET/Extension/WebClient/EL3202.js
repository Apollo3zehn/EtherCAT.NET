var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : new P(function (resolve) { resolve(result.value); }).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
let ViewModelConstructor = (model, identification) => new EL3202ViewModel(model, identification);
class EL3202ViewModel extends SlaveExtensionViewModelBase {
    constructor(model, identification) {
        super(model, identification);
        EnumerationHelper.Description["WiringModeEnum_Wire2"] = "2-wire";
        EnumerationHelper.Description["WiringModeEnum_Wire3"] = "3-wire";
        EnumerationHelper.Description["WiringModeEnum_Wire4"] = "4-wire";
        this.WiringMode = ko.observable(model.WiringMode);
    }
    // methods
    InitializeAsync() {
        return __awaiter(this, void 0, void 0, function* () {
            //
        });
    }
    ExtendModel(model) {
        super.ExtendModel(model);
        model.WiringMode = this.WiringMode();
    }
}
var WiringModeEnum;
(function (WiringModeEnum) {
    WiringModeEnum[WiringModeEnum["Wire2"] = 0] = "Wire2";
    WiringModeEnum[WiringModeEnum["Wire3"] = 1] = "Wire3";
    WiringModeEnum[WiringModeEnum["Wire4"] = 2] = "Wire4";
})(WiringModeEnum || (WiringModeEnum = {}));
window["WiringModeEnum"] = WiringModeEnum;
