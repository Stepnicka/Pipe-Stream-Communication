using System;
using System.Collections.Generic;
using System.Text;

namespace DataSharingLibrary
{
    public class PipeTaskState<T>
    {
        public PipeResponse<T> Response { get; set; }
    }
}
