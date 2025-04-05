using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Core.Native;

namespace DxConvolutionTest.Extensions
{
    public static class SilkExtensions
    {
        public static string ReadAsString(this ID3D10Blob block, Encoding encoding)
        {
            return encoding.GetString(block.Buffer);
        }
    }
}
