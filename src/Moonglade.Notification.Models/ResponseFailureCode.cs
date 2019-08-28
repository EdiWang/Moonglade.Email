using System;
using System.Collections.Generic;
using System.Text;

namespace Moonglade.Notification.Models
{
    public enum ResponseFailureCode
    {
        None = 0,
        GeneralException = 1,
        EmailSendingDisabled = 500
    }
}
