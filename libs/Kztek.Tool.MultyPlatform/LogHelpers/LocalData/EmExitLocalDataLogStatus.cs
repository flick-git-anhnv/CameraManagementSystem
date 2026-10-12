using System;
using System.Collections.Generic;
using System.Text;

namespace Kztek.Tool.LogHelpers.LocalData
{
    public enum EmExitLocalDataLogStatus
    {
        Processing,
        StartCheckOut,
        WaitForConfirm,
        WaitForDelete,
        CreateTransaction,
        Exit
    }
}

