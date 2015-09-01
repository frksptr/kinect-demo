using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using KinectDemoCommon.Messages;
using KinectDemoCommon.Model;
using KinectDemoCommon.Util;
using KinectDemoSGL.Properties;

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
        private KinectServer kinectServer = KinectServer.Instance;
        private Transformation transformation;

        public CalibrationView()
        {
            InitializeComponent();

            dataStore = DataStore.Instance;
            clientCalibrationStates = new ObservableCollection<ObservableKeyValuePair<KinectClient, CalibrationState>>();

            clients = dataStore.GetClients();
            clients.CollectionChanged += ClientsOnCollectionChanged;

            ClientList.ItemsSource = clientCalibrationStates;

            ServerMessageProcessor.Instance.CalibrationMessageArrived += CalibrationDataArrived;

        }

        private void CalibrationDataArrived(KinectDemoMessage message, KinectClient kinectClient)
        {
            SetCalibrationState(kinectClient, CalibrationState.Done);
            bool allDone = true;
            foreach (ObservableKeyValuePair<KinectClient, CalibrationState> keyValuePair in clientCalibrationStates)
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
            ObservableCollection<KinectClient> clients = dataStore.GetClients();

            CalibrationProcessor.Instance.CalculateTransformationFromAtoB(
                dataStore.GetCalibrationBodiesForClient(clients[0]),
                dataStore.GetCalibrationBodiesForClient(clients[1])
            );

            foreach (ObservableKeyValuePair<KinectClient, CalibrationState> keyValuePair in clientCalibrationStates)
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

        private void SetCalibrationStates(CalibrationState state)
        {
            foreach (ObservableKeyValuePair<KinectClient, CalibrationState> observableKeyValuePair in clientCalibrationStates)
            {
                observableKeyValuePair.Value = state;
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (clientCalibrationStates.Any(keyValuePair => keyValuePair.Value != CalibrationState.Ready))
            {
                return;
            }
            foreach (ObservableKeyValuePair<KinectClient, CalibrationState> keyValuePair in clientCalibrationStates)
            {
                keyValuePair.Value = CalibrationState.Calibrating;
            }
            kinectServer.StartCalibration();
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
            List<KinectClient> workingClients = new List<KinectClient>();
            foreach (var keyValuePair in clientCalibrationStates)
            {
                if (keyValuePair.Value.Equals(CalibrationState.Calibrating))
                {
                    workingClients.Add(keyValuePair.Key);
                }
            }

            kinectServer.StopCalibration(workingClients);

            int maxCount = 0;
            Dictionary<KinectClient, List<SerializableBody>> dict = dataStore.GetAllCalibrationBodies();
            foreach (KeyValuePair<KinectClient, List<SerializableBody>> keyValuePair in dict)
            {
                int count = keyValuePair.Value.Count;
                if (count > maxCount)
                {
                    maxCount = count;
                }
            }

            foreach (KeyValuePair<KinectClient, List<SerializableBody>> keyValuePair in dict)
            {
                if (maxCount > keyValuePair.Value.Count)
                {
                    keyValuePair.Value.Remove(keyValuePair.Value.Last());
                }
            }
            SetCalibrationStates(CalibrationState.Ready);
        }

        private void EvaluateButton_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<KinectClient, List<SerializableBody>> allCalibrationBodies = dataStore.GetAllCalibrationBodies();
            List<SerializableBody>[] calibrationBodies = allCalibrationBodies.Values.ToArray();
            transformation = CalibrationProcessor.Instance.CalculateTransformationFromAtoB(calibrationBodies[0], calibrationBodies[1]);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Transformation = transformation;
            Settings.Default.Save();
        }


        private void LoadButton_OnClick(object sender, RoutedEventArgs e)
        {
            transformation = Settings.Default.Transformation;
        }
    }
}
