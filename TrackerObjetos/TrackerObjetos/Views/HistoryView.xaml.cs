using TrackerObjetos.Models;
using TrackerObjetos.Services;

namespace TrackerObjetos.Views;

public partial class HistoryView : ContentPage
{
    public HistoryView()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadHistory();
    }

    private async Task LoadHistory()
    {
        var db = App.Current?.Handler?.MauiContext?.Services.GetService<DatabaseService>();
        if (db == null) return;

        if (!db.IsLoggedIn)
        {
            HistoryList.ItemsSource = null;
            return;
        }

        var records = await db.GetHistoryAsync();
        var items = records.Select(r => new HistoryItem
        {
            Id = r.Id,
            Titulo = r.Titulo,
            DetectedAt = r.DetectedAt,
            ThumbnailSource = r.Thumbnail != null
                ? ImageSource.FromStream(() => new MemoryStream(r.Thumbnail))
                : null
        }).ToList();

        HistoryList.ItemsSource = items;
    }

    private async void OnItemSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not HistoryItem item) return;

        var db = App.Current?.Handler?.MauiContext?.Services.GetService<DatabaseService>();
        if (db == null) return;

        var record = await db.GetDetectionByIdAsync(item.Id);
        if (record == null) return;

        WebSearchResult? webResult = null;
        if (!string.IsNullOrEmpty(record.WebTitle))
        {
            webResult = new WebSearchResult
            {
                Title = record.WebTitle,
                Extract = record.WebExtract ?? "",
                Source = "Wikipedia"
            };
        }

        var page = new DetalleView(record.Titulo, record.Descripcion, record.Thumbnail, webResult);
        await Navigation.PushModalAsync(page);
    }
}

public class HistoryItem
{
    public int Id { get; set; }
    public string Titulo { get; set; } = "";
    public DateTime DetectedAt { get; set; }
    public ImageSource? ThumbnailSource { get; set; }
}
