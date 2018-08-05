using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Net;
using System.Net.Sockets;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.NetworkInformation;

namespace WPFStuff
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string myDrive = @"C:\Users\dando\Google Drive\Classroom\Introduction to Computer Science 1";
        const string dateFormat = "{0:yyyy-MM-dd.HH.mm}.{1:000}";
        const string journalPad1 = "------";
        const string journalPad2 = "======";

        private string journalFilename = "";
        private String filePattern;
        private String timeStamp;
        private String userReferences;
        private string whoAmI;
        private bool saveJournal = false;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            InitializeText();
            OkButton.IsEnabled = saveJournal = false;
            ILearned.Focus();
            ILearned.SelectionStart = ILearned.Text.Length;
            ILearned.SelectionLength = 0;
        }

        public void InitializeText()
        {
            if (FindCurrentJournalFile()) LoadJournal(journalFilename);
        }

        private bool FindCurrentJournalFile()
        {
            filePattern = string.Format(dateFormat, DateTime.Now, 0).Substring(0, 10);
            string[] jFiles = Directory.GetFiles(myDrive, "jnl." + filePattern + "*.txt");
            if (jFiles.Length == 0) return false;
            Array.Sort(jFiles);
            journalFilename = jFiles[0];
            return true;
        }

        public void LoadJournal(string filename)
        {
            StreamReader sr = null;
            SortedSet<string> timeStamps = new SortedSet<string>();
            SortedSet<string> userIds = new SortedSet<string>();
            StringBuilder sb = new StringBuilder();

            userIds.Add(whoAmI);
            try
            {
                int index = 0;
                string returnStr = "";

                sr = new StreamReader(filename);
                string[] lines = sr.ReadToEnd().Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                sr.Close();
                sr = null;

                if (!CompareLine('=' + journalPad2, lines, ref index, ref returnStr)) throw new Exception('=' + journalPad2);
                while (ComparePartial(filePattern, lines, ref index, ref returnStr)) timeStamps.Add(returnStr);
                if (!CompareLine('0' + journalPad1, lines, ref index, ref returnStr)) throw new Exception('0' + journalPad1);
                string userId = lines[index].Split(new char[] { ' ' })[0];
                while (ComparePartial(userId, lines, ref index, ref returnStr)) userIds.Add(returnStr);
                if (!CompareLine('1' + journalPad1, lines, ref index, ref returnStr)) throw new Exception('1' + journalPad1);
                string section1 = ReadSectionText('2' + journalPad1, lines, ref index);
                string section2 = ReadSectionText('3' + journalPad1, lines, ref index);
                string section3 = ReadSectionText('=' + journalPad2, lines, ref index);

                foreach (string ts in timeStamps.Reverse()) sb.AppendFormat("{0}\r\n", ts);
                timeStamp = sb.ToString();
                sb.Clear();

                foreach (string uid in userIds) sb.AppendFormat("{0}\r\n", uid);
                userReferences = sb.ToString();
                sb.Clear();

                ILearned.Text = section1;
                ILiked.Text = section2;
                IDisliked.Text = section3;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Journal Read Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                userIds.Clear();
                timeStamps.Clear();
                if (sr != null)
                {
                    try
                    {
                        sr.Close();
                        sr = null;
                    }
                    catch { }
                }
            }
        }

        public string ReadSectionText(string matchLine, string[] lines, ref int index)
        {
            StringBuilder sb = new StringBuilder();
            string returnStr = "";
            try
            {
                while (!CompareLine(matchLine, lines, ref index, ref returnStr))
                {
                    sb.AppendFormat("{0}\r\n", returnStr);
                    ++index;
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                sb.Clear();
            }
        }

        public bool CompareLine(string matchLine, string[] lines, ref int index, ref string returnStr)
        {
            returnStr = lines[index];
            if (String.Compare(returnStr, matchLine) != 0) return false;
            ++index;
            return true;
        }

        public bool ComparePartial(string matchLine, string[] lines, ref int index, ref string returnStr)
        {
            returnStr = lines[index];
            if (returnStr.IndexOf(matchLine) < 0) return false;
            ++index;
            return true;
        }

        public string StudentName
        {
            get
            {
                string userName = Environment.UserName.ToUpper();
                string pcName = Environment.MachineName.ToLower();
                string domainName = Environment.UserDomainName.ToLower();
                string hostName = Dns.GetHostName();
                string ipAddress = GetLocalIPAddress();

                return whoAmI = string.Format($"{userName} on {domainName}\\{pcName} at {ipAddress}");
            }
        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "No LAN";
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        public string DateToday
        {
            get
            {
                return DateTime.Today.ToString("dddd, MMMM d, yyyy");
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            textBox.Background = new SolidColorBrush(Colors.White);
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            textBox.Background = new SolidColorBrush(Colors.LightGray);
        }

        private bool SaveJournal(ref string errMessage)
        {
            char[] trimChars = { ' ', '\t', '\r', '\n' };

            DateTime now = DateTime.Now;
            StreamWriter sw = null;
            try
            {
                string filename = journalFilename;
                if (filename.Length == 0) filename = string.Format(@"{0}\{1}", myDrive, JournalFilename);

                string currentTimeStamp = string.Format(dateFormat, now, now.Millisecond);
                if (timeStamp.Length > 0) timeStamp = string.Format("{0}\r\n{1}", currentTimeStamp, timeStamp);

                if (userReferences.Length == 0) userReferences = whoAmI;

                sw = new StreamWriter(filename);
                sw.WriteLine("={1}\r\n{0}", timeStamp.TrimEnd(trimChars), journalPad2);
                sw.WriteLine("0{1}\r\n{0}", userReferences.TrimEnd(trimChars), journalPad1);
                sw.WriteLine("1{1}\r\n{0}", ILearned.Text.TrimEnd(trimChars), journalPad1);
                sw.WriteLine("2{1}\r\n{0}", ILiked.Text.TrimEnd(trimChars), journalPad1);
                sw.WriteLine("3{1}\r\n{0}", IDisliked.Text.TrimEnd(trimChars), journalPad1);
                sw.WriteLine("={0}", journalPad2);
                sw.Close();
                errMessage = "";
                OkButton.IsEnabled = saveJournal = false;
                return true;
            }
            catch (Exception ex)
            {
                errMessage = ex.Message;
                return false;
            }
            finally
            {
                if (sw != null)
                {
                    try
                    {
                        sw.Flush();
                        sw.Close();
                        sw = null;
                    }
                    catch { }
                }
            }
        }

        private void MainWin_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (saveJournal)
            {
                string errMessage = "";
                if (SaveJournal(ref errMessage))
                {
                    e.Cancel = false;
                    return;
                }
                if (MessageBox.Show(this, string.Format("{0}\r\n\r\nIs it ok to exit?", errMessage), "Journal Capture Error", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    e.Cancel = false;
                    return;
                }
                e.Cancel = true;
                return;
            }
            e.Cancel = false;
        }

        private string JournalFilename
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                try
                {
                    DateTime dt = DateTime.Now;
                    sb.AppendFormat("jnl." + dateFormat + ".txt", dt, dt.Millisecond);
                    return sb.ToString();
                }
                finally
                {
                    sb.Clear();
                    sb = null;
                }
            }
        }

        private void ResetButtonColor(Button button)
        {
            Color color = (Color)ColorConverter.ConvertFromString("#FFDDDDDD");
            button.Background = new SolidColorBrush(color);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ResetButtonColor(button);
            string errMessage = "";
            if (SaveJournal(ref errMessage))
                Close();
            else
                MessageBox.Show(this, errMessage, "Journal Capture Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void Button_GotFocus(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            button.Background = SystemColors.GradientActiveCaptionBrush;
        }

        private void Button_LostFocus(object sender, RoutedEventArgs e)
        {
            ResetButtonColor(sender as Button);
        }

        private void Journal_TextChanged(object sender, TextChangedEventArgs e)
        {
            saveJournal = true;
            OkButton.IsEnabled = saveJournal;
        }
    }
}
