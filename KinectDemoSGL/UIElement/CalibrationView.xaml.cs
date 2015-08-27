using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using KinectDemoCommon.Util;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using KinectDemoCommon.Messages;
using KinectDemoCommon.Model;

namespace KinectDemoSGL.UIElement
{
    /// <summary>
    /// UI for initiating and managing calibration between KINECT devices.
    /// </summary>
    /// 

    //  TODO: show calculated transformation matrices, statistics (select outlying points and recalibrate with them skipped)
    //  TODO: refactor for ability to select which clients to calibrate (if using more than two)

    public partial class CalibrationView : UserControl
    {
        private DataStore dataStore;

        private ServerMessageProcessor serverMessageProcessor;

        private enum CalibrationState
        {
            Ready, Calibrating, Done
        }

        private ObservableCollection<ObservableKeyValuePair<KinectClient, CalibrationState>> clientCalibrationStates;
        private ObservableCollection<KinectClient> clients;

        public CalibrationView()
        {
            InitializeComponent();

            dataStore = DataStore.Instance;
            clientCalibrationStates = new ObservableCollection<ObservableKeyValuePair<KinectClient, CalibrationState>>();

            clients = dataStore.GetClients();
            clients.CollectionChanged += ClientsOnCollectionChanged;

            ClientList.ItemsSource = clientCalibrationStates;

            ServerMessageProcessor.Instance.CalibrationDataArrived += CalibrationDataArrived;

        }

        private void CalibrationDataArrived(KinectDemoMessage message, KinectClient kinectClient)
        {
            SetCalibrationState(kinectClient, CalibrationState.Done);
            bool allDone = true;
            foreach (var keyValuePair in clientCalibrationStates)
            {
                if (!keyValuePair.Value.Equals(CalibrationState.Done))
                {
                    allDone = false;
                }
            }
            if (allDone)
            {
                CalibrationFinished();
            }
        }

        private void CalibrationFinished()
        {
            if (dataStore.GetClients().Count != 2)
            {
                throw new Exception("Function requires 2 active clients");
            }
            var clients = dataStore.GetClients();

            CalibrationProcessor.Instance.CalculateTransformationFromAtoB(
                dataStore.GetCalibrationBodiesForClient(clients[0]),
                dataStore.GetCalibrationBodiesForClient(clients[1])
            );

            foreach (var keyValuePair in clientCalibrationStates)
            {
                keyValuePair.Value = CalibrationState.Ready;
            }
        }

        private void ClientsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            Dispatcher.Invoke(() =>
            {
                foreach (KinectClient client in clients)
                {
                    clientCalibrationStates.Clear();
                    clientCalibrationStates.Add(new ObservableKeyValuePair<KinectClient, CalibrationState>()
                    {
                        Key = client,
                        Value = CalibrationState.Ready
                    });
                }
            });
        }

        private void SetCalibrationState(KinectClient client, CalibrationState state)
        {
            foreach (ObservableKeyValuePair<KinectClient, CalibrationState> observableKeyValuePair in clientCalibrationStates)
            {
                if (client.Equals(observableKeyValuePair.Key))
                {
                    observableKeyValuePair.Value = state;
                }
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (clientCalibrationStates.Any(keyValuePair => keyValuePair.Value != CalibrationState.Ready))
            {
                return;
            }
            KinectServer.Instance.StartCalibration();
        }

        //  TODO: visible only when calibrating        
        /// <summary>
        /// Aborts current calibration session and deletes data received during this session.
        /// Used as a temporary manual solution for cases when not all KINECT devices fire the calibration data
        /// resulting in unpaired datasets.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void AbortButton_Click(object sender, RoutedEventArgs e)
        {
            int maxCount = 0;
            Dictionary<KinectClient, List<SerializableBody>> dict = dataStore.GetAllCalibrationBodies();
            foreach (var keyValuePair in dict)
            {
                int count = keyValuePair.Value.Count;
                if (count > maxCount)
                {
                    maxCount = count;
                }
            }

            foreach (var keyValuePair in dict)
            {
                if (maxCount > keyValuePair.Value.Count)
                {
                    keyValuePair.Value.Remove(keyValuePair.Value.Last());
                }
            }
        }
    }
}
