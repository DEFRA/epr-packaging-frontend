using System.Text;

namespace FrontendSchemeRegistration.Application.Helpers
{
    public static class BomHelper
    {
        /// <summary>
        /// BOM sequence is added to the start of the file. This helps programs like Excel ensure that all non-standard characters eg (C) and TM
        /// are encoded to the end user as expected.
        /// </summary>
        /// <param name="memoryStream">MemoryStream object to prepend BOM bytes to.</param>
        public static void PrependBOMBytes(MemoryStream memoryStream)
        {
            if (memoryStream is null)
            {
                return;
            }

            var memoryStreamPosition = memoryStream.Position;
            memoryStream.Position = 0;
            memoryStream.Write(Encoding.UTF8.GetPreamble());
        }
    }
}