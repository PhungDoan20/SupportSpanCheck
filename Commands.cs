using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using SupportSpanCheck.ViewModels;
using SupportSpanCheck.Views;
using System;

namespace SupportSpanCheck
{
    public class Commands
    {
        private static AntigravityView? _view;

        [CommandMethod("SupportSpanCheck")]
        public void ShowSupportSpanCheck()
        {
            try
            {
                if (_view == null || !_view.IsLoaded)
                {
                    _view = new AntigravityView();
                    _view.DataContext = new MainViewModel();
                    Application.ShowModelessWindow(Application.MainWindow.Handle, _view);
                }
                else
                {
                    _view.Focus();
                }
            }
            catch (System.Exception ex)
            {
                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"\nError: {ex.Message}");
            }
        }
    }
}
