﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.core
{
    public class MsgException : Exception
    {
        public MsgException(string msg) : base(msg)
        { }
    }
}
