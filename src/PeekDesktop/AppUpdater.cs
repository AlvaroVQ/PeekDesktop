using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Updatum;

namespace PeekDesktop;

internal sealed class AppUpdater
{
    private readonly UpdatumManager _updater = new("shanselman", "PeekDesktop")
    {
        FetchOnlyLatestRelease = true,
        InstallUpdateSingleFileExecutableName = "PeekDesktop"
    };

    private bool _isChecking;

    public async Task CheckForUpdatesAsync(bool interactive)
    {
        if (_isChecking)
        {
            AppDiagnostics.Log("Update check already in progress");

            if (interactive)
            {
                MessageBox.Show(
                    "PeekDesktop is already checking for updates.",
                    "PeekDesktop Update",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            return;
        }

        _isChecking = true;

        try
        {
            AppDiagnostics.Log(interactive ? "Manual update check started" : "Background update check started");

            var updateFound = await _updater.CheckForUpdatesAsync();
            if (!updateFound)
            {
                AppDiagnostics.Log("No updates available");

                if (interactive)
                {
                    MessageBox.Show(
                        "You're already on the latest version of PeekDesktop.",
                        "PeekDesktop Update",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                return;
            }

            var release = _updater.LatestRelease;
            if (release is null)
                throw new InvalidOperationException("An update was reported, but no release details were returned.");

            AppDiagnostics.Log($"Update available: {release.TagName}");

            using var prompt = new UpdatePromptDialog(release.TagName, _updater.GetChangelog(true) ?? "No release notes available.");
            if (prompt.ShowDialog() == DialogResult.OK)
                await DownloadAndInstallUpdateAsync();
        }
        catch (Exception ex)
        {
            AppDiagnostics.Log($"Update check failed: {ex}");

            if (interactive)
            {
                MessageBox.Show(
                    $"PeekDesktop couldn't check for updates.\n\n{ex.Message}",
                    "Update Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        finally
        {
            _isChecking = false;
        }
    }

    private async Task DownloadAndInstallUpdateAsync()
    {
        try
        {
            AppDiagnostics.Log("Downloading update");

            using var progress = new UpdateProgressDialog();
            progress.Show();
            progress.Refresh();

            var downloadedAsset = await _updater.DownloadUpdateAsync();
            progress.Close();

            if (downloadedAsset is null)
                throw new InvalidOperationException("The update download did not complete.");

            var confirm = MessageBox.Show(
                "The update has been downloaded. PeekDesktop will now close and install the new version.\n\nContinue?",
                "Install Update",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
                await _updater.InstallUpdateAsync(downloadedAsset);
        }
        catch (Exception ex)
        {
            AppDiagnostics.Log($"Update install failed: {ex}");
            MessageBox.Show(
                $"PeekDesktop couldn't install the update.\n\n{ex.Message}",
                "Update Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}

internal sealed class UpdatePromptDialog : Form
{
    public UpdatePromptDialog(string version, string changelog)
    {
        Text = "PeekDesktop Update Available";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(620, 420);

        var titleLabel = new Label
        {
            AutoSize = true,
            Location = new Point(12, 12),
            Text = $"A new version is available: {version}"
        };

        var bodyLabel = new Label
        {
            AutoSize = true,
            Location = new Point(12, 38),
            Text = "Release notes:"
        };

        var notesBox = new TextBox
        {
            Location = new Point(12, 62),
            Size = new Size(596, 310),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Text = changelog
        };

        var laterButton = new Button
        {
            Text = "Later",
            DialogResult = DialogResult.Cancel,
            Location = new Point(452, 382),
            Size = new Size(75, 26)
        };

        var updateButton = new Button
        {
            Text = "Update now",
            DialogResult = DialogResult.OK,
            Location = new Point(533, 382),
            Size = new Size(75, 26)
        };

        Controls.Add(titleLabel);
        Controls.Add(bodyLabel);
        Controls.Add(notesBox);
        Controls.Add(laterButton);
        Controls.Add(updateButton);

        AcceptButton = updateButton;
        CancelButton = laterButton;
    }
}

internal sealed class UpdateProgressDialog : Form
{
    public UpdateProgressDialog()
    {
        Text = "PeekDesktop Update";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ControlBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(320, 90);

        Controls.Add(new Label
        {
            AutoSize = true,
            Location = new Point(12, 15),
            Text = "Downloading the latest release..."
        });

        Controls.Add(new ProgressBar
        {
            Location = new Point(12, 40),
            Size = new Size(296, 18),
            Style = ProgressBarStyle.Marquee
        });
    }
}
