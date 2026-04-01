using System.Windows;
using AudioGuard.Models;

namespace AudioGuard;

public partial class MainWindow : Window
{
    private readonly AppSettings _settings;
    private readonly Action _onSaved;

    public MainWindow(AppSettings settings, Action onSaved)
    {
        InitializeComponent();
        _settings = settings;
        _onSaved = onSaved;
        LoadSettingsToUI();
    }

    private void LoadSettingsToUI()
    {
        ChkMute.IsChecked = _settings.MuteByDefault;
        SliderVolume.Value = _settings.DefaultVolume;
        TxtVolumeLabel.Text = $"{_settings.DefaultVolume}%";
        ChkAutoStart.IsChecked = _settings.AutoStart;

        LstExcluded.ItemsSource = null;
        LstExcluded.ItemsSource = _settings.ExcludedProcesses;

        LstApplied.ItemsSource = null;
        LstApplied.ItemsSource = _settings.AppliedProcesses;
    }

    private void SliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TxtVolumeLabel != null)
            TxtVolumeLabel.Text = $"{(int)SliderVolume.Value}%";
    }

    private void BtnAddExclude_Click(object sender, RoutedEventArgs e)
    {
        var name = TxtNewExclude.Text.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(name)) return;
        if (!name.EndsWith(".exe")) name += ".exe";
        if (!_settings.ExcludedProcesses.Contains(name))
        {
            _settings.ExcludedProcesses.Add(name);
            RefreshLists();
        }
        TxtNewExclude.Text = "";
    }

    private void BtnRemoveExclude_Click(object sender, RoutedEventArgs e)
    {
        if (LstExcluded.SelectedItem is string item)
        {
            _settings.ExcludedProcesses.Remove(item);
            RefreshLists();
        }
    }

    private void BtnRemoveApplied_Click(object sender, RoutedEventArgs e)
    {
        if (LstApplied.SelectedItem is string item)
        {
            _settings.AppliedProcesses.Remove(item);
            RefreshLists();
        }
    }

    private void BtnClearApplied_Click(object sender, RoutedEventArgs e)
    {
        _settings.AppliedProcesses.Clear();
        RefreshLists();
    }

    private void RefreshLists()
    {
        LstExcluded.ItemsSource = null;
        LstExcluded.ItemsSource = _settings.ExcludedProcesses;
        LstApplied.ItemsSource = null;
        LstApplied.ItemsSource = _settings.AppliedProcesses;
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        _settings.MuteByDefault = ChkMute.IsChecked == true;
        _settings.DefaultVolume = (int)SliderVolume.Value;
        _settings.AutoStart = ChkAutoStart.IsChecked == true;

        SettingsService.Save(_settings);
        SettingsService.SetAutoStart(_settings.AutoStart);
        _onSaved();

        System.Windows.MessageBox.Show("設定を保存しました。", "AudioGuard", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // ウィンドウを閉じずに隠す
        e.Cancel = true;
        Hide();
    }
}
