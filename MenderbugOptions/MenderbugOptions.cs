using Modding;
using System;
using HutongGames.PlayMaker;
using Satchel.BetterMenus;

namespace MenderbugOptions
{

    public static class ModMenu
    {
        private static Menu? MenuRef;
        public static MenuScreen CreateModMenu(MenuScreen modlistmenu)
        {
            MenuRef ??= new Menu("Menderbug Options", new Element[]
            {
            BoolOption(
                "Menderbug Ignores Death",
                "Should Menderbug spawn even after dying?",
                b =>
                {
                    MenderbugOptionsMod.globalSettings.MenderbugNeverDies = b;
                },
                () => MenderbugOptionsMod.globalSettings.MenderbugNeverDies,
                Id:"NeverDies"),

            BoolOption(
                "Menderbug Ignores Sign",
                "Should Menderbug spawn even if the sign is fixed?",
                b =>
                {
                    MenderbugOptionsMod.globalSettings.MenderbugIgnoresSign = b;
                },
                () => MenderbugOptionsMod.globalSettings.MenderbugIgnoresSign,
                Id:"IgnoresSign"),

            new CustomSlider(
                "Spawn Chance (default: 2%)",
                f =>
                {
                    MenderbugOptionsMod.globalSettings.MenderbugSpawnChance = (int)f;
                },
                () => MenderbugOptionsMod.globalSettings.MenderbugSpawnChance,
                0f,
                100f,
                true,
                Id:"SpawnChance"),

            new MenuButton(
                "50% Spawn Chance",
                "Changes the spawn chance to 50%",
                submitAction =>
                {
                    MenderbugOptionsMod.globalSettings.MenderbugSpawnChance = 50;
                    MenuRef?.Update();
                }),

            new MenuButton(
                "Reset to defaults",
                "Resets all settings to their default values",
                submitAction =>
                {
                    MenderbugOptionsMod.globalSettings.MenderbugIgnoresSign = false;
                    MenderbugOptionsMod.globalSettings.MenderbugNeverDies = false;
                    MenderbugOptionsMod.globalSettings.MenderbugSpawnChance = 2;
                    MenuRef?.Update();
                })
            });

            return MenuRef.GetMenuScreen(modlistmenu);
        }
        public static HorizontalOption BoolOption(
            string name,
            string description,
            Action<bool> applySetting,
            Func<bool> loadSetting,
            string _true = "True",
            string _false = "False",
            string Id = "__UseName")
        {
            if (Id == "__UseName")
            {
                Id = name;
            }

            return new HorizontalOption(
                name,
                description,
                new[] { _true, _false },
                (i) => applySetting(i == 0),
                () => loadSetting() ? 0 : 1,
                Id
            );
        }
    }
    public class MenderbugOptionsMod : Mod, ICustomMenuMod, ILocalSettings<LocalSettings>, IGlobalSettings<GlobalSettings>
    {
        private static MenderbugOptionsMod? _instance;

        internal static MenderbugOptionsMod Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException($"An instance of {nameof(MenderbugOptionsMod)} was never constructed");
                }
                return _instance;
            }
        }
        public static LocalSettings localSettings { get; private set; } = new();
        public void OnLoadLocal(LocalSettings s) => localSettings = s;
        public LocalSettings OnSaveLocal() => localSettings;
        public static GlobalSettings globalSettings { get; private set; } = new();
        public void OnLoadGlobal(GlobalSettings s) => globalSettings = s;
        public GlobalSettings OnSaveGlobal() => globalSettings;
        public override string GetVersion() => GetType().Assembly.GetName().Version.ToString();

        public MenderbugOptionsMod() : base("MenderbugOptions")
        {
            _instance = this;
        }

        public override void Initialize()
        {
            Log("Initializing");

            On.HutongGames.PlayMaker.Actions.IntCompare.OnEnter += OnIntCompareAction;
            On.HutongGames.PlayMaker.Actions.RandomInt.OnEnter += OnRandomIntAction;
            On.HutongGames.PlayMaker.Actions.PlayerDataBoolTest.OnEnter += OnPlayerDataBoolTestAction;

            Log("Initialized");
        }

        private void OnPlayerDataBoolTestAction(On.HutongGames.PlayMaker.Actions.PlayerDataBoolTest.orig_OnEnter orig, HutongGames.PlayMaker.Actions.PlayerDataBoolTest self)
        {
            if (self.Fsm.FsmComponent.ActiveStateName == "Sign Broken?" && self.Fsm.FsmComponent.gameObject.name == "Mender Bug" && self.Fsm.FsmComponent.FsmName == "Mender Bug Ctrl")
            {
                self.isFalse = globalSettings.MenderbugIgnoresSign ? FsmEvent.GetFsmEvent("FINISHED") : FsmEvent.GetFsmEvent("DESTROY");
            }
            orig(self);
        }
        private void OnRandomIntAction(On.HutongGames.PlayMaker.Actions.RandomInt.orig_OnEnter orig, HutongGames.PlayMaker.Actions.RandomInt self)
        {
            if (self.Fsm.FsmComponent.ActiveStateName == "Chance" && self.Fsm.FsmComponent.gameObject.name == "Mender Bug" && self.Fsm.FsmComponent.FsmName == "Mender Bug Ctrl")
            {
                self.max = 100;
            }
            orig(self);
        }

        private void OnIntCompareAction(On.HutongGames.PlayMaker.Actions.IntCompare.orig_OnEnter orig, HutongGames.PlayMaker.Actions.IntCompare self)
        {
            if (self.Fsm.FsmComponent.ActiveStateName == "Dead?" && self.Fsm.FsmComponent.gameObject.name == "Mender Bug" && self.Fsm.FsmComponent.FsmName == "Mender Bug Ctrl")
            {
                self.equal = globalSettings.MenderbugNeverDies ? FsmEvent.GetFsmEvent("FINISHED") : FsmEvent.GetFsmEvent("DESTROY");
            }
            else if (self.Fsm.FsmComponent.ActiveStateName == "Chance" && self.Fsm.FsmComponent.gameObject.name == "Mender Bug" && self.Fsm.FsmComponent.FsmName == "Mender Bug Ctrl")
            {
                self.integer2 = 100 - globalSettings.MenderbugSpawnChance;
            }
            else if (self.Fsm.FsmComponent.ActiveStateName == "Mender Alive?" && self.Fsm.FsmComponent.gameObject.name == "Crossroads Sign Post" && self.Fsm.FsmComponent.FsmName == "Crossroads Sign Control")
            {
                self.equal = globalSettings.MenderbugNeverDies ? FsmEvent.GetFsmEvent("IS BROKEN") : FsmEvent.GetFsmEvent("PERMANENT");
            }
            orig(self);
        }

        public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates) => ModMenu.CreateModMenu(modListMenu);

        public bool ToggleButtonInsideMenu => false;
    }
    public class LocalSettings
    {
    }
    public class GlobalSettings
    {
        public bool MenderbugIgnoresSign = false;
        public bool MenderbugNeverDies = false;
        public int MenderbugSpawnChance = 2;
    }
}
