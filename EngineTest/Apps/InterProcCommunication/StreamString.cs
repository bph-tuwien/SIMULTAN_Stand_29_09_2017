using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Pipes;
using System.Runtime.CompilerServices;

namespace InterProcCommunication
{
    public class StreamString
    {
        private PipeStream ioStream;
        private UnicodeEncoding streamEncoding;

        public StreamString(PipeStream ioStream)
        {
            this.ioStream = ioStream;
            streamEncoding = new UnicodeEncoding();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public string ReadString()
        {
            if (this.ioStream == null) return string.Empty;
            if (!this.ioStream.IsConnected) return string.Empty;

            int len = 0;

            try
            {
                len = ioStream.ReadByte() * 256;
                if (len < 0)
                    return string.Empty;
                len += ioStream.ReadByte(); // causes System.ObjectDisposedException

                byte[] inBuffer = new byte[len];
                ioStream.Read(inBuffer, 0, len);

                string just_read = streamEncoding.GetString(inBuffer);
                return just_read;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public int WriteString(string outString)
        {
            if (this.ioStream == null) return 0;
            if (!this.ioStream.IsConnected) return 0;

            try
            {
                byte[] outBuffer = streamEncoding.GetBytes(outString);
                int len = outBuffer.Length;
                if (len > UInt16.MaxValue)
                {
                    len = (int)UInt16.MaxValue;
                }
                var debug1 = (byte)(len / 256);
                var debug2 = (byte)(len & 255);
                ioStream.WriteByte((byte)(len / 256));
                ioStream.WriteByte((byte)(len & 255));
                ioStream.Write(outBuffer, 0, len);
                ioStream.Flush();

                return outBuffer.Length + 2;
            }
            catch
            {
                return 0;
            }
        }
    }
}
