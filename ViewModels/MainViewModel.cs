using SupportSpanCheck.Models;
using SupportSpanCheck.Services;
using SupportSpanCheck.Utils;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SupportSpanCheck.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<SupportSpanItem> _allSpanItems = new ObservableCollection<SupportSpanItem>();
        private ObservableCollection<SupportSpanItem> _displaySpanItems = new ObservableCollection<SupportSpanItem>();
        private ObservableCollection<SpanStandard> _spanStandards = new ObservableCollection<SpanStandard>();
        private bool _showAllSpans = true;
        private string _statusMessage = "";

        public ObservableCollection<SpanStandard> SpanStandards
        {
            get => _spanStandards;
            set { _spanStandards = value; OnPropertyChanged(); }
        }

        public ObservableCollection<SupportSpanItem> DisplaySpanItems
        {
            get => _displaySpanItems;
            set { _displaySpanItems = value; OnPropertyChanged(); }
        }

        public bool ShowAllSpans
        {
            get => _showAllSpans;
            set
            {
                if (_showAllSpans != value)
                {
                    _showAllSpans = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ShowOnlyViolated));
                    UpdateDisplayList();
                }
            }
        }

        public bool ShowOnlyViolated
        {
            get => !_showAllSpans;
            set { ShowAllSpans = !value; }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public ICommand ScanCommand { get; }
        public ICommand RowDoubleClickCommand { get; }
        public ICommand SaveSettingsCommand { get; }

        public MainViewModel()
        {
            SpanStandards = SettingsService.LoadSettings();

            ScanCommand = new RelayCommand(ExecuteScan);
            RowDoubleClickCommand = new RelayCommand(ExecuteRowDoubleClick);
            SaveSettingsCommand = new RelayCommand(ExecuteSaveSettings);
        }

        private void ExecuteScan(object? parameter)
        {
            _allSpanItems.Clear();
            StatusMessage = "";
            bool hasMissingSize = false;
            int orphanCount = 0;

            var results = Plant3DService.ScanSupports(SpanStandards, out hasMissingSize, out orphanCount);
            
            foreach (var item in results)
            {
                _allSpanItems.Add(item);
            }

            if (orphanCount > 0)
            {
                StatusMessage = $"LỖI: Có {orphanCount} Support mồ côi (đặt quá xa ống)!";
            }
            else if (hasMissingSize)
            {
                StatusMessage = "CẢNH BÁO: Có ống mang Size chưa được khai báo! Đã dùng mặc định 1000mm.";
            }
            else if (results.Count > 0)
            {
                StatusMessage = "Quét thành công.";
            }

            UpdateDisplayList();
        }

        private void ExecuteSaveSettings(object? parameter)
        {
            SettingsService.SaveSettings(SpanStandards);
            StatusMessage = "Đã lưu tiêu chuẩn nhịp thành công!";
        }

        private void UpdateDisplayList()
        {
            DisplaySpanItems.Clear();
            foreach (var item in _allSpanItems)
            {
                if (ShowAllSpans || item.IsViolated)
                {
                    DisplaySpanItems.Add(item);
                }
            }
        }

        private void ExecuteRowDoubleClick(object? parameter)
        {
            if (parameter is SupportSpanItem item)
            {
                Plant3DService.ZoomAndHighlight(item.Support1Id, item.Support2Id);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
