using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;

namespace DxConvolutionTest
{
    public unsafe class DxBlur
    {
        D3D11 _api;
        D3DCompiler _compiler;

        ComPtr<ID3D11Device> _device;
        ComPtr<ID3D11DeviceContext> _deviceContext;
        ComPtr<ID3D11Texture2D> _renderTarget;
        ComPtr<ID3D11Texture2D> _outputBuffer;
        ComPtr<ID3D11Buffer> _vertexBuffer;
        ComPtr<ID3D11Buffer> _indexBuffer;
        ComPtr<ID3D10Blob> _vertexShaderCode;
        ComPtr<ID3D10Blob> _pixelShaderCode;
        ComPtr<ID3D11VertexShader> _vertexShader;
        ComPtr<ID3D11PixelShader> _pixelShader;
        ComPtr<ID3D11InputLayout> _inputLayout;

        private float[] _background;

        public int InputWidth { get; }
        public int OutputWidth { get; }
        public int InputHeight { get; }
        public int OutputHeight { get; }
        public int BlurSize { get; }

        public DxBlur(
            int inputWidth, int inputHeight, int blurSize,
            float backgroundR, float backgroundG, float backgroundB)
        {
            if (blurSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(blurSize));
            }

            if (inputWidth - blurSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(inputWidth));
            }

            if (inputHeight - blurSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(inputHeight));
            }

            int outputWidth = inputWidth - blurSize;
            int outputHeight = inputHeight - blurSize;

            _api = D3D11.GetApi(null, false);
            _compiler = D3DCompiler.GetApi();

            _api.CreateDevice(ref Unsafe.NullRef<IDXGIAdapter>(), D3DDriverType.Hardware, 0, (uint)CreateDeviceFlag.Debug, ref Unsafe.NullRef<D3DFeatureLevel>(), 0, D3D11.SdkVersion, ref _device, null, ref _deviceContext);

            //This is not supported under DXVK 
            //TODO: PR a stub into DXVK for this maybe?
            if (OperatingSystem.IsWindows())
            {
                // Log debug messages for this device (given that we've enabled the debug flag). Don't do this in release code!
                _device.SetInfoQueueCallback(msg => Console.WriteLine(SilkMarshal.PtrToString((nint)msg.PDescription)));
            }

            var renderTargetDesc = new Texture2DDesc()
            {
                Width = (uint)outputWidth,
                Height = (uint)outputHeight,
                ArraySize = 1,
                BindFlags = (uint)BindFlag.RenderTarget,
                CPUAccessFlags = 0,
                Format = Format.FormatB8G8R8A8Unorm,
                MipLevels = 1,
                MiscFlags = 0,
                SampleDesc = new SampleDesc(1, 0),
                Usage = Usage.Default,
            };

            var outputBufferDesc = new Texture2DDesc()
            {
                Width = (uint)outputWidth,
                Height = (uint)outputHeight,
                ArraySize = 1,
                BindFlags = 0,
                CPUAccessFlags = (uint)CpuAccessFlag.Read,
                Format = Format.FormatB8G8R8A8Unorm,
                MipLevels = 1,
                MiscFlags = 0,
                SampleDesc = new SampleDesc(1, 0),
                Usage = Usage.Staging,
            };

            _device.CreateTexture2D(in renderTargetDesc, ref Unsafe.NullRef<SubresourceData>(), ref _renderTarget);
            _device.CreateTexture2D(in outputBufferDesc, ref Unsafe.NullRef<SubresourceData>(), ref _outputBuffer);

            var vertices = new float[]
            {
                -1, -1, 0,
                -1, 1, 0,
                1, 1, 0,
                1, -1, 0
            };

            var indices = new uint[]
            {
                0, 1, 2,
                0, 2, 3
            };

            fixed (float* vertexData = vertices)
            {
                BufferDesc bufferDesc = new BufferDesc
                {
                    BindFlags = (uint)BindFlag.VertexBuffer,
                    ByteWidth = (uint)(sizeof(float) * vertices.Length),
                    CPUAccessFlags = 0,
                    MiscFlags = 0,
                    Usage = Usage.Default,
                };

                SubresourceData data = new SubresourceData
                {
                    PSysMem = vertexData,
                };

                _device.CreateBuffer(in bufferDesc, in data, ref _vertexBuffer);
            }

            fixed (uint* indexData = indices)
            {
                BufferDesc bufferDesc = new BufferDesc
                {
                    BindFlags = (uint)BindFlag.IndexBuffer,
                    ByteWidth = (uint)(sizeof(uint) * indices.Length),
                    CPUAccessFlags = 0,
                    MiscFlags = 0,
                    Usage = Usage.Default,
                };

                SubresourceData data = new SubresourceData
                {
                    PSysMem = indexData,
                };

                _device.CreateBuffer(in bufferDesc, in data, ref _indexBuffer);
            }

            string shaderCode = CreateShaderText(inputWidth, inputHeight, blurSize);
            byte[] shaderCodeBytes = Encoding.ASCII.GetBytes(shaderCode);

            fixed (byte* pShaderCode = shaderCodeBytes)
            {
                ComPtr<ID3D10Blob> errorMsgs = null;
                _compiler.Compile(pShaderCode, (nuint)(sizeof(char) * shaderCode.Length), "shader", ref Unsafe.NullRef<D3DShaderMacro>(), ref Unsafe.NullRef<ID3DInclude>(), "vs_main", "vs_5_0", 0, 0, ref _vertexShaderCode, ref errorMsgs);
                if (errorMsgs.Handle != null)
                {
                    string error = Encoding.ASCII.GetString((byte*)errorMsgs.GetBufferPointer(), (int)errorMsgs.GetBufferSize());
                }

                _compiler.Compile(pShaderCode, (nuint)(sizeof(char) * shaderCode.Length), "shader", ref Unsafe.NullRef<D3DShaderMacro>(), ref Unsafe.NullRef<ID3DInclude>(), "ps_main", "ps_5_0", 0, 0, ref _pixelShaderCode, ref errorMsgs);
                if (errorMsgs.Handle != null)
                {
                    string error = Encoding.ASCII.GetString((byte*)errorMsgs.GetBufferPointer(), (int)errorMsgs.GetBufferSize());
                }

                _device.CreateVertexShader(_vertexShaderCode.GetBufferPointer(), _vertexShaderCode.GetBufferSize(), ref Unsafe.NullRef<ID3D11ClassLinkage>(), ref _vertexShader);
                _device.CreatePixelShader(_pixelShaderCode.GetBufferPointer(), _pixelShaderCode.GetBufferSize(), ref Unsafe.NullRef<ID3D11ClassLinkage>(), ref _pixelShader);
            }

            var sematicName = "POSITION";
            var sematicNameBytes = Encoding.ASCII.GetBytes(sematicName);

            fixed (byte* pSematicName = sematicNameBytes)
            {
                InputElementDesc inputElementDesc = new InputElementDesc
                {
                    SemanticName = (byte*)pSematicName,
                    SemanticIndex = 0,
                    Format = Format.FormatR32G32B32Float,
                    InputSlot = 0,
                    InputSlotClass = InputClassification.PerVertexData,
                };

                _device.CreateInputLayout(in inputElementDesc, 1, _vertexShaderCode.GetBufferPointer(), _vertexShaderCode.GetBufferSize(), ref _inputLayout);
            }

            _background = new float[]
            {
                backgroundR,
                backgroundG,
                backgroundB,
                1.0f
            };

            InputWidth = inputWidth;
            InputHeight = inputHeight;
            OutputWidth = outputWidth;
            OutputHeight = outputHeight;
            BlurSize = blurSize;
        }

        private static string CreateShaderText(int inputWidth, int inputHeight, int blurSize)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(
                """
                Texture2D srcTexture;
                SamplerState PointSampler;

                struct vs_in 
                {
                    float3 position : POSITION;
                };

                struct vs_out
                {
                    float4 position : SV_POSITION;
                };

                vs_out vs_main(vs_in input) 
                {
                    vs_out output = 
                    {
                        float4(input.position, 1.0)
                    };

                    return output;
                }

                float4 ps_main(vs_out input) : SV_TARGET
                {
                    float4 result = float4(0, 0, 0, 0);
                
                """);

            for (int x = 0; x < blurSize; x++)
            {
                for (int y = 0; y < blurSize; y++)
                {
                    sb.AppendLine(
                        $"""
                            result = result + srcTexture.Sample(PointSampler, float2((input.position.x + {x}) / {inputWidth}, (input.position.y + {y}) / {inputHeight}));
                        """);
                }
            }

            var rectPixelCount = blurSize * blurSize;
            sb.AppendLine(
                $$"""
                    result = result / {{rectPixelCount}};

                    return result;
                }
                """);

            return sb.ToString();
        }

        public void Process(ReadOnlySpan<byte> bgraInput, Span<byte> bgraOutput)
        {
            if (bgraInput.Length != (InputWidth * InputHeight * 4))
            {
                throw new ArgumentException("Size not match", nameof(bgraInput));
            }

            if (bgraOutput.Length != (OutputWidth * OutputHeight * 4))
            {
                throw new ArgumentException("Size not match", nameof(bgraOutput));
            }

            var inputTextureDesc = new Texture2DDesc()
            {
                Width = (uint)InputWidth,
                Height = (uint)InputHeight,
                ArraySize = 1,
                BindFlags = (uint)BindFlag.ShaderResource,
                CPUAccessFlags = 0,
                Format = Format.FormatB8G8R8A8Unorm,
                MipLevels = 1,
                MiscFlags = 0,
                SampleDesc = new SampleDesc(1, 0),
                Usage = Usage.Immutable,
            };

            ComPtr<ID3D11Texture2D> inputTexture = default;

            fixed (byte* ptr = bgraInput)
            {
                SubresourceData subresourceData = new SubresourceData
                {
                    PSysMem = ptr,
                    SysMemPitch = (uint)(InputWidth * 4),
                    SysMemSlicePitch = (uint)(InputWidth * InputHeight * 4)
                };

                var hr = _device.CreateTexture2D(in inputTextureDesc, in subresourceData, ref inputTexture);
                SilkMarshal.ThrowHResult(hr);

                ShaderResourceViewDesc inputTextureShaderResourceViewDesc = new ShaderResourceViewDesc
                {
                    Format = inputTextureDesc.Format,
                    ViewDimension = D3DSrvDimension.D3D11SrvDimensionTexture2D,
                };

                inputTextureShaderResourceViewDesc.Texture2D.MipLevels = 1;
                inputTextureShaderResourceViewDesc.Texture2D.MostDetailedMip = 0;

                ComPtr<ID3D11ShaderResourceView> inputTextureShaderResourceView = default;
                _device.CreateShaderResourceView(inputTexture, in inputTextureShaderResourceViewDesc, ref inputTextureShaderResourceView);

                SamplerDesc samplerDesc = new SamplerDesc
                {
                    Filter = Filter.MinMagMipPoint,
                    AddressU = TextureAddressMode.Clamp,
                    AddressV = TextureAddressMode.Clamp,
                    AddressW = TextureAddressMode.Clamp,
                };

                ComPtr<ID3D11SamplerState> samplerState = default;
                _device.CreateSamplerState(in samplerDesc, ref samplerState);

                var viewport = new Viewport(0, 0, OutputWidth, OutputHeight, 0, 1);

                ComPtr<ID3D11RenderTargetView> renderTargetView = default;
                _device.CreateRenderTargetView<ID3D11Texture2D, ID3D11RenderTargetView>(_renderTarget, in Unsafe.NullRef<RenderTargetViewDesc>(), ref renderTargetView);

                // clear ouptut
                _deviceContext.ClearRenderTargetView(renderTargetView, ref _background[0]);

                _deviceContext.RSSetViewports(1, in viewport);
                _deviceContext.OMSetRenderTargets(1, ref renderTargetView, ref Unsafe.NullRef<ID3D11DepthStencilView>());

                _deviceContext.VSSetShader(_vertexShader, ref Unsafe.NullRef<ComPtr<ID3D11ClassInstance>>(), 0);
                _deviceContext.PSSetShader(_pixelShader, ref Unsafe.NullRef<ComPtr<ID3D11ClassInstance>>(), 0);

                _deviceContext.IASetPrimitiveTopology(D3DPrimitiveTopology.D3D11PrimitiveTopologyTrianglelist);
                _deviceContext.IASetInputLayout(_inputLayout);

                uint vertexStride = sizeof(float) * 3;
                uint vertexOffset = 0;
                _deviceContext.IASetVertexBuffers(0, 1, ref _vertexBuffer, in vertexStride, in vertexOffset);
                _deviceContext.IASetIndexBuffer(_indexBuffer, Format.FormatR32Uint, 0);

                _deviceContext.PSSetShaderResources(0, 1, ref inputTextureShaderResourceView);
                _deviceContext.PSSetSamplers(0, 1, ref samplerState);

                _deviceContext.DrawIndexed(6, 0, 0);

                _deviceContext.CopyResource(_outputBuffer, _renderTarget);

                MappedSubresource mappedSubresource = default;
                _deviceContext.Map(_outputBuffer, 0, Map.Read, 0, ref mappedSubresource);

                fixed (byte* outputPtr = bgraOutput)
                {
                    for (int y = 0; y < OutputHeight; y++)
                    {
                        var lineBytes = OutputWidth * 4;
                        NativeMemory.Copy((byte*)((nint)mappedSubresource.PData + mappedSubresource.RowPitch * y), outputPtr + lineBytes * y, (nuint)lineBytes);
                    }
                }

                _deviceContext.Unmap(_outputBuffer, 0);

                renderTargetView.Dispose();
                inputTextureShaderResourceView.Dispose();
                samplerState.Dispose();
                inputTexture.Dispose();
            }
        }
    }
}
