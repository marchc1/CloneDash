using Nucleus.Types;

namespace Nucleus.Core
{
    public class KeybindSystem {
        internal Dictionary<KeyboardKey, List<Keybind>> FinalKeybindAssociation { get; } = [];

        public Keybind AddKeybind(List<KeyboardKey> requiredKeys, Action bind) {
            Keybind ret = Keybind.Make(requiredKeys, bind);

            if (!FinalKeybindAssociation.ContainsKey(ret.FinalKey))
                FinalKeybindAssociation[ret.FinalKey] = [];
            FinalKeybindAssociation[ret.FinalKey].Add(ret);

            return ret;
        }

        public bool TestKeybinds(KeyboardState state) {
            bool ranKeybinds = false;

            foreach (var keybindFinal in FinalKeybindAssociation) {
                if (!state.KeyPressed(keybindFinal.Key))
                    continue;
                foreach (var keybind in keybindFinal.Value) {
                    //Need to check the state of every key.
                    bool Passed = true;
                    foreach (var k in keybind.RequiredKeys) {
                        if (!state.KeyDown(k)) {
                            Passed = false;
                            break; //move onto other keybinds
                        }
                    }

                    if (Passed) {
                        //Console.WriteLine($"Likely key bind {keybind.NiceKeybindString}, checking for any disturbances...");
                        //ok, every other key matches. need to make sure theres not a single key currently down that might interfere with this keybind
                        bool FinalCheck = true;
                        foreach (var keyDown in state.KeysPressed) {
                            bool isThisAKey = false;
                            foreach (var keyRequired in keybind.RequiredKeys) {
                                if (keyRequired.Key == keyDown) {
                                    isThisAKey = true;
                                }
                            }

                            //final just in case
                            if (keyDown == keybind.FinalKey.Key)
                                isThisAKey = true;

                            if (isThisAKey == false) {
                                FinalCheck = false;
                                break;
                            }
                        }

                        if (FinalCheck) {
                            Action bind;
                            keybind.Bind?.Invoke();
                            ranKeybinds = true;
                        }
                    }
                }
            }

            return ranKeybinds;
        }
    }

    public class Keybind {
        public List<KeyboardKey> RequiredKeys;
        public KeyboardKey FinalKey;
        public Action Bind;
        public string NiceKeybindString;

        internal Keybind() { }

        public static Keybind Make(List<KeyboardKey> requiredKeys, Action bind) {
            Keybind ret = new Keybind();

            ret.RequiredKeys = requiredKeys;
            ret.FinalKey = requiredKeys.Last();
            ret.Bind = bind;

            List<string> keyNames = [];
            foreach (KeyboardKey key in requiredKeys) {
                keyNames.Add(KeyboardLayout.USA.FromInt(key.Key).Name);
            }
            ret.NiceKeybindString = string.Join(" + ", keyNames);

            return ret;
        }        
    }
}
