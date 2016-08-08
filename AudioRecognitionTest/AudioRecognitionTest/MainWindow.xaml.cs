using AudioRecognitionTest.ViewModels;
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

namespace AudioRecognitionTest
{
    public partial class MainWindow : Window
    {
       private AudioRecognitionViewModel audioRecognitionViewModel;

        public MainWindow()
        {
            InitializeComponent();
            audioRecognitionViewModel = new AudioRecognitionViewModel();
            DataContext = audioRecognitionViewModel;
        }

        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            audioRecognitionViewModel.audioRecognition.StartTest(false);        //False to get the samples from the microphone
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            audioRecognitionViewModel.audioRecognition.StopTest(false);         //False to stop the samples from the microphone
        }
    }
}
