using System.Globalization;
using LifeWorks.Application.Services;
using LifeWorks.Domain.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace LifeWorks.Web.Components.Pages.Improvements;

public partial class ImprovementDetail
{
    [Parameter]
    public Guid Id { get; set; }

    [Inject] private IHomeImprovementService HomeImprovementService { get; set; } = null!;
    [Inject] private IAttachmentService AttachmentService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    private HomeImprovement? _improvement;
    private bool _loading = true;

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        _improvement = await HomeImprovementService.GetByIdAsync(Id);
        _loading = false;
    }

    private async Task UploadAttachmentAsync(IBrowserFile file)
    {
        if (_improvement is null) return;
        await using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
        await AttachmentService.SaveAsync(_improvement.Id, file.Name, file.ContentType, stream);
        _improvement = await HomeImprovementService.GetByIdAsync(Id);
        Snackbar.Add("Attachment uploaded", Severity.Success);
    }

    private async Task DeleteAttachmentAsync(Guid attachmentId, string fileName)
    {
        var parameters = new DialogParameters<MudMessageBox>
        {
            { x => x.Message, $"Delete '{fileName}'? This cannot be undone." },
            { x => x.YesText, "Delete" },
            { x => x.CancelText, "Cancel" }
        };
        var dialog = await DialogService.ShowAsync<MudMessageBox>("Delete Attachment", parameters, new DialogOptions { CloseOnEscapeKey = true });
        var result = await dialog.Result;
        if (result is null || result.Canceled) return;
        await AttachmentService.DeleteAsync(attachmentId);
        _improvement = await HomeImprovementService.GetByIdAsync(Id);
        Snackbar.Add("Attachment deleted", Severity.Success);
    }

    private static string DownloadHref(Guid id) => "/attachments/" + id + "/download";

    private static string AttachmentSubtitle(Attachment att)
    {
        string size;
        if (att.FileSize < 1024)
            size = att.FileSize + " B";
        else if (att.FileSize < 1024 * 1024)
            size = (att.FileSize / 1024.0).ToString("F1", CultureInfo.CurrentCulture) + " KB";
        else
            size = (att.FileSize / (1024.0 * 1024)).ToString("F1", CultureInfo.CurrentCulture) + " MB";

        return size + " - " + att.UploadedAt.ToLocalTime().ToString("MM/dd/yyyy h:mm tt", CultureInfo.CurrentCulture);
    }

    private void EditImprovement() => Navigation.NavigateTo("/improvements/" + Id + "/edit");
    private void BackToList() => Navigation.NavigateTo("/improvements");

    private bool HasManufacturerInfo() =>
        _improvement is not null &&
        (!string.IsNullOrWhiteSpace(_improvement.ManufacturerName) ||
         !string.IsNullOrWhiteSpace(_improvement.ManufacturerModel) ||
         !string.IsNullOrWhiteSpace(_improvement.ManufacturerSerialNumber) ||
         _improvement.ManufacturerWarrantyExpiration.HasValue);

    private static Color WarrantyColor(DateOnly expiration)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var daysRemaining = expiration.DayNumber - today.DayNumber;
        if (daysRemaining > 90) return Color.Success;
        if (daysRemaining >= 1) return Color.Warning;
        return Color.Error;
    }

    private static string WarrantyLabel(DateOnly expiration)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var daysRemaining = expiration.DayNumber - today.DayNumber;
        if (daysRemaining > 0)
            return "Expires " + expiration.ToString("MM/dd/yyyy", CultureInfo.CurrentCulture) + " (" + daysRemaining + "d remaining)";
        if (daysRemaining == 0)
            return "Expires today (" + expiration.ToString("MM/dd/yyyy", CultureInfo.CurrentCulture) + ")";
        return "Expired " + expiration.ToString("MM/dd/yyyy", CultureInfo.CurrentCulture) + " (" + (-daysRemaining) + "d ago)";
    }
}
