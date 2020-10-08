using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chat.Common;
using Concentus.Enums;
using Concentus.Oggfile;
using Concentus.Structs;
using NAudio.Wave;

namespace Chat.UI.Util
{
    class OggRecorder
    {
        public event Action<byte[]> OnRecordFinished;

        private WaveIn _waveIn = null;

        private MemoryStream _recordingBuffer = null;
        private OpusOggWriteStream _recorder = null;

        public bool IsRecording { get; private set; }

        public OggRecorder()
        {
            this.IsRecording = false;
        }

        public void StartRecording()
        {
            if (this.IsRecording)
                throw new InvalidOperationException("Already recording");

            this.IsRecording = true;

            _waveIn = new WaveIn() {
                WaveFormat = new WaveFormat(48000, 16, 1)
            };
            _waveIn.DataAvailable += (sender, ea) => {
                var buff = new short[ea.BytesRecorded / 2];
                Buffer.BlockCopy(ea.Buffer, 0, buff, 0, ea.BytesRecorded);
                _recorder.WriteSamples(buff, 0, buff.Length);
            };
            _waveIn.RecordingStopped += (sender, ea) => {
                this.IsRecording = false;
                if (ea.Exception != null)
                {
                    this.OnRecordFinished?.Invoke(null);
                    Chat.UI.Common.Util.MsgBox(ea.Exception.ToString());
                }
                else
                {
                    _recorder.Finish();
                    _recordingBuffer.Flush();
                    var data = _recordingBuffer.ToArray();
                    this.OnRecordFinished?.Invoke(data);
                }

                _waveIn.SafeDispose();
                _recordingBuffer.SafeDispose();

                _waveIn = null;
                _recordingBuffer = null;
                _recorder = null;
            };

            _recordingBuffer = new MemoryStream();
            var encoder = new OpusEncoder(48000, 1, OpusApplication.OPUS_APPLICATION_VOIP);
            _recorder = new OpusOggWriteStream(encoder, _recordingBuffer);

            _recordingBuffer.SetLength(0);
            _recordingBuffer.Position = 0;
            _waveIn.StartRecording();

        }

        public void StopRecording()
        {
            if (this.IsRecording)
                _waveIn.StopRecording();
        }
    }
}
