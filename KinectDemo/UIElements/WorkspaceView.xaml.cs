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
    /// Interaction logic for WorkspaceView.xaml
    /// </summary>
    public partial class WorkspaceView : UserControl
    {
        public Workspace Workspace { get; set; }
        public WorkspaceView()
        {
            InitializeComponent();
        }

        public WorkspaceView(Workspace workspace)
        {
            InitializeComponent();
            this.Workspace = workspace;
            this.DataContext = workspace;
        }

        public void setSource(Workspace workspaceSource)
        {
            this.Workspace = workspaceSource;
            this.DataContext = this.Workspace;
        }
    }


}
