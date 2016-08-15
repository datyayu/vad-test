using AudioRecognitionTest.Commons;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioRecognitionTest.Helpers
{
    public class AudioRecognition : ObservableBase
    {

        private int sampleRate;
        private int channels;
        private WaveIn waveIn;
        private WaveOut waveOut;
        private WaveInEvent waveInStream;
        private MyFilterProvider filter;
        private ICollection<float[]> frameStack;
        private ICollection<float[]> silenceStack;
        private ICollection<bool> latestDetectionResults;
        private bool voiceIsActive = false;
        private bool thresholdFlag = true;
        private bool test = true;
        //sacarla al config
        private const double K = 1;

        public AudioRecognition(int sampleRate, int channels)
        {
            /*Set the parameters*/
            this.sampleRate = sampleRate;
            this.channels = channels;
            frameStack = new Collection<float[]>();
            silenceStack = new Collection<float[]>();
            latestDetectionResults = new Collection<bool>();
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
                //waveInStream.DeviceNumber = 1;
                waveInStream.WaveFormat = new WaveFormat(sampleRate, channels);
                WaveInCapabilities deviceInfo1 = NAudio.Wave.WaveIn.GetCapabilities(0);
                //WaveInCapabilities deviceInfo2 = NAudio.Wave.WaveIn.GetCapabilities(1);
                waveInStream.DataAvailable += waveIn_DataAvailable;       //EventHandler it triggers when the dataBuffer gets the AudioSamples from the IO Device (Microphone)
                
                /*To not duplicate work*/
                filter = new MyFilterProvider(waveInStream.WaveFormat.Channels, 8800, 200);

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

        private const int FRAMES_PER_EVENT_8K = 50;
        private const int FRAMES_PER_EVENT_16K = 100;

        private const int FRAMES_PER_COMPARISON = 50;
        private const int MIN_FRAMES_FOR_VERIFICATION = 5;
        private double threshold = 0;
        private double eSilence = 0;


        private void waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            int i = 0;
            float[] audioDataFrame_32 = new float[SAMPLES_PER_FRAME_8K];
            short[] audioDataFrame_16 = new short[SAMPLES_PER_FRAME_8K];

            for (int index = 0; index < (e.BytesRecorded); index += 2)
            {
                short sample_16 = (short)((e.Buffer[index + 1] << 8) | e.Buffer[index + 0]);
                float sample_32 = sample_16 / 32768f;

                if ((index % (BYTES_PER_FRAME_8K)) == 0 && index > 0)
                {
                    //FRAME READY
                    noiseReduction(audioDataFrame_32, 0, SAMPLES_PER_FRAME_8K);
                    //add this frame to the frameStack
                    frameStack.Add(audioDataFrame_32);

                    if ( frameStack.Count >= FRAMES_PER_COMPARISON)
                    {
                        if (thresholdFlag)
                        {
                            threshold = calculateThreshold(getCurrentFrameStackEnergy());
                            ThresholdDetected = threshold.ToString();
                            thresholdFlag = false;
                        }
                        else
                        {
                            // Check current frame for Voice Activity.
                            bool isFrameActive = checkForVoiceActivity(audioDataFrame_32, threshold, SAMPLES_PER_FRAME_8K);
                            //Add the result no matter if it was true or false
                            latestDetectionResults.Add(isFrameActive);

                            //chech the frame to see if we want to update the Threshold 
                            if (!(isFrameActive))
                            {
                                if (!(silenceStack.Count >= 10))
                                {
                                    silenceStack.Add(audioDataFrame_32);
                                }
                                else
                                {
                                    eSilence = calculateThreshold(getCurrentFrameStackEnergy());
                                    threshold = updateThreshold(threshold, eSilence);
                                    ThresholdDetected = threshold.ToString();
                                    silenceStack.Clear();
                                    test = false;
                                }
                            }
                        }
                        updateStacks();
                        confirmVoiceActivity(audioDataFrame_32);
                        
                    }
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

        private void confirmVoiceActivity(float[] audioDataFrame_32)
        {
            // Voice was detected only if all the latest frames had a positive detection.
            if (frameStack.Count >= FRAMES_PER_COMPARISON && latestDetectionResults.Take(5).All(result => result))
            {
                VadDetected = "Yes";
                voiceIsActive = true;
                test = false;
            }
            else
            {
                VadDetected = "No";
                if (voiceIsActive)
                {
                    if (silenceStack.Count >= 5)
                    {
                        eSilence = getFrameEnergy(audioDataFrame_32, SAMPLES_PER_FRAME_8K);
                        threshold = updateThreshold(threshold, eSilence);
                        ThresholdDetected = threshold.ToString();
                        voiceIsActive = false;
                    }
                }
            }
        }


        private void updateStacks()
        {
            if (frameStack.Count > FRAMES_PER_COMPARISON)
            {
                frameStack.Remove(frameStack.First());
            }

            if (latestDetectionResults.Count > (MIN_FRAMES_FOR_VERIFICATION))
            {
                latestDetectionResults.Remove(latestDetectionResults.First());
            }
        }

        private double[] getCurrentFrameStackEnergy()
        {
            return (frameStack.Take(FRAMES_PER_COMPARISON)
                    .Select(frame => getFrameEnergy(frame, SAMPLES_PER_FRAME_8K))
                    .ToArray());
        }

        private const double P  = 0.5;
        private double updateThreshold(double threshold, double eSilence)
        {
            return (((((double)1 - P) * threshold) + (P * eSilence)));
        }

        private void noiseReduction(float[] audioDataFrame_32, int offset, int sampleCount)
        {
            filter.Read(audioDataFrame_32, offset, sampleCount);
        }


        private double getFrameEnergy(float[] frameSamples, int sampleCount)
        {
            // Formula original
            //           jk
            // E = (1/k) ∑ [x(i)]^2
            //       i=(j-1)K+ 1
            //
            // E => Energia del frame actual
            // k => cantidad de samples
            // j => numero de frame
            // x(i) => nth sample

            // Formula modificada (trata los frames de manera individual).
            //           k
            // E = (1/k) ∑ [x(i)]^2
            //          i=1

            double EnergySum = 0;
            double k = (((double)1.0) / sampleCount);

            for (int i=1; i < sampleCount; i++)
            {
                EnergySum += Math.Pow(frameSamples[i], 2);
                //EnergySum += frameSamples[i] * frameSamples[i];
            }
            return (k * EnergySum);
        } 


        private double calculateThreshold(double[] framesEnergy)
        {
            double framesEnergySum = 0;
            for (int i = 1; i < framesEnergy.Length; i++)
            {
                framesEnergySum += framesEnergy[i];
            }
            return (framesEnergySum / framesEnergy.Length);
        }

        private bool checkForVoiceActivity(float[] frame, double threshold, int sampleCount)
        {
            double frameEnergy = getFrameEnergy(frame, sampleCount);
            bool voiceActivityFound = frameEnergy > K * threshold;
            return voiceActivityFound;
        }

        private string vadDetected { get; set; }
        public string VadDetected
        {
            get
            {
                return vadDetected;
            }
            set
            {
                vadDetected = value;
                OnPropertyChanged();
            }
        }

        private string thresholdDetected { get; set; }
        public string ThresholdDetected
        {
            get
            {
                return thresholdDetected;
            }
            set
            {
                thresholdDetected = value;
                OnPropertyChanged();
            }
        }



    }
}
