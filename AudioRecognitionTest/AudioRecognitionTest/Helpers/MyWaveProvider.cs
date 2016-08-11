using NAudio.Dsp;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioRecognitionTest.Helpers
{
    public class MyWaveProvider : ISampleProvider
    {
        private ISampleProvider inWaveProvider;
        int channels;
        int lowCutOffFreq;
        int topCutOffFreq;
        BiQuadFilter[] highFilter;
        BiQuadFilter[] lowFilter;


        public MyWaveProvider(ISampleProvider inWaveProvider, int lowCutOffFreq, int topCutOffFreq)
        {
            this.inWaveProvider = inWaveProvider;
            this.WaveFormat = inWaveProvider.WaveFormat;
            this.channels = inWaveProvider.WaveFormat.Channels;
            this.lowFilter = new BiQuadFilter[channels];
            this.highFilter = new BiQuadFilter[channels];
            this.lowCutOffFreq = lowCutOffFreq;
            this.topCutOffFreq = topCutOffFreq;
            filter_LowPass();
            filter_HighPass();
        }

        public WaveFormat WaveFormat
        {
            get
            {
                return inWaveProvider.WaveFormat;
            }
            set
            {

            }
        }

        private void filter_LowPass()
        {

            for (int n = 0; n < channels; n++)
                if (lowFilter[n] == null)
                    lowFilter[n] = BiQuadFilter.LowPassFilter(44100, lowCutOffFreq, 1);
        }

        private void filter_HighPass()
        {

            for (int n = 0; n < channels; n++)
                if (highFilter[n] == null)
                    highFilter[n] = BiQuadFilter.HighPassFilter(44100, topCutOffFreq, 1);
        }


        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = inWaveProvider.Read(buffer, offset, count);

            for (int i = 0; i < samplesRead; i++)
            {
                buffer[offset + i] = lowFilter[(i % channels)].Transform(buffer[offset + i]);
                buffer[offset + i] = highFilter[(i % channels)].Transform(buffer[offset + i]);

            }
            return samplesRead;
        }


    }
}
