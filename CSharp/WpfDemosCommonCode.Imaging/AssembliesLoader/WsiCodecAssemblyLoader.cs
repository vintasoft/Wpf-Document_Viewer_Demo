using Vintasoft.Imaging;

namespace WpfCommonCode
{
    /// <summary>
    /// Loads the Vintasoft.Imaging.WsiCodec assembly.
    /// </summary>
    public class WsiCodecAssemblyLoader
    {

        /// <summary>
        /// Loads the Vintasoft.Imaging.WebpCodec assembly.
        /// </summary>
        public static void Load()
        {
            WsiCodecAssembly.Init();
        }

    }
}
