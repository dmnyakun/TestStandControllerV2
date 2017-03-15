using System;
using System.ComponentModel;
using System.IO.Ports;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace TestStandControllerV2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
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

            info = "Please place a sample into the tester, enter a gauge, and press go";
            gauge = "0";
            timeRemaining = "Time: 60";
            passVisible = false;
            passColor = new SolidColorBrush(Colors.Green);
            pass = "Pass";
            go = "Go";
        }

        private SerialPort com = new SerialPort("COM4", 115200, Parity.None, 8, StopBits.One);
        private DispatcherTimer timer;
        private int testTime = 60;
        private double maxValue = 0.0;
        private double force;
        private bool lockUI;
        public Brush green = new SolidColorBrush(Colors.Green);
        public Brush red = new SolidColorBrush(Colors.Red);

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

        // getForce method
        // returns a string to be sent to the tester, given a gauge
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
        
        private void goButton_Click(object sender, RoutedEventArgs e)
        {
            if (!lockUI)
            {
                lockUI = true;
                // reset pass and time values

                passVisible = false;
                timeRemaining = "Time: 60";
                info = "Reading Data";
                // Read data from gaugeEntry field, and convert it to a force value
                // Strips any leading zeros from the string, in case an operator enters "08" or similar
                string forceString = getForce(gauge.TrimStart('0'));
                double.TryParse(forceString, out force);
                if (forceString != null)
                {
                    info = "Setting tester to " + forceString + " pounds";

                    //
                    //com.WriteLine("p");
                    //// if machine is not at lower limit
                    //if (com.ReadLine().Contains("DL"))
                    //{
                    //    // move machine crosshead downward, will stop automatically at lower limit switch
                    //    com.WriteLine("d");
                    //}
                    //

                    // set gauge to force level
                    //com.Write("\\/SPH-" + forceString + ".0\r");
                    info = "Testing sample at " + forceString + " pounds";
                    go = "Stop";
                    // initialize timer, and begin monitoring pull test
                    timer = new DispatcherTimer(DispatcherPriority.Send);
                    timer.Interval = new TimeSpan(0,0,1);
                    timer.Tick += new EventHandler(timer_Tick);
                    timer.Start();

                }
                else
                {
                    info = "Incorrect gauge, please enter a gauge between 1 and 28";
                    lockUI = false;
                }
            }
            else
            {
                failTest();
            }
        }

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
                    info = "Please place a sample into the tester, enter a gauge, and press go";
                    go = "Go";
                    lockUI = false;
                }
            }
        }

        private void checkStatus()
        {
            // request gauge reading
            //com.Write("?\r");
            string str = "-12.05 lbF";//com.ReadLine();

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

                    // if value is significantly less than max value, the wire's broken
                    if (value < maxValue * 0.6)
                    {
                        failTest();
                    }
                }
            }
        }

        private void failTest()
        {
            timer.Stop();
            pass = "Fail";
            passColor = red;
            passVisible = true;
            testTime = 60;
            info = "Please place a sample into the tester, enter a gauge, and press go";
            go = "Go";
            lockUI = false;
            // should halt test, and reset to initial position
        }
    }
}
