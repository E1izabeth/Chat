using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chat.UI.ViewModel;
using Concentus.Oggfile;
using Concentus.Structs;
using NAudio.Wave;

namespace Chat.UI.Util
{
    public class OggPlayer
    {
        private object _playbackLock = new object();
        private Queue<VoiceMessageItem> _queue = new Queue<VoiceMessageItem>();
        private NAudio.Wave.WaveOut _waveOut = null;
        private NAudio.Wave.IWaveProvider _currentReader = null;

        public bool IsPlaying { get; private set; }

        private VoiceMessageItem _currentItem = null;

        public OggPlayer()
        {
            this.IsPlaying = false;
        }

        public void PlayOggAudio(VoiceMessageItem stream)
        {
            lock (_playbackLock)
            {
                _queue.Enqueue(stream);

                if (!this.IsPlaying)
                    this.PlaybackProc();
            }
        }

        private void PlaybackProc()
        {
            void StartPlaying()
            {
                if (_queue.Count > 0)
                {
                    //_currentReader = new NAudio.Vorbis.VorbisWaveReader(_queue.Dequeue());
                    //_currentReader = new NAudio.Vorbis.VorbisWaveReader(@"v:\out2.ogg");
                    _currentItem = _queue.Dequeue();
                    _currentReader = this.ExtractOgg(new MemoryStream(_currentItem.Data.OggData));
                    _currentItem.OnStared();
                    _waveOut.Init(_currentReader);
                    _waveOut.Play();
                    this.IsPlaying = true;
                }
                else
                {
                    this.IsPlaying = false;
                }
            }

            lock (_playbackLock)
            {
                if (_waveOut == null)
                {
                    _waveOut = new NAudio.Wave.WaveOut();
                    _waveOut.PlaybackStopped += (sender, ea) => {
                        lock (_playbackLock)
                        {
                            _currentItem.OnStopped();
                            _currentItem = null;
                            StartPlaying();
                        }
                    };
                }

                StartPlaying();
            }
        }

        public IWaveProvider ExtractOgg(Stream ogg)
        {
            //using (FileStream fileIn = new FileStream($"{filePath}{fileOgg}", FileMode.Open))
            using (MemoryStream pcmStream = new MemoryStream())
            {
                OpusDecoder decoder = new OpusDecoder(48000, 1);
                OpusOggReadStream oggIn = new OpusOggReadStream(decoder, ogg);
                while (oggIn.HasNextPacket)
                {
                    short[] packet = oggIn.DecodeNextPacket();
                    if (packet != null)
                    {
                        for (int i = 0; i < packet.Length; i++)
                        {
                            var bytes = BitConverter.GetBytes(packet[i]);
                            pcmStream.Write(bytes, 0, bytes.Length);
                        }
                    }
                }

                pcmStream.Position = 0;
                var wavStream = new RawSourceWaveStream(new MemoryStream(pcmStream.ToArray()), new WaveFormat(48000, 1));
                //var sampleProvider = wavStream.ToSampleProvider();
                //WaveFileWriter.CreateWaveFile16($"{filePath}{fileWav}", sampleProvider);

                return wavStream;// new SampleToWaveProvider24(sampleProvider);
            }
        }
    }
}
