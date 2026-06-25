using TrackerObjetos.Services;

namespace TrackerObjetos.Views;

public partial class LoginView : ContentPage
{
    public LoginView()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        BtnLogin.IsEnabled = false;
        LblError.IsVisible = false;

        string usuario = UsuarioEntry.Text?.Trim() ?? "";
        string password = PasswordEntry.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(password))
        {
            LblError.Text = "Ingrese usuario y contraseña";
            LblError.IsVisible = true;
            BtnLogin.IsEnabled = true;
            return;
        }

        var db = App.Current?.Handler?.MauiContext?.Services.GetService<DatabaseService>();
        if (db == null) return;

        if (await db.LoginAsync(usuario, password))
        {
            if (Application.Current?.Windows.Count > 0)
            {
                Application.Current.Windows[0].Page = new AppShell();
            }
        }
        else
        {
            LblError.Text = "Usuario o contraseña incorrectos";
            LblError.IsVisible = true;
            BtnLogin.IsEnabled = true;
        }
    }

    private async void OnRegisterClicked(object? sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new RegisterView());
    }

    private async void OnSkipClicked(object? sender, EventArgs e)
    {
        if (Application.Current?.Windows.Count > 0)
        {
            Application.Current.Windows[0].Page = new AppShell();
        }
    }
}
