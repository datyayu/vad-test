using NAudio.Dsp;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioTest
{
    class Program
    {

        public static void Main(string[] args)
        {
            AudioFileReader read = new AudioFileReader("C:\\Users\\Jaime\\Desktop\\rec.wav");
            var filter = new MyWaveProvider(read, 8800, 200); // reader is the source for filter
            var waveOut = new WaveOut();
            waveOut.Init(filter); // filter is the source for waveOut
            waveOut.Play();
            System.Console.ReadLine();

        }
    }
}
