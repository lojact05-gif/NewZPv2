using System.Text.Json.Serialization;

namespace ZPv2.Common.Models;

public sealed class PrintJobRequest
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "RECEIPT";

    [JsonPropertyName("printer_name")]
    public string PrinterName { get; set; } = string.Empty;

    [JsonPropertyName("payload")]
    public object? Payload { get; set; }

    [JsonPropertyName("pages")]
    public List<string> Pages { get; set; } = new();

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("cut")]
    public bool Cut { get; set; } = true;

    [JsonPropertyName("open_drawer")]
    public bool OpenDrawer { get; set; }

    [JsonPropertyName("cut_mode")]
    public string CutMode { get; set; } = "partial";

    [JsonPropertyName("feed_lines")]
    public int FeedLines { get; set; } = 6;

    [JsonPropertyName("width_dots")]
    public int WidthDots { get; set; } = 576;

    [JsonPropertyName("segment_height")]
    public int SegmentHeight { get; set; } = 1200;
}
