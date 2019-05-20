using System;
using System.Text;

namespace pluginiq
{
    internal class PackageFrame
    {
        public PackageFrame()
        {
            
        }

        public byte[] surroundFisFrame(string data_, UInt32 plgId_)
        {
            byte[] data = Encoding.ASCII.GetBytes(data_);
            byte[] opId = BitConverter.GetBytes((UInt32)0);
            byte[] plgId = BitConverter.GetBytes((UInt32)plgId_);
            byte[] len = BitConverter.GetBytes((UInt32)(data_.Length + 1));

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(opId);
                Array.Reverse(plgId);
                Array.Reverse(len);
            }

            byte[] fisPkg = new byte[opId.Length + plgId.Length + len.Length + data_.Length + 1];
            opId.CopyTo(fisPkg, 0);
            plgId.CopyTo(fisPkg, opId.Length);
            len.CopyTo(fisPkg, opId.Length + plgId.Length);
            data.CopyTo(fisPkg, opId.Length + plgId.Length + len.Length);
            fisPkg[fisPkg.Length - 1] = 0;
            return fisPkg;
        }

    }
}