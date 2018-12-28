using System;

namespace ScsContentMigrator.DataBlaster.Sitecore.DataBlaster.Load
{
    [Flags]
    public enum StageResult
    {
        Unknown = 0,
        Succeeded = 1,
        Failed = 2,
        HasWarnings = 4,
        HasErrors = 8
    }
}