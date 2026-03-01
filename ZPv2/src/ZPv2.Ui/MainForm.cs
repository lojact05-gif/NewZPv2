using System.Net.Http.Json;
using System.Text.Json;
using ZPv2.Common.System;

namespace ZPv2.Ui;

public sealed class MainForm : Form
{
    private readonly Label _status;
    private readonly TextBox _token;
    private readonly Button _copy;
    private readonly System.Windows.Forms.Timer _timer;

    public MainForm()
    {
        Text = "ZPv2";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 480;
        Height = 260;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        BackColor = Color.White;

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 64,
            BackColor = ColorTranslator.FromHtml("#026EB7")
        };
        var title = new Label
        {
            Text = "ZPv2",
            ForeColor = Color.White,
            Font = new Font("Myriad Pro", 18, FontStyle.Bold),
            AutoSize = true,
            Left = 20,
            Top = 18,
        };
        header.Controls.Add(title);
        Controls.Add(header);

        var tokenLabel = new Label
        {
            Text = "Token (16 caracteres)",
            Font = new Font("Myriad Pro", 11, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#0D7ECC"),
            AutoSize = true,
            Left = 20,
            Top = 88,
        };
        Controls.Add(tokenLabel);

        _token = new TextBox
        {
            Left = 20,
            Top = 112,
            Width = 300,
            ReadOnly = true,
            Font = new Font("Consolas", 14, FontStyle.Bold),
            BackColor = Color.White,
            MaxLength = 16,
            TextAlign = HorizontalAlignment.Center,
        };
        Controls.Add(_token);

        _copy = new Button
        {
            Text = "Copiar token",
            Left = 330,
            Top = 111,
            Width = 120,
            Height = 34,
            BackColor = ColorTranslator.FromHtml("#00A4FF"),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            Font = new Font("Myriad Pro", 10, FontStyle.Bold),
        };
        _copy.FlatAppearance.BorderSize = 0;
        _copy.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(_token.Text))
            {
                Clipboard.SetText(_token.Text);
                _status.Text = "Token copiado.";
                _status.ForeColor = ColorTranslator.FromHtml("#026EB7");
            }
        };
        Controls.Add(_copy);

        _status = new Label
        {
            Text = "A verificar serviço...",
            Font = new Font("Myriad Pro", 10, FontStyle.Regular),
            ForeColor = ColorTranslator.FromHtml("#026EB7"),
            AutoSize = true,
            Left = 20,
            Top = 162,
        };
        Controls.Add(_status);

        var hint = new Label
        {
            Text = "Configuração de impressora/gaveta/corte é feita no POS.",
            Font = new Font("Myriad Pro", 9, FontStyle.Regular),
            ForeColor = ColorTranslator.FromHtml("#0D7ECC"),
            AutoSize = true,
            Left = 20,
            Top = 188,
        };
        Controls.Add(hint);

        _timer = new System.Windows.Forms.Timer { Interval = 5000 };
        _timer.Tick += async (_, _) => await RefreshStatusAsync();

        Load += async (_, _) =>
        {
            LoadToken();
            await RefreshStatusAsync();
            _timer.Start();
        };
    }

    private void LoadToken()
    {
        var cfg = new JsonConfigStore().GetOrCreate();
        _token.Text = cfg.Token;
    }

    private async Task RefreshStatusAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var json = await client.GetFromJsonAsync<JsonElement>("http://127.0.0.1:16262/health");
            if (json.ValueKind == JsonValueKind.Object
                && json.TryGetProperty("ok", out var ok)
                && ok.ValueKind == JsonValueKind.True)
            {
                _status.Text = "Serviço online";
                _status.ForeColor = ColorTranslator.FromHtml("#026EB7");
                return;
            }
        }
        catch
        {
            // ignored
        }

        _status.Text = "Serviço offline";
        _status.ForeColor = Color.Black;
    }
}
