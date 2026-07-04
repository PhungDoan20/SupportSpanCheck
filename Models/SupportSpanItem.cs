using Autodesk.AutoCAD.DatabaseServices;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SupportSpanCheck.Models
{
    public class SupportSpanItem : INotifyPropertyChanged
    {
        private int _index;
        private string _lineNumber = string.Empty;
        private string _spanLabel = string.Empty;
        private double _actualSpan;
        private bool _isViolated;
        private ObjectId _support1Id;
        private ObjectId _support2Id;
        private string _pipeSize = string.Empty;
        private double _maxSpan;
        private double _insulationThickness;

        public int Index
        {
            get => _index;
            set { _index = value; OnPropertyChanged(); }
        }

        public string LineNumber
        {
            get => _lineNumber;
            set { _lineNumber = value; OnPropertyChanged(); }
        }

        public string SpanLabel
        {
            get => _spanLabel;
            set { _spanLabel = value; OnPropertyChanged(); }
        }

        public string PipeSize
        {
            get => _pipeSize;
            set { _pipeSize = value; OnPropertyChanged(); }
        }

        public double MaxSpan
        {
            get => _maxSpan;
            set { _maxSpan = value; OnPropertyChanged(); }
        }

        public double InsulationThickness
        {
            get => _insulationThickness;
            set { _insulationThickness = value; OnPropertyChanged(); }
        }

        public double ActualSpan
        {
            get => _actualSpan;
            set { _actualSpan = value; OnPropertyChanged(); }
        }

        public bool IsViolated
        {
            get => _isViolated;
            set { _isViolated = value; OnPropertyChanged(); }
        }

        public ObjectId Support1Id
        {
            get => _support1Id;
            set { _support1Id = value; OnPropertyChanged(); }
        }

        public ObjectId Support2Id
        {
            get => _support2Id;
            set { _support2Id = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
