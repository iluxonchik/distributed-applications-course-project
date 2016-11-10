using System;

namespace ConfigTypes
{
    [Serializable]
   public  enum CommandType
    {
        Start,
        Interval,
        Status,
        Crash,
        Freeze,
        Unfreeze,
        Wait

    }
}