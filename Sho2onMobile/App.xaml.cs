namespace Sho2onMobile
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }



        protected override Window CreateWindow(IActivationState? activationState)
        {
            //return new Window(new ContentPage { Content = new Label { Text = "Test Page Loaded" } });

            return new Window(new AppShell());
        }
    }
}