using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoPOE.Logic.Actions
{
    public interface IAction
    {
        Task<ActionResultType> Tick();
        void Render();
    }
}
