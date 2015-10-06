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
using System.Management;
using BuzzIO;
using System.Timers;
using System.IO;
using System.Data.OleDb;
using System.Data;
using HidSharp;
using System.Windows.Threading;
using System.Diagnostics;

namespace MetagamerBuzzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Timer to check if the Dongle is connected
        private Timer timer_WBuzz;

        // Timer to reset the buzzer priorities (to allow a team to buzz again)
        private Timer timer_Priorities;

        // Bools to prevent buzz spamming (can only buzz when true)
        bool team1CanBuzz = true, team2CanBuzz = true, team3CanBuzz = true;

        // List of questions to be retrieved for each set
        private List<Element> questionsManche1 = new List<Element>();
        private List<Element> musiquesManche2 = new List<Element>();
        private List<List<Element>> elementsManche3 = new List<List<Element>>();

        // Question currently asked
        private Element currentElement = null;

        // Counters to browse the questions lists
        int compteurManche1 = 0, compteurManche2 = 0, compteurQuestionsManche3 = 0, compteurMusiquesManche3 = 0, compteurDevinettesManche3 = 0;

        // Static properties to directly access the buzzers
        static List<IBuzzHandsetDevice> handsets;
        static BuzzHandsetDevice buzzers;

        // Windows Media Player to play all the sounds needed
        private WMPLib.WindowsMediaPlayer wplayer;

        public MainWindow()
        {
            InitializeComponent();
            retrieveQuestions();
            InitWBuzzTimer();
            timer_WBuzz_Tick(null, null);
            InitWBuzzReceiver();
            InitPrioritiesTimer();
            handsets = new BuzzHandsetFinder().FindHandsets().ToList();
            buzzers = (BuzzHandsetDevice)handsets[0];
            buzzers.ButtonChanged += BuzzButtonChangedEventHandler;
        }

        /// <summary>
        /// Logic when a buzz is triggered by a participant
        /// </summary>
        private void BuzzButtonChangedEventHandler(object sender, BuzzButtonChangedEventArgs args)
        {
            if ((args.Buttons[0].Red && team1CanBuzz) || (args.Buttons[1].Red && team2CanBuzz) || (args.Buttons[2].Red && team3CanBuzz))
            {
                buzzers.ButtonChanged -= BuzzButtonChangedEventHandler;
                if (args.Buttons[0].Red == true)
                {
                    playRandomSound(1);
                    buzzers.SetLights(true, false, false, false);
                    if (!Dispatcher.CheckAccess())
                    {
                        Dispatcher.Invoke(
                               () => this.team1BuzzAlert.Visibility = System.Windows.Visibility.Visible, DispatcherPriority.Normal);
                        Dispatcher.Invoke(
                               () => this.team1RightAnswer.IsEnabled = true, DispatcherPriority.Normal);
                        Dispatcher.Invoke(
                               () => this.team1WrongAnswer.IsEnabled = true, DispatcherPriority.Normal);
                    }
                }
                else if (args.Buttons[1].Red == true)
                {
                    playRandomSound(2);
                    buzzers.SetLights(false, true, false, false);
                    if (!Dispatcher.CheckAccess())
                    {
                        Dispatcher.Invoke(
                               () => this.team2BuzzAlert.Visibility = System.Windows.Visibility.Visible, DispatcherPriority.Normal);
                        Dispatcher.Invoke(
                               () => this.team2RightAnswer.IsEnabled = true, DispatcherPriority.Normal);
                        Dispatcher.Invoke(
                               () => this.team2WrongAnswer.IsEnabled = true, DispatcherPriority.Normal);
                    }
                }
                else if (args.Buttons[2].Red == true)
                {
                    playRandomSound(3);
                    buzzers.SetLights(false, false, true, false);
                    if (!Dispatcher.CheckAccess())
                    {
                        Dispatcher.Invoke(
                               () => this.team3BuzzAlert.Visibility = System.Windows.Visibility.Visible, DispatcherPriority.Normal);
                        Dispatcher.Invoke(
                               () => this.team3RightAnswer.IsEnabled = true, DispatcherPriority.Normal);
                        Dispatcher.Invoke(
                               () => this.team3WrongAnswer.IsEnabled = true, DispatcherPriority.Normal);
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves all questions from .xlsx files and adds them to the lists of questions
        /// </summary>
        public void retrieveQuestions()
        {
            //Defining necessary strings to access the Excel sheet
            var fileName = string.Format("{0}\\Questions.xlsx", Directory.GetCurrentDirectory());
            var connectionString = string.Format("Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0}; Extended Properties=Excel 12.0;", fileName);

            ////////////////////////////////////////////
            ////////////////////////////////////////////
            //--------------- FIRST SET --------------//
            ////////////////////////////////////////////
            ////////////////////////////////////////////
            var adapterManche1 = new OleDbDataAdapter("SELECT * FROM [Questions$]", connectionString);
            var dsManche1 = new DataSet();

            adapterManche1.Fill(dsManche1, "Questions");

            var data = dsManche1.Tables["Questions"].AsEnumerable();

            //Queries all questions of the first set and adds them to the appropriate list
            var queryManche1 = data.Select(x =>
                new Question(Convert.ToInt32(x.ItemArray[0]), x.ItemArray[1].ToString(), x.ItemArray[2].ToString(), Convert.ToInt32(x.ItemArray[3]))
            );
            queryManche1.ToList().ForEach(i => questionsManche1.Add(i));

            ////////////////////////////////////////////
            ////////////////////////////////////////////
            //-------------- SECOND SET --------------//
            ////////////////////////////////////////////
            ////////////////////////////////////////////
            var adapterManche2 = new OleDbDataAdapter("SELECT * FROM [Musiques$]", connectionString);
            var dsManche2 = new DataSet();

            adapterManche2.Fill(dsManche2, "Musiques");

            var dataManche2 = dsManche2.Tables["Musiques"].AsEnumerable();

            //Queries all musics of the second set and adds them to the appropriate list
            var queryManche2 = data.Select(x =>
                new Musique(Convert.ToInt32(x.ItemArray[0]), x.ItemArray[1].ToString(), x.ItemArray[2].ToString(), Convert.ToInt32(x.ItemArray[3]))
            );
            queryManche2.ToList().ForEach(i => musiquesManche2.Add(i));

            ////////////////////////////////////////////
            ////////////////////////////////////////////
            //---------- THIRD SET QUESTIONS ---------//
            ////////////////////////////////////////////
            ////////////////////////////////////////////
            List<Element> questionsManche3 = new List<Element>();
            var adapterQuestionsManche3 = new OleDbDataAdapter("SELECT * FROM [Manche 3 - Questions$]", connectionString);
            var dsQuestionsManche3 = new DataSet();

            adapterQuestionsManche3.Fill(dsQuestionsManche3, "Manche 3 - Questions");

            var dataQuestionsManche3 = dsQuestionsManche3.Tables["Manche 3 - Questions"].AsEnumerable();

            //Queries all questions of the third set and adds them to the appropriate list
            var queryQuestionsManche3 = data.Select(x =>
                new Question(Convert.ToInt32(x.ItemArray[0]), x.ItemArray[1].ToString(), x.ItemArray[2].ToString(), Convert.ToInt32(x.ItemArray[3]))
            );
            queryQuestionsManche3.ToList().ForEach(i => questionsManche3.Add(i));
            elementsManche3.Add(questionsManche3);

            ////////////////////////////////////////////
            ////////////////////////////////////////////
            //---------- THIRD SET MUSIQUES ---------//
            ////////////////////////////////////////////
            ////////////////////////////////////////////
            List<Element> musiquesManche3 = new List<Element>();
            var adapterMusiquesManche3 = new OleDbDataAdapter("SELECT * FROM [Manche 3 - Musiques$]", connectionString);
            var dsMusiquesManche3 = new DataSet();

            adapterMusiquesManche3.Fill(dsMusiquesManche3, "Manche 3 - Musiques");

            var dataMusiquesManche3 = dsMusiquesManche3.Tables["Manche 3 - Musiques"].AsEnumerable();

            //Queries all musics of the third set and adds them to the appropriate list
            var queryMusiquesManche3 = data.Select(x =>
                new Musique(Convert.ToInt32(x.ItemArray[0]), x.ItemArray[1].ToString(), x.ItemArray[2].ToString(), Convert.ToInt32(x.ItemArray[3]))
            );
            queryMusiquesManche3.ToList().ForEach(i => musiquesManche3.Add(i));
            elementsManche3.Add(musiquesManche3);

            ////////////////////////////////////////////
            ////////////////////////////////////////////
            //---------- THIRD SET DEVINETTES --------//
            ////////////////////////////////////////////
            ////////////////////////////////////////////
            List<Element> devinettesManche3 = new List<Element>();
            var adapterDevinettesManche3 = new OleDbDataAdapter("SELECT * FROM [Manche 3 - Devinettes$]", connectionString);
            var dsDevinettesManche3 = new DataSet();

            adapterDevinettesManche3.Fill(dsDevinettesManche3, "Manche 3 - Devinettes");

            var dataDevinettesManche3 = dsDevinettesManche3.Tables["Manche 3 - Devinettes"].AsEnumerable();

            //Queries all riddles of the third set and adds them to the appropriate list
            var queryDevinettesManche3 = data.Select(x =>
                new Devinette(Convert.ToInt32(x.ItemArray[0]), x.ItemArray[1].ToString(), x.ItemArray[2].ToString(), Convert.ToInt32(x.ItemArray[3]))
            );
            queryDevinettesManche3.ToList().ForEach(i => devinettesManche3.Add(i));
            elementsManche3.Add(devinettesManche3);
        }

        /// <summary>
        /// Iinitiates timer to check on the dongle's status
        /// </summary>
        public void InitWBuzzTimer()
        {
            timer_WBuzz = new Timer(); // Creates the timer
            timer_WBuzz.Elapsed += new ElapsedEventHandler(timer_WBuzz_Tick); // Adds the timer_WBuzz_Tick as an event every time the buzzer ticks
            timer_WBuzz.Interval = 1000; // Buzzer will tick every second
        }

        /// <summary>
        /// Iinitiates timer to check if a team can buzz again after a wrong answer
        /// </summary>
        public void InitPrioritiesTimer()
        {
            timer_Priorities = new Timer(); // Creates the timer
            timer_Priorities.Elapsed += new ElapsedEventHandler(timer_Priorities_Tick); // Adds the timer_Priorities_Tick as an event every time the buzzer ticks
            timer_Priorities.Interval = 5000; // Buzzer will tick every 5 seconds
        }

        /// <summary>
        /// Event trigerred every time timer_WBuzz ticks. Checks if the dongle still is connected.
        /// </summary>
        private void timer_WBuzz_Tick(object sender, EventArgs e)
        {
            bool device = IsUsbDeviceConnected("1000", "054C"); // Checks if the dongle is connected
            if (device == false) // If it is not...
            {
                do
                {
                    timer_WBuzz.Stop(); // Toki yo tomare!
                    MessageBoxResult result = MessageBox.Show("Veuillez insérer le dongle USB puis réessayer.", "Dongle non connecté", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                    if (result == MessageBoxResult.Yes)
                    {
                        device = IsUsbDeviceConnected("1000", "054C"); // Checks if he did ; if not, it loops until it is plugged
                    }
                    else if (result == MessageBoxResult.No)
                    {
                        Process.GetCurrentProcess().Kill();
                    }
                } while (device == false);
                InitWBuzzReceiver(); // Initializes the dongle to receive buzzer inputs
            }
            timer_WBuzz.Start(); // Soshite, toki wa ugoki dasu.
        }

        /// <summary>
        /// Event trigerred every time timer_Priorities ticks. Allows everyone to buzz again.
        /// </summary>
        private void timer_Priorities_Tick(object sender, EventArgs e)
        {
            team1CanBuzz = true;
            team2CanBuzz = true;
            team3CanBuzz = true;
            timer_Priorities.Stop();
        }

        /// <summary>
        /// Initializes the dongle to receive buzzer inputs. Uses HidSharp Library.
        /// </summary>
        public void InitWBuzzReceiver()
        {
            // Looks for the Device according to its VID and PID, then tries to open a stream to write to it
            var loader = new HidDeviceLoader();
            var device = loader.GetDevices(0x054C, 0x1000).First();
            HidStream stream;
            device.TryOpen(out stream);

            // Sends a 7-byte message to make it ready to communicate with the buzzers
            var message = new byte[] { 0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            stream.Write(message);
            stream.Close();
        }

        /// <summary>
        /// Method to check if a device is connected. Primarily used to check on the dongle.
        /// </summary>
        public bool IsUsbDeviceConnected(string pid, string vid)
        {
            using (var searcher =
              new ManagementObjectSearcher(@"Select * From Win32_USBControllerDevice"))
            {
                using (var collection = searcher.Get())
                {
                    foreach (var device in collection)
                    {
                        var usbDevice = Convert.ToString(device);

                        if (usbDevice.Contains("PID_" + pid) && usbDevice.Contains("VID_" + vid))
                            return true;
                    }
                }
            }
            return false;
        }

        #region Right Answers
        /// <summary>
        /// Event triggered when team 1 gave a good answer
        /// </summary>
        private void team1RightAnswer_Click(object sender, RoutedEventArgs e)
        {
            playSound(10);
            if (currentElement != null)
            {
                File.WriteAllText("reponse.txt", currentElement.reponse);
            }
            this.team1BuzzAlert.Visibility = System.Windows.Visibility.Hidden;
            this.team1RightAnswer.IsEnabled = false;
            this.team1WrongAnswer.IsEnabled = false;
            buzzers.SetLights(false, false, false, false);
            buzzers.ButtonChanged += BuzzButtonChangedEventHandler;
        }

        /// <summary>
        /// Event triggered when team 2 gave a good answer
        /// </summary>
        private void team2RightAnswer_Click(object sender, RoutedEventArgs e)
        {
            playSound(10);
            if (currentElement != null)
            {
                File.WriteAllText("reponse.txt", currentElement.reponse);
            }
            this.team2BuzzAlert.Visibility = System.Windows.Visibility.Hidden;
            this.team2RightAnswer.IsEnabled = false;
            this.team2WrongAnswer.IsEnabled = false;
            buzzers.SetLights(false, false, false, false);
            buzzers.ButtonChanged += BuzzButtonChangedEventHandler;
        }

        /// <summary>
        /// Event triggered when team 3 gave a good answer
        /// </summary>
        private void team3RightAnswer_Click(object sender, RoutedEventArgs e)
        {
            playSound(10);
            if (currentElement != null)
            {
                File.WriteAllText("reponse.txt", currentElement.reponse);
            }
            this.team3BuzzAlert.Visibility = System.Windows.Visibility.Hidden;
            this.team3RightAnswer.IsEnabled = false;
            this.team3WrongAnswer.IsEnabled = false;
            buzzers.SetLights(false, false, false, false);
            buzzers.ButtonChanged += BuzzButtonChangedEventHandler;
        }
        #endregion

        #region Wrong Answers
        /// <summary>
        /// Event triggered when team 1 gave a wrong answer
        /// </summary>
        private void team1WrongAnswer_Click(object sender, RoutedEventArgs e)
        {
            this.team1BuzzAlert.Visibility = System.Windows.Visibility.Hidden;
            this.team1RightAnswer.IsEnabled = false;
            this.team1WrongAnswer.IsEnabled = false;
            timer_Priorities.Start();
            team1CanBuzz = false;
            team2CanBuzz = true;
            team3CanBuzz = true;
            buzzers.SetLights(false, false, false, false);
            buzzers.ButtonChanged += BuzzButtonChangedEventHandler;
        }

        /// <summary>
        /// Event triggered when team 2 gave a wrong answer
        /// </summary>
        private void team2WrongAnswer_Click(object sender, RoutedEventArgs e)
        {
            this.team2BuzzAlert.Visibility = System.Windows.Visibility.Hidden;
            this.team2RightAnswer.IsEnabled = false;
            this.team2WrongAnswer.IsEnabled = false;
            timer_Priorities.Start();
            team1CanBuzz = true;
            team2CanBuzz = false;
            team3CanBuzz = true;
            buzzers.SetLights(false, false, false, false);
            buzzers.ButtonChanged += BuzzButtonChangedEventHandler;
        }

        /// <summary>
        /// Event triggered when team 3 gave a wrong answer
        /// </summary>
        private void team3WrongAnswer_Click(object sender, RoutedEventArgs e)
        {
            this.team3BuzzAlert.Visibility = System.Windows.Visibility.Hidden;
            this.team3RightAnswer.IsEnabled = false;
            this.team3WrongAnswer.IsEnabled = false;
            timer_Priorities.Start();
            team1CanBuzz = true;
            team2CanBuzz = true;
            team3CanBuzz = false;
            buzzers.SetLights(false, false, false, false);
            buzzers.ButtonChanged += BuzzButtonChangedEventHandler;
        }
        #endregion

        #region Manche 1 buttons
        /// <summary>
        /// Event triggered when the Manche 1 "Question suivante" button is pressed
        /// </summary>
        private void manche1Button_Click(object sender, RoutedEventArgs e)
        {
            playSound(0);
            compteurManche1++;

            currentElement = questionsManche1[compteurManche1-1];
            File.WriteAllText("question.txt", ((Question)currentElement).question);
            File.WriteAllText("reponse.txt", "");

            this.manche1Number.Content = compteurManche1;
            this.manche1Ratio.Content = compteurManche1 + "/" + questionsManche1.Count().ToString();
            this.manche1Points.Content = ((Question)questionsManche1[compteurManche1-1]).points.ToString();
            if (compteurManche1 == 1)
            {
                this.manche1Button.Content = "Question suivante";
                this.manche1Previous.IsEnabled = false;
                this.manche1Next.IsEnabled = true;
                this.manche1LabelQuestion.Visibility = System.Windows.Visibility.Visible;
                this.manche1LabelPoints.Visibility = System.Windows.Visibility.Visible;
                this.manche1Number.Visibility = System.Windows.Visibility.Visible;
                this.manche1Ratio.Visibility = System.Windows.Visibility.Visible;
            }
            else if (compteurManche1 == questionsManche1.Count())
            {
                this.manche1Button.IsEnabled = false;
                this.manche1Previous.IsEnabled = true;
                this.manche1Next.IsEnabled = false;
            }
            else
            {
                this.manche1Button.IsEnabled = true;
                this.manche1Previous.IsEnabled = true;
                this.manche1Next.IsEnabled = true;
            }
        }

        /// <summary>
        /// Event triggered when the Manche 1 "Previous" button is pressed
        /// </summary>
        private void manche1Previous_Click(object sender, RoutedEventArgs e)
        {
            compteurManche1--;

            currentElement = questionsManche1[compteurManche1-1];
            File.WriteAllText("question.txt", ((Question)currentElement).question);
            File.WriteAllText("reponse.txt", "");

            this.manche1Number.Content = compteurManche1;
            this.manche1Ratio.Content = compteurManche1 + "/" + questionsManche1.Count().ToString();
            this.manche1Points.Content = ((Question)questionsManche1[compteurManche1 - 1]).points.ToString();
            if (compteurManche1 == 1)
            {
                this.manche1Previous.IsEnabled = false;
            }
            this.manche1Button.IsEnabled = true;
            this.manche1Next.IsEnabled = true;
        }

        /// <summary>
        /// Event triggered when the Manche 1 "Next" button is pressed
        /// </summary>
        private void manche1Next_Click(object sender, RoutedEventArgs e)
        {
            compteurManche1++;

            currentElement = questionsManche1[compteurManche1-1];
            File.WriteAllText("question.txt", ((Question)currentElement).question);
            File.WriteAllText("reponse.txt", "");

            this.manche1Number.Content = compteurManche1;
            this.manche1Ratio.Content = compteurManche1 + "/" + questionsManche1.Count().ToString();
            this.manche1Points.Content = ((Question)questionsManche1[compteurManche1 - 1]).points.ToString();
            if (compteurManche1 == 1)
            {
                this.manche1Button.Content = "Question suivante";
                this.manche1Previous.IsEnabled = false;
                this.manche1Next.IsEnabled = true;
                this.manche1LabelQuestion.Visibility = System.Windows.Visibility.Visible;
                this.manche1LabelPoints.Visibility = System.Windows.Visibility.Visible;
                this.manche1Number.Visibility = System.Windows.Visibility.Visible;
                this.manche1Ratio.Visibility = System.Windows.Visibility.Visible;
            }
            else if (compteurManche1 == questionsManche1.Count())
            {
                this.manche1Button.IsEnabled = false;
                this.manche1Previous.IsEnabled = true;
                this.manche1Next.IsEnabled = false;
            }
            else
            {
                this.manche1Button.IsEnabled = true;
                this.manche1Previous.IsEnabled = true;
                this.manche1Next.IsEnabled = true;
            }
        }
        #endregion

        #region Manche 2 buttons
        /// <summary>
        /// Event triggered when the Manche 2 "Musique suivante" button is pressed
        /// </summary>
        private void manche2Button_Click(object sender, RoutedEventArgs e)
        {
            playSound(0);
            compteurManche2++;

            currentElement = musiquesManche2[compteurManche2-1];
            File.WriteAllText("question.txt", ((Musique)currentElement).indice);
            File.WriteAllText("reponse.txt", "");

            this.manche2Number.Content = compteurManche2;
            this.manche2Ratio.Content = compteurManche2 + "/" + musiquesManche2.Count().ToString();
            this.manche2Points.Content = ((Musique)musiquesManche2[compteurManche2 - 1]).points.ToString();
            if (compteurManche2 == 1)
            {
                this.manche2Button.Content = "Musique suivante";
                this.manche2Previous.IsEnabled = false;
                this.manche2Next.IsEnabled = true;
                this.manche2LabelQuestion.Visibility = System.Windows.Visibility.Visible;
                this.manche2LabelPoints.Visibility = System.Windows.Visibility.Visible;
                this.manche2Number.Visibility = System.Windows.Visibility.Visible;
                this.manche2Ratio.Visibility = System.Windows.Visibility.Visible;
            }
            else if (compteurManche2 == musiquesManche2.Count())
            {
                this.manche2Button.IsEnabled = false;
                this.manche2Previous.IsEnabled = true;
                this.manche2Next.IsEnabled = false;
            }
            else
            {
                this.manche2Button.IsEnabled = true;
                this.manche2Previous.IsEnabled = true;
                this.manche2Next.IsEnabled = true;
            }
        }

        /// <summary>
        /// Event triggered when the Manche 2 "Previous" button is pressed
        /// </summary>
        private void manche2Previous_Click(object sender, RoutedEventArgs e)
        {
            compteurManche2--;

            currentElement = musiquesManche2[compteurManche2-1];
            File.WriteAllText("question.txt", ((Musique)currentElement).indice);
            File.WriteAllText("reponse.txt", "");

            this.manche2Number.Content = compteurManche2;
            this.manche2Ratio.Content = compteurManche2 + "/" + musiquesManche2.Count().ToString();
            this.manche2Points.Content = ((Musique)musiquesManche2[compteurManche2 - 1]).points.ToString();
            if (compteurManche2 == 1)
            {
                this.manche2Previous.IsEnabled = false;
            }
            this.manche2Button.IsEnabled = true;
            this.manche2Next.IsEnabled = true;
        }

        /// <summary>
        /// Event triggered when the Manche 2 "Next" button is pressed
        /// </summary>
        private void manche2Next_Click(object sender, RoutedEventArgs e)
        {
            compteurManche2++;

            currentElement = musiquesManche2[compteurManche2-1];
            File.WriteAllText("question.txt", ((Musique)currentElement).indice);
            File.WriteAllText("reponse.txt", "");

            this.manche2Number.Content = compteurManche2;
            this.manche2Ratio.Content = compteurManche2 + "/" + musiquesManche2.Count().ToString();
            this.manche2Points.Content = ((Musique)musiquesManche2[compteurManche2 - 1]).points.ToString();
            if (compteurManche2 == 1)
            {
                this.manche2Button.Content = "Musique suivante";
                this.manche2Previous.IsEnabled = false;
                this.manche2Next.IsEnabled = true;
                this.manche2LabelQuestion.Visibility = System.Windows.Visibility.Visible;
                this.manche2LabelPoints.Visibility = System.Windows.Visibility.Visible;
                this.manche2Number.Visibility = System.Windows.Visibility.Visible;
                this.manche2Ratio.Visibility = System.Windows.Visibility.Visible;
            }
            else if (compteurManche2 == musiquesManche2.Count())
            {
                this.manche2Button.IsEnabled = false;
                this.manche2Previous.IsEnabled = true;
                this.manche2Next.IsEnabled = false;
            }
            else
            {
                this.manche2Button.IsEnabled = true;
                this.manche2Previous.IsEnabled = true;
                this.manche2Next.IsEnabled = true;
            }
        }
        #endregion

        #region Manche 3 buttons
        /// <summary>
        /// Event triggered when the Manche 3 "X suivante" button is pressed
        /// </summary>
        private void manche3Button_Click(object sender, RoutedEventArgs e)
        {
            if (manche3Question.IsChecked == true)
            {
                playSound(0);
                compteurQuestionsManche3++;

                currentElement = elementsManche3[0][compteurQuestionsManche3-1];
                File.WriteAllText("question.txt", ((Question)currentElement).question);
                File.WriteAllText("reponse.txt", "");

                this.manche3LabelElement.Content = "Question n°" + compteurQuestionsManche3;
                if (compteurQuestionsManche3 == 1)
                {
                    this.manche3Button.Content = "Question suivante";
                    this.manche3Previous.IsEnabled = false;
                    this.manche3Next.IsEnabled = true;
                    this.manche3LabelElement.Visibility = System.Windows.Visibility.Visible;
                }
                else if (compteurQuestionsManche3 == elementsManche3[0].Count())
                {
                    this.manche3Button.IsEnabled = false;
                    this.manche3Previous.IsEnabled = true;
                    this.manche3Next.IsEnabled = false;
                }
                else
                {
                    this.manche3Button.IsEnabled = true;
                    this.manche3Previous.IsEnabled = true;
                    this.manche3Next.IsEnabled = true;
                }
            }
            else if (manche3Musique.IsChecked == true)
            {
                playSound(0);
                compteurMusiquesManche3++;

                currentElement = elementsManche3[1][compteurMusiquesManche3-1];
                File.WriteAllText("question.txt", ((Musique)currentElement).indice);
                File.WriteAllText("reponse.txt", "");

                this.manche3LabelElement.Content = "Musique n°" + compteurMusiquesManche3;
                if (compteurQuestionsManche3 == 1)
                {
                    this.manche3Button.Content = "Musique suivante";
                    this.manche3Previous.IsEnabled = false;
                    this.manche3Next.IsEnabled = true;
                    this.manche3LabelElement.Visibility = System.Windows.Visibility.Visible;
                }
                else if (compteurMusiquesManche3 == elementsManche3[1].Count())
                {
                    this.manche3Button.IsEnabled = false;
                    this.manche3Previous.IsEnabled = true;
                    this.manche3Next.IsEnabled = false;
                }
                else
                {
                    this.manche3Button.IsEnabled = true;
                    this.manche3Previous.IsEnabled = true;
                    this.manche3Next.IsEnabled = true;
                }
            }
            else if (manche3Devinette.IsChecked == true)
            {
                playSound(0);
                compteurDevinettesManche3++;

                currentElement = elementsManche3[2][compteurDevinettesManche3-1];
                File.WriteAllText("question.txt", ((Devinette)currentElement).indice);
                File.WriteAllText("reponse.txt", "");

                this.manche3LabelElement.Content = "Devinette n°" + compteurDevinettesManche3;
                if (compteurDevinettesManche3 == 1)
                {
                    this.manche3Button.Content = "Devinette suivante";
                    this.manche3Previous.IsEnabled = false;
                    this.manche3Next.IsEnabled = true;
                    this.manche3LabelElement.Visibility = System.Windows.Visibility.Visible;
                }
                else if (compteurDevinettesManche3 == elementsManche3[2].Count())
                {
                    this.manche3Button.IsEnabled = false;
                    this.manche3Previous.IsEnabled = true;
                    this.manche3Next.IsEnabled = false;
                }
                else
                {
                    this.manche3Button.IsEnabled = true;
                    this.manche3Previous.IsEnabled = true;
                    this.manche3Next.IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// Event triggered when the Manche 3 "Previous" button is pressed
        /// </summary>
        private void manche3Previous_Click(object sender, RoutedEventArgs e)
        {
            if (manche3Question.IsChecked == true)
            {
                compteurQuestionsManche3--;

                currentElement = elementsManche3[0][compteurQuestionsManche3-1];
                File.WriteAllText("question.txt", ((Question)currentElement).question);
                File.WriteAllText("reponse.txt", "");

                this.manche3LabelElement.Content = "Question n°" + compteurQuestionsManche3;
                if (compteurQuestionsManche3 == 1)
                {
                    this.manche3Previous.IsEnabled = false;
                }
                this.manche3Button.IsEnabled = true;
                this.manche3Next.IsEnabled = true;
            }
            else if (manche3Musique.IsChecked == true)
            {
                compteurMusiquesManche3--;

                currentElement = elementsManche3[1][compteurMusiquesManche3-1];
                File.WriteAllText("question.txt", ((Musique)currentElement).indice);
                File.WriteAllText("reponse.txt", "");

                this.manche3LabelElement.Content = "Musique n°" + compteurMusiquesManche3;
                if (compteurMusiquesManche3 == 1)
                {
                    this.manche3Previous.IsEnabled = false;
                }
                this.manche3Button.IsEnabled = true;
                this.manche3Next.IsEnabled = true;
            }
            else if (manche3Devinette.IsChecked == true)
            {
                compteurDevinettesManche3--;

                currentElement = elementsManche3[2][compteurDevinettesManche3-1];
                File.WriteAllText("question.txt", ((Devinette)currentElement).indice);
                File.WriteAllText("reponse.txt", "");

                this.manche3LabelElement.Content = "Devinette n°" + compteurDevinettesManche3;
                if (compteurDevinettesManche3 == 1)
                {
                    this.manche3Previous.IsEnabled = false;
                }
                this.manche3Button.IsEnabled = true;
                this.manche3Next.IsEnabled = true;
            }
        }

        /// <summary>
        /// Event triggered when the Manche 3 "Next" button is pressed
        /// </summary>
        private void manche3Next_Click(object sender, RoutedEventArgs e)
        {
            if (manche3Question.IsChecked == true)
            {
                compteurQuestionsManche3++;

                currentElement = elementsManche3[0][compteurQuestionsManche3-1];
                File.WriteAllText("question.txt", ((Question)currentElement).question);
                File.WriteAllText("reponse.txt", "");

                this.manche3LabelElement.Content = "Question n°" + compteurQuestionsManche3;
                if (compteurQuestionsManche3 == 1)
                {
                    this.manche3Button.Content = "Question suivante";
                    this.manche3Previous.IsEnabled = false;
                    this.manche3Next.IsEnabled = true;
                    this.manche3LabelElement.Visibility = System.Windows.Visibility.Visible;
                }
                else if (compteurQuestionsManche3 == elementsManche3[0].Count())
                {
                    this.manche3Button.IsEnabled = false;
                    this.manche3Previous.IsEnabled = true;
                    this.manche3Next.IsEnabled = false;
                }
                else
                {
                    this.manche3Button.IsEnabled = true;
                    this.manche3Previous.IsEnabled = true;
                    this.manche3Next.IsEnabled = true;
                }
            }
            else if (manche3Musique.IsChecked == true)
            {
                compteurMusiquesManche3++;

                currentElement = elementsManche3[1][compteurMusiquesManche3-1];
                File.WriteAllText("question.txt", ((Musique)currentElement).indice);
                File.WriteAllText("reponse.txt", "");

                this.manche3LabelElement.Content = "Musique n°" + compteurMusiquesManche3;
                if (compteurQuestionsManche3 == 1)
                {
                    this.manche3Button.Content = "Musique suivante";
                    this.manche3Previous.IsEnabled = false;
                    this.manche3Next.IsEnabled = true;
                    this.manche3LabelElement.Visibility = System.Windows.Visibility.Visible;
                }
                else if (compteurMusiquesManche3 == elementsManche3[1].Count())
                {
                    this.manche3Button.IsEnabled = false;
                    this.manche3Previous.IsEnabled = true;
                    this.manche3Next.IsEnabled = false;
                }
                else
                {
                    this.manche3Button.IsEnabled = true;
                    this.manche3Previous.IsEnabled = true;
                    this.manche3Next.IsEnabled = true;
                }
            }
            else if (manche3Devinette.IsChecked == true)
            {
                compteurDevinettesManche3++;

                currentElement = elementsManche3[2][compteurDevinettesManche3-1];
                File.WriteAllText("question.txt", ((Devinette)currentElement).indice);
                File.WriteAllText("reponse.txt", "");

                this.manche3LabelElement.Content = "Devinette n°" + compteurDevinettesManche3;
                if (compteurDevinettesManche3 == 1)
                {
                    this.manche3Button.Content = "Devinette suivante";
                    this.manche3Previous.IsEnabled = false;
                    this.manche3Next.IsEnabled = true;
                    this.manche3LabelElement.Visibility = System.Windows.Visibility.Visible;
                }
                else if (compteurDevinettesManche3 == elementsManche3[2].Count())
                {
                    this.manche3Button.IsEnabled = false;
                    this.manche3Previous.IsEnabled = true;
                    this.manche3Next.IsEnabled = false;
                }
                else
                {
                    this.manche3Button.IsEnabled = true;
                    this.manche3Previous.IsEnabled = true;
                    this.manche3Next.IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// Event triggered when the Manche 3 "Question" radio button is checked
        /// </summary>
        private void manche3Question_Checked(object sender, RoutedEventArgs e)
        {
            if (compteurQuestionsManche3 == 0)
            {
                this.manche3Button.Content = "Commencer";
                this.manche3Previous.IsEnabled = false;
                this.manche3Next.IsEnabled = false;
                this.manche3LabelElement.Visibility = System.Windows.Visibility.Hidden;
            }
            else if (compteurQuestionsManche3 == 1)
            {
                this.manche3LabelElement.Content = "Question n°" + compteurQuestionsManche3;
                this.manche3Button.Content = "Question suivante";
                this.manche3Previous.IsEnabled = false;
                this.manche3Next.IsEnabled = true;
                this.manche3LabelElement.Visibility = System.Windows.Visibility.Visible;
            }
            else if (compteurQuestionsManche3 == elementsManche3[0].Count())
            {
                this.manche3LabelElement.Content = "Question n°" + compteurQuestionsManche3;
                this.manche3Button.IsEnabled = false;
                this.manche3Previous.IsEnabled = true;
                this.manche3Next.IsEnabled = false;
                this.manche3LabelElement.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                this.manche3LabelElement.Content = "Question n°" + compteurQuestionsManche3;
                this.manche3Button.IsEnabled = true;
                this.manche3Previous.IsEnabled = true;
                this.manche3Next.IsEnabled = true;
                this.manche3LabelElement.Visibility = System.Windows.Visibility.Visible;
            }
        }

        /// <summary>
        /// Event triggered when the Manche 3 "Musique" radio button is checked
        /// </summary>
        private void manche3Musique_Checked(object sender, RoutedEventArgs e)
        {
            if (compteurMusiquesManche3 == 0)
            {
                this.manche3Button.Content = "Commencer";
                this.manche3Previous.IsEnabled = false;
                this.manche3Next.IsEnabled = false;
                this.manche3LabelElement.Visibility = System.Windows.Visibility.Hidden;
            }
            else if (compteurMusiquesManche3 == 1)
            {
                this.manche3LabelElement.Content = "Musique n°" + compteurMusiquesManche3;
                this.manche3Button.Content = "Musique suivante";
                this.manche3Previous.IsEnabled = false;
                this.manche3Next.IsEnabled = true;
                this.manche3LabelElement.Visibility = System.Windows.Visibility.Visible;
            }
            else if (compteurMusiquesManche3 == elementsManche3[1].Count())
            {
                this.manche3LabelElement.Content = "Musique n°" + compteurMusiquesManche3;
                this.manche3Button.IsEnabled = false;
                this.manche3Previous.IsEnabled = true;
                this.manche3Next.IsEnabled = false;
                this.manche3LabelElement.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                this.manche3LabelElement.Content = "Musique n°" + compteurMusiquesManche3;
                this.manche3Button.IsEnabled = true;
                this.manche3Previous.IsEnabled = true;
                this.manche3Next.IsEnabled = true;
                this.manche3LabelElement.Visibility = System.Windows.Visibility.Visible;
            }
        }

        /// <summary>
        /// Event triggered when the Manche 3 "Devinette" radio button is checked
        /// </summary>
        private void manche3Devinette_Checked(object sender, RoutedEventArgs e)
        {
            if (compteurDevinettesManche3 == 0)
            {
                this.manche3Button.Content = "Commencer";
                this.manche3Previous.IsEnabled = false;
                this.manche3Next.IsEnabled = false;
                this.manche3LabelElement.Visibility = System.Windows.Visibility.Hidden;
            }
            else if (compteurDevinettesManche3 == 1)
            {
                this.manche3LabelElement.Content = "Devinette n°" + compteurDevinettesManche3;
                this.manche3Button.Content = "Devinette suivante";
                this.manche3Previous.IsEnabled = false;
                this.manche3Next.IsEnabled = true;
                this.manche3LabelElement.Visibility = System.Windows.Visibility.Visible;
            }
            else if (compteurDevinettesManche3 == elementsManche3[1].Count())
            {
                this.manche3LabelElement.Content = "Devinette n°" + compteurDevinettesManche3;
                this.manche3Button.IsEnabled = false;
                this.manche3Previous.IsEnabled = true;
                this.manche3Next.IsEnabled = false;
                this.manche3LabelElement.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                this.manche3LabelElement.Content = "Musique n°" + compteurDevinettesManche3;
                this.manche3Button.IsEnabled = true;
                this.manche3Previous.IsEnabled = true;
                this.manche3Next.IsEnabled = true;
                this.manche3LabelElement.Visibility = System.Windows.Visibility.Visible;
            }
        }
        #endregion

        #region Sounds
        /// <summary>
        /// Method to play a chosen sound
        /// </summary>
        public void playSound(int sound)
        {
            String filename = "";
            switch (sound)
            {
                case 1:
                    filename = "Petit_point.mp3";
                    break;
                case 2:
                    filename = "Moyen_point.mp3";
                    break;
                case 3:
                    filename = "Grand_point.mp3";
                    break;
                case 5:
                    filename = "Gros_point.mp3";
                    break;
                case -1:
                    filename = "Nega_point.mp3";
                    break;
                case 0:
                    filename = "Question.mp3";
                    break;
                case 10:
                    filename = "Correct.mp3";
                    break;
            }
            wplayer = new WMPLib.WindowsMediaPlayer();
            wplayer.URL = "sons\\habillage\\" + filename;
            wplayer.controls.play();
        }

        /// <summary>
        /// Method to play a randomly chosen sound from the buzzer sounds directories
        /// </summary>
        public void playRandomSound(int team)
        {
            String path = "";
            switch (team)
            {
                case 1:
                    path = "sons\\equipe_1";
                    break;
                case 2:
                    path = "sons\\equipe_2";
                    break;
                case 3:
                    path = "sons\\equipe_3";
                    break;
                case 0:
                    path = "sons\\habillage\\no";
                    break;
            }

            var files = new DirectoryInfo(Environment.CurrentDirectory + "\\" + path).GetFiles("*.mp3");
            int index = new Random().Next(0, files.Length);


            string filename = files[index].Name.ToString();

            wplayer = new WMPLib.WindowsMediaPlayer();
            wplayer.URL = path + "\\" + filename;
            wplayer.controls.play();
        }
        #endregion

        #region Team 1 Score changes

        /// <summary>
        /// Adds -1 to Team One's Score
        /// </summary>
        private void team1MinusOne_Click(object sender, RoutedEventArgs e)
        {
            playSound(-1);
            playRandomSound(0);
            team1Score.Text = (Convert.ToInt32(team1Score.Text) - 1).ToString();
            File.WriteAllText("score_1.txt", team1Score.Text);
        }

        /// <summary>
        /// Adds +1 to Team One's Score
        /// </summary>
        private void team1PlusOne_Click(object sender, RoutedEventArgs e)
        {
            playSound(1);
            team1Score.Text = (Convert.ToInt32(team1Score.Text) + 1).ToString();
            File.WriteAllText("score_1.txt", team1Score.Text);
        }

        /// <summary>
        /// Adds +2 to Team One's Score
        /// </summary>
        private void team1PlusTwo_Click(object sender, RoutedEventArgs e)
        {
            playSound(2);
            team1Score.Text = (Convert.ToInt32(team1Score.Text) + 2).ToString();
            File.WriteAllText("score_1.txt", team1Score.Text);
        }

        /// <summary>
        /// Adds +3 to Team One's Score
        /// </summary>
        private void team1PlusThree_Click(object sender, RoutedEventArgs e)
        {
            playSound(3);
            team1Score.Text = (Convert.ToInt32(team1Score.Text) + 3).ToString();
            File.WriteAllText("score_1.txt", team1Score.Text);
        }

        /// <summary>
        /// Adds +5 to Team One's Score
        /// </summary>
        private void team1PlusFive_Click(object sender, RoutedEventArgs e)
        {
            playSound(5);
            team1Score.Text = (Convert.ToInt32(team1Score.Text) + 5).ToString();
            File.WriteAllText("score_1.txt", team1Score.Text);
        }

        /// <summary>
        /// Writes Team One's Score to the file on change
        /// </summary>
        private void team1Score_TextChanged(object sender, TextChangedEventArgs e)
        {
            File.WriteAllText("score_1.txt", team1Score.Text);
        }
        #endregion

        #region Team 2 Score changes

        /// <summary>
        /// Adds -1 to Team Two's Score
        /// </summary>
        private void team2MinusOne_Click(object sender, RoutedEventArgs e)
        {
            playSound(-1);
            playRandomSound(0);
            team2Score.Text = (Convert.ToInt32(team2Score.Text) - 1).ToString();
            File.WriteAllText("score_2.txt", team2Score.Text);
        }

        /// <summary>
        /// Adds +1 to Team Two's Score
        /// </summary>
        private void team2PlusOne_Click(object sender, RoutedEventArgs e)
        {
            playSound(1);
            team2Score.Text = (Convert.ToInt32(team2Score.Text) + 1).ToString();
            File.WriteAllText("score_2.txt", team2Score.Text);
        }

        /// <summary>
        /// Adds +2 to Team Two's Score
        /// </summary>
        private void team2PlusTwo_Click(object sender, RoutedEventArgs e)
        {
            playSound(2);
            team2Score.Text = (Convert.ToInt32(team2Score.Text) + 2).ToString();
            File.WriteAllText("score_2.txt", team2Score.Text);
        }

        /// <summary>
        /// Adds +3 to Team Two's Score
        /// </summary>
        private void team2PlusThree_Click(object sender, RoutedEventArgs e)
        {
            playSound(3);
            team2Score.Text = (Convert.ToInt32(team2Score.Text) + 3).ToString();
            File.WriteAllText("score_2.txt", team2Score.Text);
        }

        /// <summary>
        /// Adds +5 to Team Two's Score
        /// </summary>
        private void team2PlusFive_Click(object sender, RoutedEventArgs e)
        {
            playSound(5);
            team2Score.Text = (Convert.ToInt32(team2Score.Text) + 5).ToString();
            File.WriteAllText("score_2.txt", team2Score.Text);
        }

        /// <summary>
        /// Writes Team Two's Score to the file on change
        /// </summary>
        private void team2Score_TextChanged(object sender, TextChangedEventArgs e)
        {
            File.WriteAllText("score_2.txt", team2Score.Text);
        }
        #endregion

        #region Team 3 Score changes

        /// <summary>
        /// Adds -1 to Team Three's Score
        /// </summary>
        private void team3MinusOne_Click(object sender, RoutedEventArgs e)
        {
            playSound(-1);
            playRandomSound(0);
            team3Score.Text = (Convert.ToInt32(team3Score.Text) - 1).ToString();
            File.WriteAllText("score_3.txt", team3Score.Text);
        }

        /// <summary>
        /// Adds +1 to Team Three's Score
        /// </summary>
        private void team3PlusOne_Click(object sender, RoutedEventArgs e)
        {
            playSound(1);
            team3Score.Text = (Convert.ToInt32(team3Score.Text) + 1).ToString();
            File.WriteAllText("score_3.txt", team3Score.Text);
        }

        /// <summary>
        /// Adds +2 to Team Three's Score
        /// </summary>
        private void team3PlusTwo_Click(object sender, RoutedEventArgs e)
        {
            playSound(2);
            team3Score.Text = (Convert.ToInt32(team3Score.Text) + 2).ToString();
            File.WriteAllText("score_3.txt", team3Score.Text);
        }

        /// <summary>
        /// Adds +3 to Team Three's Score
        /// </summary>
        private void team3PlusThree_Click(object sender, RoutedEventArgs e)
        {
            playSound(3);
            team3Score.Text = (Convert.ToInt32(team3Score.Text) + 3).ToString();
            File.WriteAllText("score_3.txt", team3Score.Text);
        }

        /// <summary>
        /// Adds +5 to Team Three's Score
        /// </summary>
        private void team3PlusFive_Click(object sender, RoutedEventArgs e)
        {
            playSound(5);
            team3Score.Text = (Convert.ToInt32(team3Score.Text) + 5).ToString();
            File.WriteAllText("score_3.txt", team3Score.Text);
        }

        /// <summary>
        /// Writes Team Three's Score to the file on change
        /// </summary>
        private void team3Score_TextChanged(object sender, TextChangedEventArgs e)
        {
            File.WriteAllText("score_3.txt", team3Score.Text);
        }
        #endregion

    }
}
