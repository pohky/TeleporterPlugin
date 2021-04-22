namespace TeleporterPlugin.Gui {
    public abstract class Window {
        protected bool WindowVisible;
        public virtual bool Visible {
            get => WindowVisible;
            set => WindowVisible = value;
        }

        protected TeleporterPlugin Plugin { get; }

        protected Window(TeleporterPlugin plugin) {
            Plugin = plugin;
        }

        public void Draw() {
            if(Visible) 
                DrawUi();
        }

        protected abstract void DrawUi();
    }
}