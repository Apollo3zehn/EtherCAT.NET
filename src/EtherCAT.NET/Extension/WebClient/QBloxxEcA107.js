class QBloxxEcA107ModuleSelectorViewModel extends OneDasModuleSelectorViewModel {
    constructor(moduleSet) {
        super(OneDasModuleSelectorModeEnum.Duplex, moduleSet);
    }
    CreateNewModule() {
        let oneDasModule;
        oneDasModule = super.CreateNewModule();
        oneDasModule.MaxSize(1);
        return oneDasModule;
    }
    Update() {
        this.RemainingCount(4 - this.ModuleSet().length);
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
let ViewModelConstructor = (model, identification) => new QBloxxEcA107ViewModel(model, identification);
class QBloxxEcA107ViewModel extends SlaveExtensionViewModelBase {
    constructor(model, identification) {
        let moduleSet;
        super(model, identification);
        moduleSet = model.ModuleSet.map(oneDasModuleModel => new OneDasModuleViewModel(oneDasModuleModel));
        moduleSet.forEach(oneDasModule => oneDasModule.MaxSize(1));
        this.OneDasModuleSelector = ko.observable(new QBloxxEcA107ModuleSelectorViewModel(moduleSet));
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
