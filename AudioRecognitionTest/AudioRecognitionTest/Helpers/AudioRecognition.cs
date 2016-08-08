using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioRecognitionTest.Helpers
{
    public class AudioRecognition
    {

        private int sampleRate;
        private int channels;
        private WaveIn waveIn;
        private WaveOut waveOut;
        private WaveInEvent waveInStream;
        private MyFilterProvider filter;
        private ICollection<float[]> frameStack;

        public AudioRecognition(int sampleRate, int channels)
        {
            /*Set the parameters*/
            this.sampleRate = sampleRate;
            this.channels = channels;
            frameStack = new Collection<float[]>();
        }



        public void StartTest( bool flag)
        {
            if (flag)
            {
                /*Testing Filter with a AudioFile from my Library*/
                AudioFileReader read = new AudioFileReader("C:\\Users\\Jaime\\Desktop\\rec.wav");
                var filter = new MyWaveProvider(read, 8800, 200); // reader is the source for filter
                waveOut = new WaveOut();
                waveOut.Init(filter); // filter is the source for waveOut
                waveOut.Play();
            }
            else
            {
                /*Testing Filter with the build in Microphone (Laptop)*/
                //WaveIn waveIn = new WaveIn();
                waveInStream = new WaveInEvent();                         //Por console application en WPF es simplmente WaveIn
                waveInStream.DeviceNumber = 0;
                waveInStream.WaveFormat = new WaveFormat(sampleRate, channels);
                waveInStream.DataAvailable += waveIn_DataAvailable;       //EventHandler it triggers when the dataBuffer gets the AudioSamples from the IO Device (Microphone)

                waveInStream.StartRecording();

            }
        }

        public void StopTest(bool flag)
        {
            if (flag)
            {
                waveOut.Stop();
            }
            else
            {
                waveInStream.StopRecording();
            }
            
        }

        /*Only When you get the data from the Mic*/
        /*Note: For a 8KHz the FRAME is each 160samples, for 16KHz is 320*/
        private const int SAMPLES_PER_FRAME_8K = 160;
        private const int BYTES_PER_FRAME_8K = 320;
        private const int SAMPLES_PER_FRAME_16K = 320;
        private const int BYTES_PER_FRAME_16K = 640;

        private void waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            int i = 0;
            filter = new MyFilterProvider(waveInStream.WaveFormat.Channels, 8800, 200);
            float[] audioDataFrame_32 = new float[SAMPLES_PER_FRAME_8K];
            short[] audioDataFrame_16 = new short[SAMPLES_PER_FRAME_8K];

            for (int index = 0; index < (e.BytesRecorded); index += 2)
            {
                short sample_16 = (short)((e.Buffer[index + 1] << 8) | e.Buffer[index + 0]);
                float sample_32 = sample_16 / 32768f;

                if ((index % (BYTES_PER_FRAME_8K)) == 0 && index > 0)
                {
                    //FRAME LISTO
                    noiseReduction(audioDataFrame_32, 0, SAMPLES_PER_FRAME_8K);
                    //add this frame to the frameStack
                    frameStack.Add(audioDataFrame_32);

                    //Star filling the next frame
                    audioDataFrame_16[i % SAMPLES_PER_FRAME_8K] = sample_16;
                    audioDataFrame_32[i % SAMPLES_PER_FRAME_8K] = sample_32;
                }
                else
                {
                    audioDataFrame_16[i % SAMPLES_PER_FRAME_8K] = sample_16;
                    audioDataFrame_32[i % SAMPLES_PER_FRAME_8K] = sample_32;
                }
                i++;
            }
        }

        private void noiseReduction(float[] audioDataFrame_32, int offset, int sampleCount)
        {
            filter.Read(audioDataFrame_32, 0, 160);
        }

    }
}
