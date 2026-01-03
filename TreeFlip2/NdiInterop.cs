using System;
using System.Runtime.InteropServices;

namespace TreeGrowth.Ndi
{
    /// <summary>
    /// P/Invoke declarations for NDI SDK
    /// Requires NDI Runtime to be installed: https://ndi.video/tools/
    /// </summary>
    internal static class NdiInterop
    {
        private const string NdiLibrary = "Processing.NDI.Lib.x64";

        #region Structures

        [StructLayout(LayoutKind.Sequential)]
        public struct NDIlib_send_create_t
        {
            public IntPtr p_ndi_name;        // UTF-8 string
            public IntPtr p_groups;          // UTF-8 string  
            public bool clock_video;
            public bool clock_audio;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NDIlib_video_frame_v2_t
        {
            public int xres;
            public int yres;
            public NDIlib_FourCC_video_type_e FourCC;
            public int frame_rate_N;
            public int frame_rate_D;
            public float picture_aspect_ratio;
            public NDIlib_frame_format_type_e frame_format_type;
            public long timecode;
            public IntPtr p_data;
            public int line_stride_in_bytes;
            public IntPtr p_metadata;
            public long timestamp;
        }

        public enum NDIlib_FourCC_video_type_e : uint
        {
            NDIlib_FourCC_type_UYVY = 0x59565955,  // 'UYVY'
            NDIlib_FourCC_type_UYVA = 0x41565955,  // 'UYVA'
            NDIlib_FourCC_type_P216 = 0x36313250,  // 'P216'
            NDIlib_FourCC_type_PA16 = 0x36314150,  // 'PA16'
            NDIlib_FourCC_type_YV12 = 0x32315659,  // 'YV12'
            NDIlib_FourCC_type_I420 = 0x30323449,  // 'I420'
            NDIlib_FourCC_type_NV12 = 0x3231564E,  // 'NV12'
            NDIlib_FourCC_type_BGRA = 0x41524742,  // 'BGRA'
            NDIlib_FourCC_type_BGRX = 0x58524742,  // 'BGRX'
            NDIlib_FourCC_type_RGBA = 0x41424752,  // 'RGBA'
            NDIlib_FourCC_type_RGBX = 0x58424752,  // 'RGBX'
        }

        public enum NDIlib_frame_format_type_e
        {
            NDIlib_frame_format_type_progressive = 1,
            NDIlib_frame_format_type_interleaved = 0,
            NDIlib_frame_format_type_field_0 = 2,
            NDIlib_frame_format_type_field_1 = 3,
        }

        #endregion

        #region Functions

        [DllImport(NdiLibrary, EntryPoint = "NDIlib_initialize", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool NDIlib_initialize();

        [DllImport(NdiLibrary, EntryPoint = "NDIlib_destroy", CallingConvention = CallingConvention.Cdecl)]
        public static extern void NDIlib_destroy();

        [DllImport(NdiLibrary, EntryPoint = "NDIlib_send_create", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr NDIlib_send_create(ref NDIlib_send_create_t p_create_settings);

        [DllImport(NdiLibrary, EntryPoint = "NDIlib_send_destroy", CallingConvention = CallingConvention.Cdecl)]
        public static extern void NDIlib_send_destroy(IntPtr p_instance);

        [DllImport(NdiLibrary, EntryPoint = "NDIlib_send_send_video_v2", CallingConvention = CallingConvention.Cdecl)]
        public static extern void NDIlib_send_send_video_v2(IntPtr p_instance, ref NDIlib_video_frame_v2_t p_video_data);

        [DllImport(NdiLibrary, EntryPoint = "NDIlib_send_send_video_async_v2", CallingConvention = CallingConvention.Cdecl)]
        public static extern void NDIlib_send_send_video_async_v2(IntPtr p_instance, ref NDIlib_video_frame_v2_t p_video_data);

        #endregion
    }
}