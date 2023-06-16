namespace CleverGirl.Menus {
    public class MenuComponents {
        public class Option {
            public string Name { set; get; }
            public char Hotkey { set; get; }
            public bool Locked { set; get; }
            public bool Selected { set; get; }

            public Option(string Name = "", char Hotkey = ' ', bool Locked = false, bool Selected = false) {
                this.Name = Name;
                this.Hotkey = Hotkey;
                this.Locked = Locked;
                this.Selected = Selected;
            }
        }
    }
}