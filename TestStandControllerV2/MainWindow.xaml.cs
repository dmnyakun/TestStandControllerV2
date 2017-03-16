using System;
using System.ComponentModel;
using System.IO.Ports;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace TestStandControllerV2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged, IDisposable
    {
        public MainWindow()
        {

            InitializeComponent();
            DataContext = this;

            try
            {
                com.Open();
                info = "Connecting to tester...";
            }
            catch (System.IO.IOException)
            {
                info = "Error connecting to tester, check connections and restart program";
            }

            // initialize UI-related fields
            info = "Please place a sample into the tester, enter a gauge, and press go";
            gauge = "0";
            timeRemaining = "Time: 60.0";
            passVisible = false;
            passColor = new SolidColorBrush(Colors.Green);
            pass = "Pass";
            go = "Go";
        }

        // variable declarations
        private SerialPort com = new SerialPort("COM4", 115200, Parity.None, 8, StopBits.One);
        private DispatcherTimer timer;
        private int testTime = 60;
        private double maxValue = 0.0;
        private double force = 0;
        private bool testInProgress = false;
        public Brush green = new SolidColorBrush(Colors.Green);
        public Brush red = new SolidColorBrush(Colors.Red);

        // declarations, getters, and setters for UI-related components
        private Brush _passColor;
        public Brush passColor
        {
            get { return _passColor; }
            set
            {
                _passColor = value;
                NotifyPropertyChanged("passColor");
            }
        }

        private bool _passVisible;
        public bool passVisible
        {
            get { return _passVisible; }
            set
            {
                _passVisible = value;
                NotifyPropertyChanged("passVisible");
            }
        }

        private string _pass;
        public string pass
        {
            get { return _pass; }
            set
            {
                _pass = value;
                NotifyPropertyChanged("pass");
            }
        }

        private string _timeRemaining;
        public string timeRemaining
        {
            get { return _timeRemaining; }
            set
            {
                _timeRemaining = value;
                NotifyPropertyChanged("timeRemaining");
            }
        }

        private string _gauge;
        public string gauge
        {
            get { return _gauge; }
            set
            {
                _gauge = value;
                NotifyPropertyChanged("gauge");
            }
        }

        private string _info;
        public string info
        {
            get { return _info; }
            set
            {
                _info = value;
                NotifyPropertyChanged("info");
            }
        }

        private string _go;
        public string go
        {
            get { return _go; }
            set
            {
                _go = value;
                NotifyPropertyChanged("go");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        // end declarations


        /// <summary>
        /// getForce method
        /// returns a string to be sent to the tester, given a gauge string
        /// </summary>
        /// <param name="gauge"></param>
        /// <returns></returns>
        private string getForce(string gauge)
        {
            // large switch statement holding all force values for given gauges
            switch (gauge)
            {
                case "1":
                    return "200";
                case "2":
                    return "180";
                case "3":
                    return "160";
                case "4":
                    return "140";
                case "6":
                    return "100";
                case "8":
                    return "90";
                case "10":
                    return "80";
                case "12":
                    return "70";
                case "14":
                    return "50";
                case "16":
                    return "30";
                case "18":
                    return "20";
                case "20":
                    return "13";
                case "22":
                    return "8";
                case "24":
                    return "5";
                case "26":
                    return "3";
                case "28":
                    return "2";
                default:
                    return null;
            }
        }

        /// <summary>
        /// goButton_Click method
        /// event handling method that begins the test whenever the go button is clicked
        /// method also stops the test if the button is clicked again mid-test
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void goButton_Click(object sender, RoutedEventArgs e)
        {
            if (!testInProgress)
            {
                testInProgress = true;
                // reset pass and time values

                passVisible = false;
                timeRemaining = "Time: 60";
                // Read data from gaugeEntry field, and convert it to a force value
                // Strips any leading zeros from the string, in case an operator enters "08" or similar
                string forceString = getForce(gauge.TrimStart('0'));
                double.TryParse(forceString, out force);
                if (forceString != null)
                {
                    info = "Setting tester to " + forceString + " pounds";

                    // halt tester
                    com.Write("\\H\r");
                    Thread.Sleep(50);

                    // zero gauge
                    com.Write("\\Z\r");
                    Thread.Sleep(50);

                    // set gauge to force level
                    com.Write("\\/SPH-" + forceString + ".0\r");
                    Thread.Sleep(50);

                    // set gauge to peak tension mode
                    com.Write("\\/PT\r");
                    Thread.Sleep(50);

                    // begin tester movement upward
                    com.Write("\\J\r");

                    info = "Testing sample at " + forceString + " pounds";
                    go = "Stop";

                    // initialize timer, and begin monitoring pull test
                    timer = new DispatcherTimer(DispatcherPriority.Send);
                    timer.Interval = new TimeSpan(0, 0, 1);
                    timer.Tick += new EventHandler(timer_Tick);
                    timer.Start();

                }
                else
                {
                    info = "Incorrect gauge, please enter a gauge between 1 and 28";
                    testInProgress = false;
                }
            }
            else
            {
                failTest();
            }
        }

        /// <summary>
        /// timer_Tick method
        /// timer method that is called every time the timer completes, and checks test progress/results
        /// method can end the test in a passing state, if the test has not failed when the timer ends
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer_Tick(object sender, EventArgs e)
        {
            checkStatus();
            // only testTime time if the max value has reached the given force value
            if (maxValue >= force)
            {
                testTime--;
                timeRemaining = "Time: " + testTime;
                if (testTime <= 0)
                {
                    timer.Stop();
                    pass = "Pass";
                    passColor = green;
                    passVisible = true;
                    testTime = 60;
                    maxValue = 0.0;
                    info = "Please place a sample into the tester, enter a gauge, and press go";
                    go = "Go";
                    testInProgress = false;
                    resetTester();
                }
            }
        }

        /// <summary>
        /// checkStatus method
        /// helper method that reads data from the gauge, and determines what action to take based on results
        /// method can cause the test to fail if it detects a broken wire
        /// broken wire is detected by a significant drop in detected force
        /// </summary>
        private void checkStatus()
        {
            // request gauge reading
            com.Write("?\r");
            string str = com.ReadLine();
            
            // if gauge reading is available
            if (str.Length > 0)
            {
                // remove units from reading, and parse reading to a positive double
                str = str.Remove(str.IndexOf(' '));
                double value;
                double.TryParse(str, out value);
                value = Math.Abs(value);
               
                // if parse was successful
                if (value != 0)
                {
                    // if value is largest seen so far, mark it as new maxValue
                    if (value > maxValue)
                    {
                        maxValue = value;
                    }

                    // if value is significantly less than max value, assume the wire's broken
                    if (value < maxValue * 0.4)
                    {
                        failTest();
                    }
                }
            }
        }

        /// <summary>
        /// failTest method
        /// helper method that handles failure conditions within the test
        /// these conditions are a broken wire or the stop button being pressed
        /// </summary>
        private void failTest()
        {
            timer.Stop();
            pass = "Fail";
            passColor = red;
            passVisible = true;
            testTime = 60;
            maxValue = 0.0;
            info = "Please place a sample into the tester, enter a gauge, and press go";
            go = "Go";
            testInProgress = false;
            resetTester();

        }

        /// <summary>
        /// resetTester method
        /// helper method that resets the tester back to its fully-downward position
        /// </summary>
        private void resetTester()
        {
            // halt tester
            com.Write("\\H\r");
            Thread.Sleep(100);

            // move downward
            com.Write("\\K\r");
            Thread.Sleep(100);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    com.Close();
                }
                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
