using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoPOE.Logic.Sequences
{
    public interface ISequence
    {
        void Tick();
        void Render();
    }
}
