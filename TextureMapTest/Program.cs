using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;


namespace TextureMapTest
{
    internal class Program
    {
        static unsafe void Main(string[] args)
        {
            var d3d = D3D11.GetApi(null);

            ComPtr<ID3D11Device> device = default;
            ComPtr<ID3D11DeviceContext> deviceContext = default;

            d3d.CreateDevice(
                ref Unsafe.NullRef<IDXGIAdapter>(),
                D3DDriverType.Hardware,
                0,
                (uint)CreateDeviceFlag.None,
                null,
                0,
                D3D11.SdkVersion,
                ref device,
                null,
                ref deviceContext);

            Texture2DDesc textDesc = new Texture2DDesc
            {
                Width = 1000,
                Height = 1000,
                CPUAccessFlags = (uint)CpuAccessFlag.Read,
                ArraySize = 1,
                BindFlags = (uint)BindFlag.RenderTarget,
                Format = Format.FormatR8G8B8A8SNorm,
                MipLevels = 0,
                MiscFlags = 0,
                SampleDesc = new SampleDesc(1, 0),
                Usage = Usage.Staging,
            };

            ComPtr<ID3D11Texture2D> texture = default;
            device.CreateTexture2D(ref textDesc, ref Unsafe.NullRef<SubresourceData>(), ref texture);

            MappedSubresource mappedSubresource = default;
            var mapHr = deviceContext.Map(texture, 0, Map.Read, 0, ref mappedSubresource);
            SilkMarshal.ThrowHResult(mapHr);

            byte[] data = new byte[mappedSubresource.DepthPitch];
            Marshal.Copy((IntPtr)mappedSubresource.PData, data, 0, (int)mappedSubresource.DepthPitch);

            File.WriteAllBytes("texture.bin", data);
        }
    }
}
