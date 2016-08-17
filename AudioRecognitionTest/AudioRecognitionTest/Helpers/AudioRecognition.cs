using AudioRecognitionTest.Commons;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioRecognitionTest.Helpers
{
    public class AudioRecognition : ObservableBase
    {

        private int sampleRate;
        private int channels;
        private WaveOut waveOut;
        private WaveInEvent waveInStream;
        private MyFilterProvider filter;
        private ICollection<float[]> frameStack;
        private ICollection<double> silenceEnergyStack;
        private bool initialTrainingFlag = true;
        private ICollection<float> activeStack;
        private MemoryStream memstream = new MemoryStream();

        //sacarla al config
        private const double K = 2.0;

        public AudioRecognition(int sampleRate, int channels)
        {
            /*Set the parameters*/
            this.sampleRate = sampleRate;
            this.channels = channels;
            frameStack = new Collection<float[]>();
            silenceEnergyStack = new Collection<double>();
            activeStack = new Collection<float>();
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
                waveInStream = new WaveInEvent();                       
                waveInStream.DeviceNumber = 0;
                waveInStream.WaveFormat = new WaveFormat(sampleRate, channels);
                WaveInCapabilities deviceInfo1 = NAudio.Wave.WaveIn.GetCapabilities(0);
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
                WaveFileWriter wfr = new WaveFileWriter(memstream, waveInStream.WaveFormat);
                wfr.WriteSamples(activeStack.ToArray(), 0, activeStack.Count);
                wfr.Flush();
                File.WriteAllBytes("C:\\Users\\Jaime\\Desktop\\superTest.wav", memstream.GetBuffer());
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

        private const int FRAMES_PER_COMPARISON = 10;
        private const int MIN_FRAMES_FOR_VERIFICATION = 5;

        private double oldNoiseEnergy = 0;
        private double newNoiseEnergy = 0;
        private double P = 0;                     //P is not a constant anymore

        private const int SILENCE_BUFFER_SIZE = 10;

        private double oldVariance = 0;
        private double newVariance = 0;

        private void waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            int i = 0;
            float[] audioDataFrame_32 = new float[SAMPLES_PER_FRAME_8K];
            short[] audioDataFrame_16 = new short[SAMPLES_PER_FRAME_8K];

            for (int index = 0; index < (e.BytesRecorded); index += 2)
            {
                short sample_16 = (short)((e.Buffer[index + 1] << 8) | e.Buffer[index + 0]);
                float sample_32 = sample_16 / 32768f;

                audioDataFrame_32[i % SAMPLES_PER_FRAME_8K] = sample_32;
                i++;

                if ((i % (SAMPLES_PER_FRAME_8K)) == 0 && index > 0) //FRAME READY?
                {
                    noiseReduction(audioDataFrame_32, 0, SAMPLES_PER_FRAME_8K);

                    if (!initialTrainingFlag)
                    {
                        // Check current frame for Voice Activity.
                        bool isFrameActive = checkForVoiceActivity(audioDataFrame_32, newNoiseEnergy, SAMPLES_PER_FRAME_8K);
                        if (isFrameActive)
                        {
                            VadDetected = "Yes";
                            addFrameToActiveList(audioDataFrame_32);
                        }
                        else
                        {
                            if (checkInactiveFrameZeroCross(audioDataFrame_32, SAMPLES_PER_FRAME_8K) && silenceEnergyStack.Count >= SILENCE_BUFFER_SIZE)
                            {
                                VadDetected = "Yes";
                                addFrameToActiveList(audioDataFrame_32);
                            }
                            else
                            {
                                VadDetected = "No";
                                if (silenceEnergyStack.Count >= SILENCE_BUFFER_SIZE)
                                {
                                    generateSilenceEnergyVariances(audioDataFrame_32);
                                    // A sudden change in the background noise would mean newVariance > oldVariance
                                    updatePValue();
                                    updateThreshold(newNoiseEnergy, getFrameEnergy(audioDataFrame_32, SAMPLES_PER_FRAME_8K));
                                }
                                else
                                    silenceEnergyStack.Add(getFrameEnergy(audioDataFrame_32, SAMPLES_PER_FRAME_8K));
                            }
                            
                        }
                    }
                    else
                    {
                        frameStack.Add(audioDataFrame_32);
                        if (frameStack.Count >= FRAMES_PER_COMPARISON)
                            setInitialNoiseEnergy(audioDataFrame_32);
                    }
                }
            }
        }

        private void addFrameToActiveList(float[] audioDataFrame_32)
        {
            foreach (float sample in audioDataFrame_32)
            {
                activeStack.Add(sample);
            }
        }

        public bool checkInactiveFrameZeroCross(float[] audioDataFrame_32, int SAMPLES_PER_FRAME_8K)
        {
            int numberOfZeroCrossings = 0;

            for (int i = 0; i < (SAMPLES_PER_FRAME_8K); i += 2)
            {
                if ((audioDataFrame_32[i] * audioDataFrame_32[i + 1]) < 0)
                    numberOfZeroCrossings++;

                if((i + 2) % (SAMPLES_PER_FRAME_8K / 2) == 0 && i != 0)
                {
                    if (numberOfZeroCrossings < 5 || numberOfZeroCrossings > 15)
                        return (false);
                    numberOfZeroCrossings = 0;
                }
            }
            return (true);
        }

        private void updatePValue()
        {
            double varianceRelationship = (newVariance / oldVariance);
            if (varianceRelationship >= 1.25)
                P = 0.25;
            else if (varianceRelationship >= 1.10 && varianceRelationship <= 1.24)
                P = 0.20;
            else if (varianceRelationship >= 1.0 && varianceRelationship <= 1.9)
                P = 0.15;
            else
                P = 0.10;
        }

        private void generateSilenceEnergyVariances(float[] audioDataFrame_32)
        {
            oldVariance = getVariance(silenceEnergyStack.ToArray());
            silenceEnergyStack.Remove(silenceEnergyStack.First());
            silenceEnergyStack.Add(getFrameEnergy(audioDataFrame_32, SAMPLES_PER_FRAME_8K));
            newVariance = getVariance(silenceEnergyStack.ToArray());
        }

        private double getVariance(double[] data)
        {
            double mean = getMean(data);
            double temp = 0;
            foreach (double a in data)
               temp += (a - mean) * (a - mean);
            return (temp / data.Length);
        }

        private double getMean(double[] data)   //la Media
        {
            double sum = 0.0;
            foreach (double a in data)
                sum += a;
            return (sum / data.Length);
        }

        private void setInitialNoiseEnergy(float[] audioDataFrame_32)
        {
            newNoiseEnergy = oldNoiseEnergy = calculateInitialNoiseEnergy(getCurrentFrameStackEnergy());
            ThresholdDetected = oldNoiseEnergy.ToString();
            initialTrainingFlag = false;
        }


        private double[] getCurrentFrameStackEnergy()
        {
            return (frameStack.Take(FRAMES_PER_COMPARISON)
                    .Select(frame => getFrameEnergy(frame, SAMPLES_PER_FRAME_8K))
                    .ToArray());
        }

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
            // Formula modificada (trata los frames de manera individual).
            //           k
            // E = (1/k) ∑ [x(i)]^2
            //          i=1

            double EnergySum = 0;
            double k = (((double)1.0) / sampleCount);

            for (int i=1; i < sampleCount; i++)
            {
                EnergySum += Math.Pow(frameSamples[i], 2);                
            }
            return (k * EnergySum);
        } 

        private double calculateInitialNoiseEnergy(double[] framesEnergy)
        {
            double framesEnergySum = 0;
            for (int i = 1; i < framesEnergy.Length; i++)
            {
                framesEnergySum += framesEnergy[i];
            }
            return (framesEnergySum / framesEnergy.Length);
        }

        private bool checkForVoiceActivity(float[] frame, double newNoiseEnergy, int sampleCount)
        {
            double frameEnergy = getFrameEnergy(frame, sampleCount);
            double threshold = (K * newNoiseEnergy);
            return (frameEnergy > threshold);       
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
