using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Generators
{
    public interface ICodeGenerator
    {
        Task<string> GenerateUserCodeAsync();

        Task<string> GenerateManagerCodeAsync();

        Task<string> GenerateBuyerCodeAsync(Guid managerId);
    }
}
