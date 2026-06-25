using Camera.MAUI;
using TrackerObjetos.Services;

namespace TrackerObjetos;

public partial class MainPage : ContentPage
{
    private AnthropicService? _anthropicService;
    private WebSearchService? _webSearchService;
    private bool _cameraStarted;
    private bool _isCapturing;

    public MainPage()
    {
        InitializeComponent();
        CamaraPreview.CamerasLoaded += OnCamerasLoaded;
#if ANDROID
        CamaraPreview.HandlerChanged += OnCameraHandlerChanged;
#endif
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
            status = await Permissions.RequestAsync<Permissions.Camera>();

        if (status != PermissionStatus.Granted)
        {
            await DisplayAlertAsync("Permiso", "Se necesita acceso a la cámara", "OK");
            return;
        }

        _anthropicService ??= new AnthropicService();
        _webSearchService ??= new WebSearchService();

        if (SnapshotImage.IsVisible)
        {
            SnapshotImage.IsVisible = false;
            SnapshotImage.Source = null;
            CamaraPreview.IsVisible = true;
            _isCapturing = false;
            SetLoadingState(false);
        }

        TryStartCamera();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (CamaraPreview != null)
        {
            _ = CamaraPreview.StopCameraAsync();
            _cameraStarted = false;
        }
    }

    private void OnCamerasLoaded(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(TryStartCamera);
    }

#if ANDROID
    private void OnCameraHandlerChanged(object? sender, EventArgs e)
    {
        try
        {
            var platformView = CamaraPreview.Handler?.PlatformView;
            if (platformView == null) return;

            var previewView = FindPreviewView((platformView as Android.Views.ViewGroup)!);
            if (previewView != null)
            {
                var method = previewView.GetType().GetMethod("setScaleType",
                    new[] { typeof(int) });
                if (method != null)
                {
                    method.Invoke(previewView, new object[] { 2 });
                }
            }
        }
        catch
        {
            // Scale type adjustment failed silently
        }
    }

     private static Android.Views.View? FindPreviewView(Android.Views.ViewGroup? group)
    {
        if (group == null) return null;
        for (int i = 0; i < group.ChildCount; i++)
        {
            var child = group.GetChildAt(i);
            if (child != null && child.GetType().Name == "PreviewView") return child;
            if (child is Android.Views.ViewGroup childGroup)
            {
                var found = FindPreviewView(childGroup);
                if (found != null) return found;
            }
        }
        return null;
    }
#endif

    private async void TryStartCamera()
    {
        if (_cameraStarted) return;

        if (CamaraPreview.Cameras.Count > 0)
        {
            CamaraPreview.Camera = CamaraPreview.Cameras[0];
            CamaraPreview.ZoomFactor = CamaraPreview.MinZoomFactor;

            await CamaraPreview.StartCameraAsync(new Size(1920, 1080));
            _cameraStarted = true;
        }
    }

    private async void OnIdentificarClicked(object? sender, EventArgs e)
    {
        if (_isCapturing || _anthropicService == null) return;
        _isCapturing = true;

        SetLoadingState(true);

        try
        {
            var imgSource = CamaraPreview.GetSnapShot(Camera.MAUI.ImageFormat.JPEG);
            if (imgSource == null)
            {
                await DisplayAlertAsync("Error", "No se pudo capturar la imagen", "OK");
                RestoreCamera();
                return;
            }

            if (imgSource is not StreamImageSource streamSource)
            {
                await DisplayAlertAsync("Error", "Formato de imagen no válido", "OK");
                RestoreCamera();
                return;
            }

            var stream = await streamSource.Stream(CancellationToken.None);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var bytes = ms.ToArray();

            if (bytes.Length < 10000)
            {
                await DisplayAlertAsync("Error", "Imagen muy pequeña, asegúrese de tener buena iluminación", "OK");
                RestoreCamera();
                return;
            }

            // Freeze snapshot: hide camera, show captured image
            ms.Position = 0;
            SnapshotImage.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
            SnapshotImage.IsVisible = true;
            CamaraPreview.IsVisible = false;

            var descripcion = await _anthropicService.IdentifyObjectAsync(bytes);

            // Parse "Nombre | TIPO: x | ..."
            string titulo = "Objeto";
            string descripcionCompleta = descripcion;

            var pipeIndex = descripcion.IndexOf('|');
            if (pipeIndex > 0)
            {
                titulo = descripcion.Substring(0, pipeIndex).Trim()
                    .Trim('\'', '"', '*', '-', ' ', '\n', '\r');
                descripcionCompleta = descripcion.Substring(pipeIndex + 1).Trim()
                    .Trim('\'', '"', '*', '-', ' ', '\n', '\r');
            }

            // Search Wikipedia for the identified object
            var webResult = await _webSearchService!.SearchObjectAsync(titulo);

            SetLoadingState(false);

            var page = new Views.DetalleView(titulo, descripcionCompleta, bytes, webResult);
            await Navigation.PushModalAsync(page);

            // When user returns from modal, OnAppearing restores camera
        }
        catch (HttpRequestException ex)
        {
            RestoreCamera();
            await DisplayAlertAsync("Error de conexión", $"No se pudo contactar a Claude: {ex.Message}", "OK");
        }
        catch (TaskCanceledException)
        {
            RestoreCamera();
            await DisplayAlertAsync("Tiempo de espera", "La solicitud tardó demasiado. Verifica tu conexión a internet.", "OK");
        }
        catch (Exception ex)
        {
            RestoreCamera();
            await DisplayAlertAsync("Error", $"Ocurrió un error: {ex.Message}", "OK");
        }
    }

    private void RestoreCamera()
    {
        SnapshotImage.IsVisible = false;
        SnapshotImage.Source = null;
        CamaraPreview.IsVisible = true;
        SetLoadingState(false);
        _isCapturing = false;
    }

    private void SetLoadingState(bool loading)
    {
        LoadingIndicator.IsRunning = loading;
        LoadingIndicator.IsVisible = loading;
        BtnIdentificar.IsEnabled = !loading;
        BtnIdentificar.Text = loading ? "Identificando..." : "Identificar objeto";
        LblInstruccion.Text = loading ? "Analizando imagen con IA..." : "Apunte la cámara al objeto y presione Identificar";
    }

    private void OnLogoutClicked(object? sender, EventArgs e)
    {
        if (CamaraPreview != null)
        {
            _ = CamaraPreview.StopCameraAsync();
            _cameraStarted = false;
        }
        if (Application.Current?.Windows.Count > 0)
        {
            Application.Current.Windows[0].Page = new Views.LoginView();
        }
    }
}
