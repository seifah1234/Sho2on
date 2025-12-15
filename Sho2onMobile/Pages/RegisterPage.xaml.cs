using Sho2onMobile.Services;
using System.Text.RegularExpressions;
using Microsoft.Maui.Controls;

namespace Sho2onMobile.Pages;

public partial class RegisterPage : ContentPage
{
    AuthService auth = new AuthService();
    private readonly IDeviceService _deviceService;

    public RegisterPage(IDeviceService deviceService)
    {
        InitializeComponent();
        _deviceService = deviceService;

        // ≈⁄œ«œ «·√Õœ«À ·· Õﬁﬁ √À‰«¡ «·ﬂ «»…
        SetupValidationEvents();
    }

    private void SetupValidationEvents()
    {
        //  Õﬁﬁ „‰ «·ﬂÊœ «·Êÿ‰Ì √À‰«¡ «·ﬂ «»…
        nationalIdField.TextChanged += OnNationalIdTextChanged;

        //  Õﬁﬁ „‰ ﬂ·„… «·„—Ê— √À‰«¡ «·ﬂ «»…
        passwordField.TextChanged += OnPasswordTextChanged;

        //  Õﬁﬁ „‰  √ﬂÌœ ﬂ·„… «·„—Ê— √À‰«¡ «·ﬂ «»…
        confirmPasswordField.TextChanged += OnConfirmPasswordTextChanged;

        // ≈Œ›«¡ „ƒ‘— ﬁÊ… ﬂ·„… «·„—Ê— ›Ì «·»œ«Ì…
        passwordStrengthContainer.IsVisible = false;
    }

    private void OnNationalIdTextChanged(object sender, TextChangedEventArgs e)
    {
        ValidateNationalId();
    }

    private void OnPasswordTextChanged(object sender, TextChangedEventArgs e)
    {
        ValidatePassword();

        // ⁄—÷ „ƒ‘— ﬁÊ… ﬂ·„… «·„—Ê— ›ﬁÿ ≈–« ﬂ«‰ Â‰«ﬂ ‰’
        passwordStrengthContainer.IsVisible = !string.IsNullOrEmpty(passwordField.Text);

        // «· Õﬁﬁ „‰  ÿ«»ﬁ ﬂ·„«  «·„—Ê—
        ValidateConfirmPassword();
    }

    private void OnConfirmPasswordTextChanged(object sender, TextChangedEventArgs e)
    {
        ValidateConfirmPassword();
    }

    private bool ValidateNationalId()
    {
        if (string.IsNullOrWhiteSpace(nationalIdField.Text))
        {
            nationalIdValidation.Text = "«·ﬂÊœ «·Êÿ‰Ì „ÿ·Ê»";
            nationalIdValidation.IsVisible = true;
            return false;
        }

        //  Õﬁﬁ „‰ √‰ «·ﬂÊœ ÌÕ ÊÌ ⁄·Ï √—ﬁ«„ ›ﬁÿ
        if (!Regex.IsMatch(nationalIdField.Text, @"^\d+$"))
        {
            nationalIdValidation.Text = "«·ﬂÊœ «·Êÿ‰Ì ÌÃ» √‰ ÌÕ ÊÌ ⁄·Ï √—ﬁ«„ ›ﬁÿ";
            nationalIdValidation.IsVisible = true;
            return false;
        }

        //  Õﬁﬁ „‰ ÿÊ· «·ﬂÊœ (Ì„ﬂ‰  ⁄œÌ·Â Õ”» «·„ ÿ·»« )
        if (nationalIdField.Text.Length < 5)
        {
            nationalIdValidation.Text = "«·ﬂÊœ «·Êÿ‰Ì ÌÃ» √‰ ÌﬂÊ‰ 5 √—ﬁ«„ ⁄·Ï «·√ﬁ·";
            nationalIdValidation.IsVisible = true;
            return false;
        }

        nationalIdValidation.IsVisible = false;
        return true;
    }

    private bool ValidatePassword()
    {
        if (string.IsNullOrWhiteSpace(passwordField.Text))
        {
            // ≈Œ›«¡ „ƒ‘— «·ﬁÊ…
            passwordStrengthContainer.IsVisible = false;
            return false;
        }

        var password = passwordField.Text;

        // Õ”«» ﬁÊ… ﬂ·„… «·„—Ê—
        int strength = 0;

        // «·ÿÊ·
        if (password.Length >= 8) strength++;
        if (password.Length >= 12) strength++;

        //  Õ ÊÌ ⁄·Ï √—ﬁ«„
        if (Regex.IsMatch(password, @"\d")) strength++;

        //  Õ ÊÌ ⁄·Ï √Õ—› ﬂ»Ì—… Ê’€Ì—…
        if (Regex.IsMatch(password, @"[A-Z]") && Regex.IsMatch(password, @"[a-z]")) strength++;

        //  Õ ÊÌ ⁄·Ï —„Ê“ Œ«’…
        if (Regex.IsMatch(password, @"[!@#$%^&*(),.?""':{}|<>]")) strength++;

        //  ÕœÌÀ „ƒ‘— «·ﬁÊ…
        UpdatePasswordStrengthIndicator(strength);

        // «· Õﬁﬁ „‰ «·„ ÿ·»«  «·√”«”Ì…
        if (password.Length < 8)
        {
            passwordStrengthLabel.Text = "ﬂ·„… «·„—Ê— ﬁ’Ì—… (8 √Õ—› ⁄·Ï «·√ﬁ·)";
            passwordStrengthLabel.TextColor = Color.FromArgb("#E74C3C");
            return false;
        }

        if (!Regex.IsMatch(password, @"\d"))
        {
            passwordStrengthLabel.Text = "ÌÃ» √‰  Õ ÊÌ ⁄·Ï √—ﬁ«„";
            passwordStrengthLabel.TextColor = Color.FromArgb("#E74C3C");
            return false;
        }

        return true;
    }

    private void UpdatePasswordStrengthIndicator(int strength)
    {
        // ≈⁄«œ…  ⁄ÌÌ‰ «·√·Ê«‰
        strengthBar1.Color = Color.FromArgb("#E0E0E0");
        strengthBar2.Color = Color.FromArgb("#E0E0E0");
        strengthBar3.Color = Color.FromArgb("#E0E0E0");
        strengthBar4.Color = Color.FromArgb("#E0E0E0");

        string strengthText = "";
        Color strengthColor = Colors.Gray;

        switch (strength)
        {
            case 0:
            case 1:
                strengthBar1.Color = Color.FromArgb("#E74C3C");
                strengthText = "÷⁄Ì›…";
                strengthColor = Color.FromArgb("#E74C3C");
                break;

            case 2:
                strengthBar1.Color = Color.FromArgb("#F39C12");
                strengthBar2.Color = Color.FromArgb("#F39C12");
                strengthText = "„ Ê”ÿ…";
                strengthColor = Color.FromArgb("#F39C12");
                break;

            case 3:
                strengthBar1.Color = Color.FromArgb("#F1C40F");
                strengthBar2.Color = Color.FromArgb("#F1C40F");
                strengthBar3.Color = Color.FromArgb("#F1C40F");
                strengthText = "ÃÌœ…";
                strengthColor = Color.FromArgb("#F1C40F");
                break;

            case 4:
            case 5:
                strengthBar1.Color = Color.FromArgb("#27AE60");
                strengthBar2.Color = Color.FromArgb("#27AE60");
                strengthBar3.Color = Color.FromArgb("#27AE60");
                strengthBar4.Color = Color.FromArgb("#27AE60");
                strengthText = "ﬁÊÌ…";
                strengthColor = Color.FromArgb("#27AE60");
                break;
        }

        passwordStrengthLabel.Text = strengthText;
        passwordStrengthLabel.TextColor = strengthColor;
    }

    private bool ValidateConfirmPassword()
    {
        if (string.IsNullOrWhiteSpace(confirmPasswordField.Text))
        {
            confirmPasswordValidation.IsVisible = false;
            return false;
        }

        if (passwordField.Text != confirmPasswordField.Text)
        {
            confirmPasswordValidation.Text = "ﬂ·„«  «·„—Ê— €Ì— „ ÿ«»ﬁ…";
            confirmPasswordValidation.IsVisible = true;
            return false;
        }

        confirmPasswordValidation.IsVisible = false;
        return true;
    }

    private async void Register_Clicked(object sender, EventArgs e)
    {
        // «· Õﬁﬁ „‰ Ã„Ì⁄ «·ÕﬁÊ·
        bool isValid = true;

        if (!ValidateNationalId()) isValid = false;
        if (!ValidatePassword()) isValid = false;
        if (!ValidateConfirmPassword()) isValid = false;

        if (!isValid)
        {
            await DisplayAlert(" ‰»ÌÂ", "Ì—ÃÏ  ’ÕÌÕ «·√Œÿ«¡ ›Ì «·‰„Ê–Ã", "„Ê«›ﬁ");
            return;
        }

        // ⁄—÷ „ƒ‘—  Õ„Ì·
        var loadingTask = DisplayAlert("Ã«—Ì «·„⁄«·Ã…", "Ã«—Ì ≈‰‘«¡ «·Õ”«»...", null);

        string mac = _deviceService.GetDeviceId();

        try
        {
            var result = await auth.RegisterAsync(nationalIdField.Text, passwordField.Text, mac);

            await loadingTask;

            if (result == "success")
            {
                await DisplayAlert("‰Ã«Õ", "?  „ ≈‰‘«¡ «·Õ”«» »‰Ã«Õ", "„Ê«›ﬁ");
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlert("Œÿ√", $"? {result}", "„Ê«›ﬁ");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Œÿ√", $"? ÕœÀ Œÿ√: {ex.Message}", "„Ê«›ﬁ");
        }
    }

    private async void OnTermsClicked(object sender, EventArgs e)
    {
        await DisplayAlert("«·‘—Êÿ Ê«·√Õﬂ«„",
            "1. ÌÃ» √‰  ﬂÊ‰ «·»Ì«‰«  «·„ﬁœ„… ’ÕÌÕ… ÊœﬁÌﬁ….\n" +
            "2. «·„” Œœ„ „”ƒÊ· ⁄‰ «·Õ›«Ÿ ⁄·Ï ”—Ì… ﬂ·„… «·„—Ê—.\n" +
            "3. ÌÕﬁ ··≈œ«—…  ⁄ÿÌ· «·Õ”«» ›Ì Õ«·… «·„Œ«·›….\n" +
            "4. ÌÃ» «·«· “«„ »„Ê«⁄Ìœ «·Õ÷Ê— Ê«·«‰’—«›.\n" +
            "5. ÌÕﬁ ·· ÿ»Ìﬁ Ã„⁄ Ê«” Œœ«„ «·»Ì«‰«  ·√€—«÷ «·≈Õ’«¡ Ê«· Õ”Ì‰.",
            "„Ê«›ﬁ");
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        //  √ﬂÌœ «· ‰ﬁ· ≈–« ﬂ«‰ Â‰«ﬂ »Ì«‰«  €Ì— „Õ›ÊŸ…
        if (!string.IsNullOrWhiteSpace(nationalIdField.Text) ||
            !string.IsNullOrWhiteSpace(passwordField.Text))
        {
            bool confirm = await DisplayAlert(" ‰»ÌÂ",
                "Â·  —Ìœ «·„€«œ—…ø «·»Ì«‰«  «·„œŒ·… ﬁœ  ›ﬁœ.",
                "‰⁄„", "·«");

            if (!confirm) return;
        }

        await Navigation.PushAsync(new LoginPage(_deviceService));
    }

    // œ«·… „”«⁄œ… ·· ‰ﬁ· ≈·Ï ’›Õ…  ”ÃÌ· «·œŒÊ·
    private async void Button_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new LoginPage(_deviceService));
    }
}