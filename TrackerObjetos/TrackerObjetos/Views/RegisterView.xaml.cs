using TrackerObjetos.Services;

namespace TrackerObjetos.Views;

public partial class RegisterView : ContentPage
{
    public RegisterView()
    {
        InitializeComponent();
    }

    private async void OnRegisterClicked(object? sender, EventArgs e)
    {
        BtnRegister.IsEnabled = false;
        LblError.IsVisible = false;

        var usuario = UsuarioEntry.Text?.Trim() ?? "";
        var password = PasswordEntry.Text?.Trim() ?? "";
        var confirm = ConfirmEntry.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(password))
        {
            LblError.Text = "Complete todos los campos";
            LblError.IsVisible = true;
            BtnRegister.IsEnabled = true;
            return;
        }

        if (password != confirm)
        {
            LblError.Text = "Las contraseñas no coinciden";
            LblError.IsVisible = true;
            BtnRegister.IsEnabled = true;
            return;
        }

        if (password.Length < 4)
        {
            LblError.Text = "La contraseña debe tener al menos 4 caracteres";
            LblError.IsVisible = true;
            BtnRegister.IsEnabled = true;
            return;
        }

        var db = App.Current?.Handler?.MauiContext?.Services.GetService<DatabaseService>();
        if (db == null) return;

        if (await db.RegisterAsync(usuario, password))
        {
            if (Application.Current?.Windows.Count > 0)
            {
                Application.Current.Windows[0].Page = new AppShell();
            }
        }
        else
        {
            LblError.Text = "El usuario ya existe";
            LblError.IsVisible = true;
            BtnRegister.IsEnabled = true;
        }
    }
}
