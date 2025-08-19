using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoPOE.Logic.Actions
{
    public class IdleAction : IAction
    {
        public async Task<ActionResultType> Tick() { await Task.Delay(100); return ActionResultType.Running; }
        public void Render() { }
    }
}