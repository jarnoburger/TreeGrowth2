                    using System;
using System.Runtime.InteropServices;
using TreeGrowth.Ndi;

namespace TreeGrowth
{
    /// <summary>
    /// Simple NDI sender for streaming video frames
    /// Requires NDI Runtime installed from https://ndi.video/tools/
    /// </summary>
    public sealed class NdiSender : IDisposable
    {
        private IntPtr _sendInstance;
        private readonly string _sourceName;
        private int _width;
        private int _height;
        private bool _isInitialized;
        private IntPtr _ndiNamePtr;

        public bool IsInitialized => _isInitialized;
        public string SourceName => _sourceName;

        public NdiSender(string sourceName, int width, int height)
        {
            _sourceName = sourceName ?? "NDI Source";
            _width = width;
            _height = height;
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                // Initialize NDI library
                if (!NdiInterop.NDIlib_initialize())
                {
                    throw new InvalidOperationException(
                        "Failed to initialize NDI.\n\n" +
                        "Please install NDI Runtime from:\n" +
                        "https://ndi.video/tools/\n\n" +
                        "(Download 'NDI 5 Tools' and install 'NDI Runtime')"
                    );
                }

                // Create NDI sender
                _ndiNamePtr = Marshal.StringToHGlobalAnsi(_sourceName);

                var sendSettings = new NdiInterop.NDIlib_send_create_t
                {
                    p_ndi_name = _ndiNamePtr,
                    p_groups = IntPtr.Zero,
                    clock_video = false,
                    clock_audio = false
                };

                _sendInstance = NdiInterop.NDIlib_send_create(ref sendSettings);

                if (_sendInstance == IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_ndiNamePtr);
                    throw new InvalidOperationException("Failed to create NDI sender");
                }

                _isInitialized = true;
            }
            catch (DllNotFoundException)
            {
                throw new InvalidOperationException(
                    "NDI Library not found.\n\n" +
                    "Please install NDI Runtime from:\n" +
                    "https://ndi.video/tools/\n\n" +
                    "Make sure 'Processing.NDI.Lib.x64.dll' is in your PATH or application directory."
                );
            }
        }

        /// <summary>
        /// Send a video frame from byte array (BGRA format)
        /// Most efficient method - zero-copy
        /// </summary>
        public unsafe void SendFrame(byte[] pixelBuffer, int width, int height)
        {
            if (!_isInitialized || _sendInstance == IntPtr.Zero)
                return;

            // Update dimensions if changed
            if (width != _width || height != _height)
            {
                _width = width;
                _height = height;
            }

            fixed (byte* pData = pixelBuffer)
            {
                var videoFrame = new NdiInterop.NDIlib_video_frame_v2_t
                {
                    xres = width,
                    yres = height,
                    FourCC = NdiInterop.NDIlib_FourCC_video_type_e.NDIlib_FourCC_type_BGRA,
                    frame_rate_N = 60000,  // 60 FPS
                    frame_rate_D = 1000,
                    picture_aspect_ratio = (float)width / height,
                    frame_format_type = NdiInterop.NDIlib_frame_format_type_e.NDIlib_frame_format_type_progressive,
                    timecode = NDIlib_send_timecode_synthesize,
                    p_data = (IntPtr)pData,
                    line_stride_in_bytes = width * 4,  // BGRA = 4 bytes per pixel
                    p_metadata = IntPtr.Zero,
                    timestamp = 0
                };

                // Send frame asynchronously (non-blocking)
                NdiInterop.NDIlib_send_send_video_async_v2(_sendInstance, ref videoFrame);
            }
        }

        // Special timecode value that tells NDI to synthesize timecode
        private const long NDIlib_send_timecode_synthesize = long.MaxValue;

        public void Dispose()
        {
            if (_sendInstance != IntPtr.Zero)
            {
                NdiInterop.NDIlib_send_destroy(_sendInstance);
                _sendInstance = IntPtr.Zero;
            }

            if (_ndiNamePtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_ndiNamePtr);
                _ndiNamePtr = IntPtr.Zero;
            }

            _isInitialized = false;
        }
    }
}
