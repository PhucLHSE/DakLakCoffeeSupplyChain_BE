using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.Repositories
{
    public class ProcessingBatchEvaluationRepository
          : GenericRepository<ProcessingBatchEvaluation>, IProcessingBatchEvaluationRepository
    {
        public ProcessingBatchEvaluationRepository() : base() { }

        public ProcessingBatchEvaluationRepository(DakLakCoffee_SCMContext context) : base(context) { }
    }
}
