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
                    info = "Please place a sample into the tester, enter a gauge, and press go.";
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
            labelText = gaugeText;
            timeRemaining = "Time: 60";
            timeVisible = false;
            passVisible = false;
            passColor = passedTest;
            pass = passedTestText;
            go = readyToStart;
            infoBackgroundColor = blank;
            forceBackgroundColor = forceColor;
            breakBackgroundColor = breakColor;
            pushBackgroundColor = pushColor;
            resetUI();
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
        public static Brush passedTest = new SolidColorBrush(Colors.Green);
        public static Brush canceledTest = new SolidColorBrush(Colors.Yellow);
        public static Brush failedTest = new SolidColorBrush(Colors.Red);
        public static SolidColorBrush breakColor = new SolidColorBrush(Color.FromArgb(160, 255, 152, 0)); // orange
        public static SolidColorBrush forceColor = new SolidColorBrush(Color.FromArgb(160, 152, 181, 229)); // blue
        public static SolidColorBrush pushColor = new SolidColorBrush(Color.FromArgb(160, 255, 0, 0)); // red
        public static SolidColorBrush comboColor = new SolidColorBrush(Color.FromArgb(160, 170, 102, 204)); // purpleish
        public static SolidColorBrush blank = new SolidColorBrush(Colors.White);
        private string passedTestText = "Pass";
        private string canceledTestText = "Cancel";
        private string failedTestText = "Failed";
        private string readyToStart = "Go";
        private string readyToStop = "Stop";
        private string gaugeText = "Gauge:";
        private string forceText = "Force:";
        private string[] resultsArray = new string[15];

        // begin declarations, getters, and setters for UI-related components
        // getter/setter for mode selector
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
                NotifyPropertyChanged("directForce");
                // set mode depending on toggle status
                setMode();
                setBackgroundColor();
            }
        }
        
        // getter/setter for pull-to-break toggle option
        private bool _pullToBreak;
        public bool pullToBreak
        {
            get { return _pullToBreak; }
            set
            {
                _pullToBreak = value;
                NotifyPropertyChanged("pullToBreak");
                setMode();
                setBackgroundColor();
            }
        }

        // getter/setter for push testing toggle option
        private bool _pushTesting;
        public bool pushTesting
        {
            get { return _pushTesting; }
            set
            {
                _pushTesting = value;
                NotifyPropertyChanged("pushTesting");
                setMode();
                setBackgroundColor();
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

        // getter/setter for time remaining visibility
        private bool _timeVisible;
        public bool timeVisible
        {
            get { return _timeVisible; }
            set
            {
                _timeVisible = value;
                NotifyPropertyChanged("timeVisible");
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

        // getter/setter for results string
        private string _results;
        public string results
        {
            get { return _results; }
            set
            {
                _results = value;
                NotifyPropertyChanged("results");
            }
        }

        // getter/setter for totalTestCount string
        private string _totalTestCount;
        public string totalTestCount
        {
            get { return _totalTestCount; }
            set
            {
                _totalTestCount = value;
                NotifyPropertyChanged("totalTestCount");
            }
        }
        
        // getter/setter for background color selection
        private SolidColorBrush _infoBackgroundColor;
        public SolidColorBrush infoBackgroundColor
        {
            get { return _infoBackgroundColor; }
            set
            {
                _infoBackgroundColor = value;
                NotifyPropertyChanged("infoBackgroundColor");
            }
        }

        // getter/setter for background color selection
        private SolidColorBrush _breakBackgroundColor;
        public SolidColorBrush breakBackgroundColor
        {
            get { return _breakBackgroundColor; }
            set
            {
                _breakBackgroundColor = value;
                NotifyPropertyChanged("breakBackgroundColor");
            }
        }

        // getter/setter for background color selection
        private SolidColorBrush _pushBackgroundColor;
        public SolidColorBrush pushBackgroundColor
        {
            get { return _pushBackgroundColor; }
            set
            {
                _pushBackgroundColor = value;
                NotifyPropertyChanged("pushBackgroundColor");
            }
        }

        // getter/setter for background color selection
        private SolidColorBrush _forceBackgroundColor;
        public SolidColorBrush forceBackgroundColor
        {
            get { return _forceBackgroundColor; }
            set
            {
                _forceBackgroundColor = value;
                NotifyPropertyChanged("forceBackgroundColor");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        // end declarations

        /// setMode method
        /// Sets mode to a value from 0-3 depending on selected options
        /// Mode 0: normal operation
        /// Mode 1: direct force entry
        /// Mode 2: pull-to-break
        /// Mode 3, direct force entry, pull-to-break
        private void setMode()
        {
            mode = 0;
            if (directForce)
            {
                mode += 1;
            }
            if (pullToBreak)
            {
                mode += 2;
            }
            if (pushTesting)
            {
                mode += 4;
            }

        }

        /// setBackgroundColor method
        /// Sets background color of the main screen depending on mode
        /// Pull-to-break modes get an orange background, and normal modes get a white one
        /// Mode 0: normal operation
        /// Mode 1: direct force entry
        /// Mode 2: pull-to-break
        /// Mode 3, direct force entry, pull-to-break
        private void setBackgroundColor()
        {
            switch (mode)
            {
                case 7:
                case 6:
                case 5:
                case 4:
                    infoBackgroundColor = pushColor;
                    break;

                case 3:
                    infoBackgroundColor = comboColor;
                    break;

                case 2:
                    infoBackgroundColor = breakColor;
                    break;

                case 1:
                    infoBackgroundColor = forceColor;
                    break;

                case 0:

                default:
                    infoBackgroundColor = blank;
                    break;

            }
        }

        /// <summary>
        /// getULForce method
        /// returns a string to be sent to the tester, given a gauge string
        /// return string is based on UL 486A
        /// </summary>
        /// <param name="gauge"></param>
        /// <returns></returns>
        private static double getULForce(string gauge)
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
        /// getSAEForce method
        /// returns a string to be sent to the tester, given a gauge string
        /// return string is based on SAE AS7928
        /// </summary>
        /// <param name="gauge"></param>
        /// <returns></returns>
        private static double getSAEForce(string gauge)
        {
            // large switch statement holding all force values for given gauges
            switch (gauge)
            {
                case "10":
                    return 150;
                case "12":
                    return 110;
                case "14":
                    return 70;
                case "16":
                    return 50;
                case "18":
                    return 38;
                case "20":
                    return 19;
                case "22":
                    return 15;
                case "24":
                    return 10;
                case "26":
                    return 7;
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
                // officially begin test
                testInProgress = true;

                // reset pass and time values
                passVisible = false;
                timeRemaining = "Time: 60";
                if (mode < 2)
                {
                    timeVisible = true;
                }
                
                // Read data from gaugeEntry field, and convert it to a force value based on the current mode
                // Strips any leading zeros from the string, in case an operator enters "08" or similar
                switch (mode)
                {

                    case 0:
                    case 4:
                        // standard mode
                        force = getULForce(gauge.TrimStart('0'));
                        break;

                    case 2:
                    case 6:
                        // pull-to-break mode
                        force = getSAEForce(gauge.TrimStart('0'));
                        break;

                    case 1:
                    case 3:
                    case 5:
                    case 7:
                        // direct force entry modes
                        if (!double.TryParse(gauge.TrimStart('0'), out force))
                        {
                            info = "Invalid force entered, please enter a force between 1 and 200.";
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
                    // clamp allowed values to be between 1 and 200 lbs, to prevent damage to the tester
                    force = Math.Min(force, 200);
                    force = Math.Max(force, 1);

                    info = "Setting tester to " + force + " pounds.";

                    // halt tester
                    com.Write("\\H\r");
                    Thread.Sleep(50);

                    // zero gauge
                    com.Write("\\Z\r");
                    Thread.Sleep(50);

                    // set gauge to force level + 1
                    com.Write("\\/SPH-" + (force + 1) + ".0\r");
                    Thread.Sleep(50);

                    // set gauge to peak tension/compression mode, depending on test mode
                    if (mode < 4)
                    {
                        com.Write("\\/PT\r");
                    } else
                    {
                        com.Write("\\/PC\r");
                    }
                    Thread.Sleep(50);

                    // begin tester movement
                    // J is upward movement, K is downward movement
                    if (mode < 4)
                    {
                        com.Write("\\J\r");
                    } else
                    {
                        com.Write("\\K\r");
                    }
                    info = "Testing sample at " + force + " pounds.";
                    go = readyToStop;

                    // initialize timer, and begin monitoring pull test
                    timer = new DispatcherTimer(DispatcherPriority.Send);
                    timer.Interval = new TimeSpan(0, 0, 1);
                    timer.Tick += new EventHandler(timer_Tick);
                    timer.Start();

                }
                else
                {
                    if (mode == 0)
                    {
                        info = "Incorrect gauge, please enter a gauge between 2 and 28.";
                    }
                    else if (mode == 2)
                    {
                        info = "Incorrect gauge, please enter a gauge between 10 and 26.";
                    }
                    testInProgress = false;
                }
            }
            else
            {
                cancelTest();
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
                    // standard mode
                    info = "Please place a sample into the tester, enter a gauge, and press go.\n\nSupported gauges: 2-28 AWG";
                    labelText = gaugeText;
                    break;

                case 1:
                    // direct force entry mode
                    info = "Direct force entry mode enabled.\nPlease place a sample into the tester, enter a force to test to, and press go.\n\nTo disable this mode, right click and select the \"Direct Force Entry Mode\" option.\n\nSupported forces: 1-200 lbs.";
                    labelText = forceText;
                    break;

                case 2:
                    // pull-to-break mode
                    info = "Pull-to-break mode enabled.\nPlease place a sample into the tester, enter a gauge, and press go.\n\nTo disable this mode, right click and select the \"Pull-to-Break Mode\" option.\n\nSupported gauges: 10-26 AWG";
                    labelText = gaugeText;
                    break;

                case 3:
                    // pull-to-break mode with direct force entry
                    info = "Pull-to-break with direct force entry mode enabled.\nPlease place a sample into the tester, enter a force to test to, and press go.\n\nTo disable this mode, right click and select either the \"Pull-to-Break Mode\" or \"Direct Force Entry Mode\" option.\n\nSupported forces: 1-200 lbs.";
                    labelText = forceText;
                    break;

                default:
                    break;
            }
            testTime = 60;
            maxValue = 0.0;
            go = readyToStart;
            testInProgress = false;
            timeVisible = false;
            // reset the day and count variables if the day changed
            if (savedTime.Date != DateTime.Now.Date)
            {
                testCount = 0;
                savedTime = DateTime.Now;
                resetResults();
            }
            // increment today's test count if a test just ended
            if (testInProgress && pass != canceledTestText)
            {
                testCount++;
            }
            totalTestCount = "Total tests today: " + testCount + ", last refresh " + DateTime.Now.ToString("t");
            results = printResult();
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
            // only decrement time if the max value has reached the given force value
            if (maxValue >= force && timeVisible)
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
            Thread.Sleep(50);
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

                    const int valueAdd = 5;
                    // if value is within valueAdd lbs of max value and mode is pull-to-break, increase pull force
                    if (value + valueAdd > maxValue && (mode == 2 || mode == 3 || mode == 6 || mode == 7))
                    {
                        if (value + valueAdd >= force)
                        {
                            com.Write("\\H\r");
                            Thread.Sleep(50);
                            com.Write("\\/SPH-" + (value + valueAdd) + ".0\r");
                            Thread.Sleep(50);
                            if (mode < 4)
                            {
                                com.Write("\\J\r");
                            }
                            else
                            {
                                com.Write("\\K\r");
                            }
                            Thread.Sleep(50);
                        }
                    }

                    // if value is significantly less than max value, and the maxValue's greater than 
                    // 10% of the required force, assume the wire's broken
                    if (value < maxValue * 0.4 && maxValue > force / 10)
                    {
                        // check if we reached the required value in pull-to-break test modes
                        if ((mode == 2 || mode == 3 || mode == 6 || mode == 7) && maxValue > force)
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
        /// helper method that handles failure conditions within the test such as a broken wire
        /// </summary>
        private void failTest()
        {
            timer.Stop();
            pass = failedTestText;
            passColor = failedTest;
            passVisible = true;
            recordResult();
            resetTester();
        }

        /// <summary>
        /// cancelTest method
        /// helper method that handles cancellation of the test via stop button
        /// </summary>
        private void cancelTest()
        {
            timer.Stop();
            pass = canceledTestText;
            passColor = canceledTest;
            passVisible = true;
            resetTester();
        }

        /// <summary>
        /// passTest method
        /// helper method that handles passing a test
        /// </summary>
        private void passTest()
        {
            timer.Stop();
            pass = passedTestText;
            passColor = passedTest;
            passVisible = true;
            recordResult();
            resetTester();
        }

        /// <summary>
        /// recordResult method
        /// helper method that records the last result, and stores it in an array
        /// </summary>
        /// 
        private void recordResult()
        {
            for (int i = resultsArray.Length - 1; i > 0; i--)
            {
                resultsArray[i] = resultsArray[i - 1];
            }
            switch (mode)
            {

                case 0:
                case 4:
                    // record pass/fail in standard test, along with gauge
                    resultsArray[0] = DateTime.Now.ToString("t") + " - " + pass + ": " + gauge.TrimStart('0') + " AWG";
                    break;

                case 1:
                case 5:
                    // record pass/fail in direct force test, along with required force
                    resultsArray[0] = DateTime.Now.ToString("t") + " - " + pass + ": " + gauge.TrimStart('0') + " lbs.";
                    break;

                case 2:
                case 3:
                case 6:
                case 7:
                    // record required and reached values in pull-to-break tests
                    resultsArray[0] = DateTime.Now.ToString("t") + " - " + pass + ": " + maxValue + "/" + force + " lbs.";
                    break;

            }
        }


        /// <summary>
        /// printResult method
        /// returns a formatted version of the results array, to be printed to the screen
        /// </summary>
        /// <returns></returns>
        private string printResult()
        {
            string ret = "";
            for (int i = 0; i < resultsArray.Length; i++)
            {
                ret += resultsArray[i] + "\n";
            }
            return ret;
        }

        /// <summary>
        /// resetResultsButton_Click method
        /// event handling method that resets the results array whenever the reset button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void resetResultsButton_Click(object sender, RoutedEventArgs e)
        {
            resetResults();
        }

        private void resetResults()
        {
            for (int i = 0; i < resultsArray.Length; i++)
            {
                resultsArray[i] = "";
            }
            resetUI();
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
            Thread.Sleep(100);

            // halt tester
            com.Write("\\H\r");
            Thread.Sleep(100);

            // move in reverse direction
            if (mode < 4)
            {
                com.Write("\\K\r");
            }
            else
            {
                com.Write("\\J\r");
            }
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
