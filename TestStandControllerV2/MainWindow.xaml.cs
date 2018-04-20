using System;
using System.ComponentModel;
using System.IO.Ports;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Input;

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
            if (SerialPort.GetPortNames().Length > 0)
            {
                try
                {
                    // try to connect to the first serial port available
                    com.PortName = SerialPort.GetPortNames()[0];
                    com.Open();
                    info = "Please place a sample into the tester, enter a gauge, and press go.\n\nTotal tests today: " + testCount;
                }
                catch (System.IO.IOException)
                {
                    info = "Error connecting to tester, check connections and restart program.";
                }
            }
            else
            {
                info = "No connection found, check connections and restart program.";
            }

            // initialize UI-related fields
            gauge = "0";
            labelText = "Gauge:";
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
        private int testCount = 0;
        private double maxValue = 0.0;
        private double force = 0;
        private bool testInProgress = false;
        private DateTime savedTime = DateTime.Now;
        public Brush green = new SolidColorBrush(Colors.Green);
        public Brush red = new SolidColorBrush(Colors.Red);

        // begin declarations, getters, and setters for UI-related components
        // getter/setter for force/gauge toggle option
        private int _mode;
        public int mode
        {
            get { return _mode; }
            set
            {
                _mode = value;
                NotifyPropertyChanged("mode");
                // calling the method here instead of dealing with events, since it's the same class anyways
                resetUI();
            }
        }

        // getter/setter for force/gauge toggle option
        private bool _directForce;
        public bool directForce
        {
            get { return _directForce; }
            set
            {
                _directForce = value;
                NotifyPropertyChanged("mode");
                // calling the method here instead of dealing with events, since it's the same class anyways
                if (directForce)
                {
                    mode = 1;
                }
                else
                {
                    mode = 0;
                }

            }
        }
        
        // getter/setter for force/gauge toggle option
        private bool _pullToBreak;
        public bool pullToBreak
        {
            get { return _pullToBreak; }
            set
            {
                _pullToBreak = value;
                NotifyPropertyChanged("mode");
                // calling the method here instead of dealing with events, since it's the same class anyways
                if (pullToBreak)
                {
                    mode = 2;
                }
                else
                {
                    mode = 0;
                }
            }
        }

        // getter/setter for pass label color selection
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

        // getter/setter for pass label visibility
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

        // getter/setter for pass label value
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

        // getter/setter for time remaining counter
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

        // getter/setter for gauge text box value
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

        // getter/setter for info text area value
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

        // getter/setter for go button text
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

        // getter/setter for gauge text box label value
        private string _labelText;
        public string labelText
        {
            get { return _labelText; }
            set
            {
                _labelText = value;
                NotifyPropertyChanged("labelText");
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
        /// getULForce method
        /// returns a string to be sent to the tester, given a gauge string
        /// return string is based on UL 486A
        /// </summary>
        /// <param name="gauge"></param>
        /// <returns></returns>
        private double getULForce(string gauge)
        {
            // large switch statement holding all force values for given gauges
            switch (gauge)
            {
                case "2":
                    return 180;
                case "3":
                    return 160;
                case "4":
                    return 140;
                case "6":
                    return 100;
                case "8":
                    return 90;
                case "10":
                    return 80;
                case "12":
                    return 70;
                case "14":
                    return 50;
                case "16":
                    return 30;
                case "18":
                    return 20;
                case "20":
                    return 13;
                case "22":
                    return 8;
                case "24":
                    return 5;
                case "26":
                    return 3;
                case "28":
                    return 2;
                default:
                    return -1;
            }
        }

        /// <summary>
        /// gaugeEntry_KeyUp
        /// event method that fires when a key is released within the gaugeEntry textbox
        /// starts test if the released key is either Enter or Return
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void gaugeEntry_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Enter)
            {
                goButton_Click(sender, new RoutedEventArgs());
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
                // Read data from gaugeEntry field, and convert it to a force value based on the current mode
                // Strips any leading zeros from the string, in case an operator enters "08" or similar
                switch (mode)
                {

                    case 0:

                        force = getULForce(gauge.TrimStart('0'));
                        break;

                    case 1:
                        
                        if (!double.TryParse(gauge.TrimStart('0'), out force))
                        {
                            info = "Invalid force entered, please enter a force between 1 and 200.\n\nTotal tests today: " + testCount;
                            testInProgress = false;
                            return;
                        }
                        break;
                       
                    case 2:

                        if (!double.TryParse(gauge.TrimStart('0'), out force))
                        {
                            info = "Invalid force entered, please enter a force between 1 and 200.\n\nTotal tests today: " + testCount;
                            testInProgress = false;
                            return;
                        }
                        break;

                    default:

                        force = -1;
                        break;
                }
                
                if (force != -1)
                {
                    info = "Setting tester to " + force + " pounds.\n\nTotal tests today: " + testCount;

                    // halt tester
                    com.Write("\\H\r");
                    Thread.Sleep(50);

                    // zero gauge
                    com.Write("\\Z\r");
                    Thread.Sleep(50);

                    // set gauge to force level + 1
                    com.Write("\\/SPH-" + (force + 1) + ".0\r");
                    Thread.Sleep(50);

                    // set gauge to peak tension mode
                    com.Write("\\/PT\r");
                    Thread.Sleep(50);

                    // begin tester movement upward
                    com.Write("\\J\r");

                    info = "Testing sample at " + force + " pounds.\n\nTotal tests today: " + testCount;
                    go = "Stop";

                    // initialize timer, and begin monitoring pull test
                    timer = new DispatcherTimer(DispatcherPriority.Send);
                    timer.Interval = new TimeSpan(0, 0, 1);
                    timer.Tick += new EventHandler(timer_Tick);
                    timer.Start();

                }
                else
                {
                    info = "Incorrect gauge, please enter a gauge between 2 and 28.\n\nTotal tests today: " + testCount;
                    testInProgress = false;
                }
            }
            else
            {
                failTest();
            }
        }

        /// <summary>
        /// resetUI method
        /// resets UI of program after a test has completed
        /// also handles counting how many tests have been run during the current day
        /// </summary>
        private void resetUI()
        {
            // reset UI and variables
            switch (mode)
            {

                case 0:
                    info = "Please place a sample into the tester, enter a gauge, and press go.\n\nTotal tests today: " + testCount;
                    labelText = "Gauge:";
                    break;

                case 1:
                    info = "Direct force entry mode enabled, please place a sample into \nthe tester, enter the force to test with, and press go.\n\nTo disable this mode, right click and select the \"Direct Force Entry Mode\" option.\n\nTotal tests today: " + testCount;
                    labelText = "Force:";
                    break;

                case 2:
                    info = "Pull-to-Break mode enabled, please place a sample into the tester,\nenter the minimum force that the wire should be able to withstand, and press go.\n\nTo disable this mode, right click and select the \"Pull-to-Break Mode\" option.\n\nTotal tests today: " + testCount;
                    labelText = "Force:";
                    break;

                default:
                    break;
            }
            testTime = 60;
            maxValue = 0.0;
            go = "Go";
            testInProgress = false;
            // increment test count if the date hasn't changed, otherwise reset the day and count variables.
            if (savedTime.Date != DateTime.Now.Date)
            {
                testCount = 0;
                savedTime = DateTime.Now;
            }
            else
            {
                testCount++;
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
                    passTest();
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
                // remove units from reading
                str = str.Remove(str.IndexOf(' '));
                double value;
               
                // if parse is successful
                if (double.TryParse(str, out value))
                {
                    // convert reading to a positive double
                    value = Math.Abs(value);

                    // if value is largest seen so far, mark it as new maxValue
                    if (value > maxValue)
                    {
                        maxValue = value;
                    }

                    // if value is within 5 lbs of max value and mode is pull-to-break, increase pull force
                    if (value + 5 > maxValue && mode == 2)
                    {
                        com.Write("\\/SPH-" + (value + 10) + ".0\r");
                        Thread.Sleep(50);
                    }

                    // if value is significantly less than max value, assume the wire's broken
                    if (value < maxValue * 0.4)
                    {
                        if (mode != 2 && maxValue > force)
                        {
                            passTest();
                        }
                        else
                        {
                            failTest();
                        }
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
            resetTester();
        }

        /// <summary>
        /// passTest method
        /// helper method that handles passing a test, and resets everything 
        /// so the next test can be run
        /// </summary>
        private void passTest()
        {
            timer.Stop();
            pass = "Pass";
            passColor = green;
            passVisible = true;
            resetTester();
        }

        /// <summary>
        /// resetTester method
        /// helper method that resets the tester back to its 
        /// fully-downward position, and resets the UI
        /// </summary>
        private void resetTester()
        {
            // reset UI components
            resetUI();

            // halt tester
            com.Write("\\H\r");
            Thread.Sleep(100);

            // move downward
            com.Write("\\K\r");
            Thread.Sleep(100);

            // set gauge to real time mode
            com.Write("\\/CUR\r");
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
