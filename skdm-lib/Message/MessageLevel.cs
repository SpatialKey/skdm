using System;

namespace SpatialKey.DataManager.Lib.Message
{
    // higher value == worse level
	public enum MessageLevel
	{
        Success,
        Result,
        Canceled,
        Verbose,
        Help,
		Status,
        Warning,
        Error
	}
}

