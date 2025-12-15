using Sho2onMobile.Services;

namespace Sho2onMobile.Pages;

public partial class LoginPage : ContentPage
{
    AuthService auth = new AuthService();
    private readonly IDeviceService _deviceService;

    public LoginPage(IDeviceService deviceService)
    {
        InitializeComponent();
        _deviceService = deviceService;
    }

    private async void Login_Clicked(object sender, EventArgs e)
    {
        var deviceId = _deviceService.GetDeviceId();

        var user = await auth.LoginAsync(idField.Text, passwordField.Text, deviceId);

        if (user == null)
        {
            await DisplayAlert("Œÿ√", "»Ì«‰«  €Ì— ’ÕÌÕ…", " „«„");
            return;
        }

        await Navigation.PushAsync(new MainPage(user));
    }

    private void GoRegister_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new RegisterPage(_deviceService));
    }
}
