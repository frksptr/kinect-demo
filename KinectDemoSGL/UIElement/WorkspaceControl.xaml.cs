using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KinectDemo
{
    /// <summary>
    /// Interaction logic for WorkspaceControl.xaml
    /// </summary>
    public partial class WorkspaceControl : UserControl
    {
        public Workspace Workspace { get; set; }
        private WorkspaceControlMode _Mode;
        public WorkspaceControlMode Mode { 
            get {
                return this._Mode;
            }
            set {
                this._Mode = value;
                if (value == WorkspaceControlMode.Add)
                {
                    this.AddButton.Content = "Add";
                }
                else if (value == WorkspaceControlMode.Edit)
                {
                    this.AddButton.Content = "Save";
                }
            }
        }

        public WorkspaceControl()
        {
            InitializeComponent();
            Mode = WorkspaceControlMode.Add;
        }

        public void setSource(Workspace workspaceSource)
        {
            Workspace = workspaceSource;
            this.DataContext = Workspace;
        }

        public enum WorkspaceControlMode { Add, Edit };
    }

    
}
