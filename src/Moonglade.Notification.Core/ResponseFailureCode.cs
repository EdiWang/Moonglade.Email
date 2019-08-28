using System;
using System.Collections.Generic;
using System.Text;

namespace Moonglade.Notification.Core
{
    public enum ResponseFailureCode
    {
        None = 0,
        GeneralException = 1,
        EmailSendingDisabled = 500
    }
}
