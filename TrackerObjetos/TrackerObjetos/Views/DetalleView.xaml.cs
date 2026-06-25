using TrackerObjetos.Services;

namespace TrackerObjetos.Views;

public partial class DetalleView : ContentPage
{
    private readonly string _titulo;
    private static readonly string[] KnownFields =
        ["TIPO", "COLOR", "MATERIAL", "MARCA", "MODELO", "PROPÓSITO", "DESCRIPCIÓN"];

    public DetalleView(string titulo, string descripcionCompleta, byte[]? imageBytes = null, WebSearchResult? webResult = null)
    {
        InitializeComponent();
        _titulo = titulo;
        LblTitulo.Text = titulo;

        // Show captured image
        if (imageBytes != null)
        {
            ObjectImage.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));
        }
        else
        {
            ObjectImage.IsVisible = false;
        }

        // Parse "FIELD: value | FIELD: value | ..."
        var parts = descripcionCompleta.Split('|')
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToArray();

        foreach (var part in parts)
        {
            var colonIdx = part.IndexOf(':');
            if (colonIdx > 0)
            {
                var fieldName = part[..colonIdx].Trim().ToUpper();
                var fieldValue = part[(colonIdx + 1)..].Trim();

                // Match known field (case-insensitive)
                var matched = KnownFields.FirstOrDefault(k =>
                    string.Equals(k, fieldName, StringComparison.OrdinalIgnoreCase));

                if (matched != null)
                {
                    var isDescription = matched == "DESCRIPCIÓN";
                    var label = new Label
                    {
                        TextColor = isDescription ? Color.FromArgb("#CCFFFFFF") : Color.FromArgb("#DDFFFFFF"),
                        FontSize = isDescription ? 15 : 14,
                        LineHeight = 1.6
                    };

                    if (isDescription)
                    {
                        label.FormattedText = new FormattedString
                        {
                            Spans =
                            {
                                new Span { Text = "DESCRIPCIÓN\n", FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#4D4AF2"), FontSize = 12 },
                                new Span { Text = fieldValue, TextColor = Color.FromArgb("#CCFFFFFF"), FontSize = 15 }
                            }
                        };
                    }
                    else
                    {
                        var displayName = matched switch
                        {
                            "TIPO" => "Tipo",
                            "COLOR" => "Color",
                            "MATERIAL" => "Material",
                            "MARCA" => "Marca",
                            "MODELO" => "Modelo",
                            "PROPÓSITO" => "Propósito",
                            _ => matched
                        };
                        label.FormattedText = new FormattedString
                        {
                            Spans =
                            {
                                new Span { Text = $"{displayName}: ", FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#4D4AF2"), FontSize = 12 },
                                new Span { Text = fieldValue, TextColor = Color.FromArgb("#DDFFFFFF"), FontSize = 14 }
                            }
                        };
                    }

                    FieldsContainer.Children.Add(label);
                    continue;
                }
            }

            // Fallback: parts that don't match known fields
            FieldsContainer.Children.Add(new Label
            {
                Text = part,
                TextColor = Color.FromArgb("#CCFFFFFF"),
                FontSize = 14,
                LineHeight = 1.5
            });
        }

        // Show Wikipedia result if available
        if (webResult != null)
        {
            FieldsContainer.Children.Add(new BoxView
            {
                HeightRequest = 1,
                Color = Color.FromArgb("#334D4AF2"),
                Margin = new Thickness(0, 10, 0, 5)
            });

            FieldsContainer.Children.Add(new Label
            {
                Text = "INFORMACIÓN WEB",
                TextColor = Color.FromArgb("#88FFFFFF"),
                FontSize = 11,
                CharacterSpacing = 1.5,
                Margin = new Thickness(0, 0, 0, 5)
            });

            FieldsContainer.Children.Add(new Label
            {
                Text = webResult.Title,
                TextColor = Colors.White,
                FontSize = 18,
                FontAttributes = FontAttributes.Bold
            });

            if (!string.IsNullOrEmpty(webResult.Description))
            {
                FieldsContainer.Children.Add(new Label
                {
                    Text = webResult.Description,
                    TextColor = Color.FromArgb("#88FFFFFF"),
                    FontSize = 13,
                    Margin = new Thickness(0, 2, 0, 8)
                });
            }

            FieldsContainer.Children.Add(new Label
            {
                Text = webResult.Extract,
                TextColor = Color.FromArgb("#CCFFFFFF"),
                FontSize = 14,
                LineHeight = 1.6
            });
        }
    }

    private void OnCerrarClicked(object? sender, EventArgs e)
    {
        Navigation.PopModalAsync();
    }

    private async void OnGoogleSearchClicked(object? sender, EventArgs e)
    {
        var query = Uri.EscapeDataString(_titulo);
        await Launcher.OpenAsync($"https://www.google.com/search?q={query}");
    }
}
