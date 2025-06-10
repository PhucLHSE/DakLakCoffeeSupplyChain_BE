using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Generators
{
    public interface ICropSeasonCodeGenerator
    {
        Task<string> GenerateCropSeasonCodeAsync(int year);
    }
}
