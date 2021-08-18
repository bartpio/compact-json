namespace CompactJson
{
    /// <summary>
    /// compact JSON formats
    /// </summary>
    public static class CompactJsonFormats
    {
        /// <summary>
        /// zero character (ASCII/UTF8)
        /// </summary>
        public const byte Zero = (byte)'0';

        /// <summary>
        /// one character (ASCII/UTF8)
        /// </summary>
        public const byte One = (byte)'1';

        /// <summary>
        /// our datetime format (within invariant culture)
        /// </summary>
        public const string DateTime = "yyyy-MM-dd HH:mm:ss.ffffff";

        /// <summary>
        /// our datetimeoffset format (within invariant culture)
        /// </summary>
        public const string DateTimeOffset = "O";
    }
}
