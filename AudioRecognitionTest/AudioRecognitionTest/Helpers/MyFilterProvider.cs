using NAudio.Dsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioRecognitionTest.Helpers
{
    public class MyFilterProvider
    {

        int channels;
        int lowCutOffFreq;
        int topCutOffFreq;
        BiQuadFilter[] highFilter;
        BiQuadFilter[] lowFilter;

        public MyFilterProvider(int channels, int lowCutOffFreq, int topCutOffFreq)
        {
            this.lowFilter = new BiQuadFilter[channels];
            this.highFilter = new BiQuadFilter[channels];
            this.lowCutOffFreq = lowCutOffFreq;
            this.topCutOffFreq = topCutOffFreq;
            this.channels = channels;
            Filter_LowPass();
            Filter_HighPass();
        }

        private void Filter_LowPass()
        {

            for (int n = 0; n < channels; n++)
                if (lowFilter[n] == null)
                    lowFilter[n] = BiQuadFilter.LowPassFilter(44100, lowCutOffFreq, 1);
        }

        private void Filter_HighPass()
        {

            for (int n = 0; n < channels; n++)
                if (highFilter[n] == null)
                    highFilter[n] = BiQuadFilter.HighPassFilter(44100, topCutOffFreq, 1);
        }

        public int Read(float[] buffer, int offset, int samplesRead)
        {
            for (int i = 0; i < samplesRead; i++)
            {
                buffer[offset + i] = lowFilter[(i % channels)].Transform(buffer[offset + i]);
                buffer[offset + i] = highFilter[(i % channels)].Transform(buffer[offset + i]);
            }
            return samplesRead;
        }


    }
}
