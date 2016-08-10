using AudioRecognitionTest.Commons;
using AudioRecognitionTest.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioRecognitionTest.ViewModels
{
    public class AudioRecognitionViewModel 
    {
        public AudioRecognition audioRecognition { get; set; }

        public AudioRecognitionViewModel()
        {
            /*set the test to 8Khz, MONO)*/
            audioRecognition = new AudioRecognition(8000, 1);
        }

    }
}
