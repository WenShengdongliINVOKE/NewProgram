using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 小玩意.Comm;

namespace 小玩意.PlcModel
{
    interface  IPlcCommunicationModel
    {
        List<IPlccommiunctionCommand> GetPlcModels();

    }
}
